using UnityEngine;

public class Setting : MonoBehaviour
{
    [Header("고정 delta 시간")]
    public double fixedDeltaTime = 0.033;

    [Header("Turn time : 실행 턴 시간(ms)")]
    public ulong turnTime = 250;

    [Header("step 대기 여부")]
    public bool stepWait = false;
}
