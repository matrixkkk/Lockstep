using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 샘플 씬
/// </summary>
public class SampleScene : MonoBehaviour
{
    public Setting setting;                 //설정 정보
    public Text messageText;                //message text
    public Text settingText;
    public Button unlockButton;

    private LockstepSyncHandler syncHandler;

    // Start is called before the first frame update
    void Start()
    {
        syncHandler = new LockstepSyncHandler();
        syncHandler.Init(setting.fixedDeltaTime, setting.turnTime, setting.stepWait, OnUpdateCallback);
        syncHandler.RegistUpdateTurnCallback(OnUpdateTurn);
        syncHandler.StartCheckTime();

        settingText.text = "Setting : " + "fixed : " + setting.fixedDeltaTime + "s, Turn time : " + setting.turnTime + "ms"; 
    }

    // Update is called once per frame
    void Update()
    {
        syncHandler.UpdateHandler();
    }

    private void OnUpdateCallback(double delta, int diff, int updateCount)
    {
        string msg = "Call Update : " + delta + ", update count : " + updateCount;
        Debug.Log(msg);
        messageText.text = msg;
    }

    /// <summary>
    /// turn 이 실행되면 호출됨
    /// </summary>
    /// <param name="turn"></param>
    private void OnUpdateTurn(uint turn)
    {
        if(setting.stepWait)
        {
            unlockButton.gameObject.SetActive(true);
        }
    }

    public void OnClickUnLockButton()
    {
        syncHandler.UnlockStep();
        unlockButton.gameObject.SetActive(false);
    }
}
