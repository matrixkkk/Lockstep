using UnityEngine;

/// <summary>
/// 고정 프레임 처리기
/// </summary>
public class LockstepSyncHandler
{
    public delegate void UpdateCallback(double delta, int turnDelta, int updateCount);
    public delegate void UpdateTurnCallback(uint turnCount);

    private double _currentFixedDelta;          //고정 프레임 delta 시간(s)
    private int _fixedDeltaMillisecond;         //fixed delta ms 단위
    private int _updateCallCount = 0;           //전투 시작 후 게임 내 update call 누적 카운트
    private UpdateCallback _updateCallback;
    private UpdateTurnCallback OnUpdateTurn { get; set; }

    private ulong _elapsedTime = 0;                      //경과 시간(ms)
    private ulong _currentTime = 0;                      //현재 시간(ms)

    #region [ lock step ]
    private bool _useLockstep = false;                   //Lock이 release될 때까지 대기 할지 여부
    private int _turnTime = 0;                           //턴 시간(ms)
    private bool _isLockStep = false;                    //lock 상태인지 여부
    private uint _currentTurnCount = 0;                  //현재 턴 개수
    private uint _nextTurn = 0;
    private bool _useRealTimeSinceSince = false;          //실제 흐른 시간 사용
    private ulong _startTime = 0;                         //시작 시간
    #endregion

    public bool UseRealTimeSince { get => _useRealTimeSinceSince; private set => _useRealTimeSinceSince = value; }
    public int UpdateCallCount => _updateCallCount;
    public ulong CurrentElapsedTime => GetRealtimeSinceStartup() - _startTime;

    /// <summary>
    /// 시간으로 턴 구함
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public uint GetTurn(double time)
    {
        return (uint)(time * 1000 / _turnTime);
    }

    /// <summary>
    /// 현재 턴 가져오기
    /// </summary>
    /// <returns></returns>
    public uint GetCurrentTurn()
    {
        return (uint)(_elapsedTime / (ulong)_turnTime);
    }

    /// <summary>
    /// 경과 시간 ms 단위로 리턴
    /// </summary>
    /// <returns></returns>
    private ulong GetRealtimeSinceStartup()
    {
        return (ulong)(Time.realtimeSinceStartup * 1000);
    }

    public void Init(double fixedFrameTime, int turnTime, bool useLockStep, UpdateCallback callback)
    {
        _turnTime = turnTime;
        _useLockstep = useLockStep;
        _updateCallback = callback;
        _currentFixedDelta = fixedFrameTime;
        _fixedDeltaMillisecond = (int)(_currentFixedDelta * 1000);      
        _nextTurn = 1;
        _updateCallCount = 0;
        _currentTime = 0;
        _elapsedTime = 0;
        _currentTurnCount = 0;
    }
    
    /// <summary>
    /// 시간 체크 시작
    /// </summary>
    public void StartCheckTime()
    {
        _startTime = GetRealtimeSinceStartup();
        _currentTime = 0;
        _elapsedTime = 0;
    }
    
    /// <summary>
    /// 롤백 시 경과 시간으로 재계산
    /// </summary>
    /// <param name="elapsedTime"></param>
    public void Rollback(ulong elapsedTime)
    {
        _currentTime = 0;
        _elapsedTime = elapsedTime;
        _startTime = GetRealtimeSinceStartup() - _elapsedTime;
    }

    public void UpdateHandler()
    {
        //lockStep 사용중이고 LockStep 상태이면 처리 안함
        if(_useLockstep && _isLockStep)
        {
            return;
        }
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
            _startTime -= delta;
            Debug.Log("Server Elapsed : " + aElapsedTime + " Current Elapsed : " + CurrentElapsedTime);
        }       
    }

    /// <summary>
    /// 경과 시간 Skip
    /// </summary>
    /// <param name="skipTime"></param>
    public void SkipElapsedTime(ulong skipTime)
    {
        _elapsedTime = _startTime + skipTime;
    }

    private void SkipFrame()
    {
        if (_useRealTimeSinceSince)
        {
            _elapsedTime = CurrentElapsedTime;      //흐른시간
        }
        else
        {
            _elapsedTime += (ulong)(Time.deltaTime * 1000);
        }

        uint elapsedTurn = (uint)(_elapsedTime / (ulong)_turnTime);

        while (_currentTime < _elapsedTime)
        {
            _currentTime += (ulong)_fixedDeltaMillisecond;
            _currentTurnCount = (uint)(_currentTime / (ulong)_turnTime);

            //Turn 차이에 따른 Skip 여부
            int turnDelta = (int)(elapsedTurn - _currentTurnCount);
            _updateCallback(_currentFixedDelta, turnDelta, _updateCallCount);
            _updateCallCount++;

            if (_currentTurnCount >= _nextTurn)
            {
                if (OnUpdateTurn != null)
                {
                    OnUpdateTurn(_currentTurnCount);
                }
                _isLockStep = true;
                _nextTurn++;
            }
        }
    }

    public void UnlockStep()
    {
        _isLockStep = false;
    }
}
