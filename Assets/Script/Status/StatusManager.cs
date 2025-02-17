using UnityEngine;

public class StatusManager : Singleton<StatusManager>
{
    public Status playerStatus;

    protected override void Init()
    {
        if (playerStatus == null)
        {
            playerStatus = FindObjectOfType<Status>();
        }
    }

    public void SetPlayerStatus(Status status)
    {
        playerStatus = status;
    }

    public float CurrentHealth => playerStatus != null ? playerStatus.CurrentHealth : 0f;
    public float BaseSpeed => playerStatus != null ? playerStatus.BaseSpeed : 0f;
    public float RunSpeed => playerStatus != null ? playerStatus.RunSpeed : 0f;
}
