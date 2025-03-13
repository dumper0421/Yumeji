using UnityEngine;

public class StatusManager : Singleton<StatusManager>
{
    public Status playerStatus;
    protected override bool IsGlobal => false;

    protected override void Awake()
    {
        base.Awake();

        if (playerStatus == null)
        {
            playerStatus = FindObjectOfType<Status>();
        }
    }
    public void SetPlayerStatus(Status status)
    {
        playerStatus = status;
    }

    protected override void OnSingletonInit()
    {
    }

    public float CurrentHealth => playerStatus.CurrentHealth;
    public float BaseSpeed =>  playerStatus.BaseSpeed;
    public float RunSpeed => playerStatus.RunSpeed;
}
