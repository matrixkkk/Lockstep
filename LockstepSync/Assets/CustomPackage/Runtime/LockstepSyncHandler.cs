using UnityEngine;

/// <summary>
/// 고정 프레임 처리기
/// </summary>
public class LockstepSyncHandler
{
    public delegate void UpdateCallback(double delta, int diff, int updateCount);
    public delegate void UpdateTurnCallback(uint turnCount);

    private double mCurrentFixedDelta;          //고정 프레임 delta 시간(s)
    private int mFixedDeltaMillisecond;         //fixed delta ms 단위
    private int mUpdateCallCount = 0;           //전투 시작 후 게임 내 update call 누적 카운트
    private UpdateCallback mUpdateCallback;
    public UpdateTurnCallback OnUpdateTurn { get; private set; }

    private ulong mElapsedTime = 0;                      //경과 시간(ms)
    private ulong mCurrentTime = 0;

    #region [ lock step ]
    private bool mWaitRelease = false;                   //Lock이 release될 때까지 대기 할지 여부
    private ulong mTurnTime = 0;                         //턴 시간(ms)
    private bool mIsLockStep = false;                     //lock 상태인지 여부
    private uint mCurrentTurnCount = 0;
    private uint mNextTurn = 0;
    private bool mUseRealTimeSince = false;               //실제 흐른 시간 사용
    private ulong mStartTime = 0;
    #endregion

    public bool UseMonotonicTime { get { return mUseRealTimeSince; } set { mUseRealTimeSince = value; } }
    public int UpdateCallCount { get { return mUpdateCallCount; } }
    public ulong CurrentElapsedTime { get { return GetElapsedMillisecond() - mStartTime; } }

    /// <summary>
    /// 시간으로 턴 구함
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public uint GetTurn(double time)
    {
        return (uint)(time * 1000 / mTurnTime);
    }

    public uint GetCurrentTurn()
    {
        return (uint)(mElapsedTime / (ulong)mTurnTime);
    }

    /// <summary>
    /// 경과 시간 ms 단위로 리턴
    /// </summary>
    /// <returns></returns>
    public ulong GetElapsedMillisecond()
    {
        return (ulong)(Time.realtimeSinceStartup * 1000);
    }

    public void Init(double fixedFrameTime, ulong turnTime, bool waitRelease, UpdateCallback callback)
    {
        mTurnTime = turnTime;
        mWaitRelease = waitRelease;
        mUpdateCallback = callback;
        mCurrentFixedDelta = fixedFrameTime;
        mFixedDeltaMillisecond = (int)(mCurrentFixedDelta * 1000);      
        mNextTurn = 1;
        mUpdateCallCount = 0;
        mCurrentTime = 0;
        mElapsedTime = 0;
        mCurrentTurnCount = 0;
    }

    public void StartCheckTime()
    {
        mStartTime = GetElapsedMillisecond();
        mCurrentTime = 0;
        mElapsedTime = 0;
    }

    public void Rollback(ulong elapsedTime)
    {
        mCurrentTime = 0;
        mElapsedTime = elapsedTime;
        mStartTime = GetElapsedMillisecond() - mElapsedTime;
    }


    public void RegistUpdateTurnCallback(UpdateTurnCallback callback)
    {
        OnUpdateTurn = callback;
    }  

    public void UpdateHandler()
    {
        SkipFrame();
    }

    /// <summary>
    /// 경과 시간 설정
    /// </summary>
    /// <param name="aElapsedTime"></param>
    public void SetElapsedTime(ulong aElapsedTime)
    {
        ulong current = CurrentElapsedTime;
        ulong delta = aElapsedTime - current;
        Debug.Log("Elapsed delta : " + delta);
        if (delta > 0 )
        {
            mStartTime -= delta;
            Debug.Log("Server Elapsed : " + aElapsedTime + " Current Elapsed : " + CurrentElapsedTime);
        }       
    }

    /// <summary>
    /// 경과 시간 Skip
    /// </summary>
    /// <param name="skipTime"></param>
    public void SkipElapsedTime(ulong skipTime)
    {
        mElapsedTime = mStartTime + skipTime;
    }

    private void SkipFrame()
    {
        //lockStep 사용중이고 LockStep 상태이면 처리 안함
        if(mWaitRelease && mIsLockStep)
        {
            return;
        }

        if (mUseRealTimeSince)
        {
            mElapsedTime = CurrentElapsedTime;      //흐른시간
        }
        else
        {
            mElapsedTime += (ulong)(Time.deltaTime * 1000);
        }

        uint elapsedTurn = (uint)(mElapsedTime / (ulong)mTurnTime);

        while (mCurrentTime < mElapsedTime)
        {
            mCurrentTime += (ulong)mFixedDeltaMillisecond;
            mCurrentTurnCount = (uint)(mCurrentTime / (ulong)mTurnTime);

            //Turn 차이에 따른 Skip 여부
            int diff = (int)(elapsedTurn - mCurrentTurnCount);
            mUpdateCallback(mCurrentFixedDelta, diff, mUpdateCallCount);
            mUpdateCallCount++;

            if (mCurrentTurnCount >= mNextTurn)
            {
                if (OnUpdateTurn != null)
                {
                    OnUpdateTurn(mCurrentTurnCount);
                }
                mIsLockStep = true;
                mNextTurn++;
            }
        }
    }

    public void UnlockStep()
    {
        mIsLockStep = false;
    }
}
