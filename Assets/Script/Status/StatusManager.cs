using UnityEngine;

public class StatusManager : Singleton<StatusManager>
{
    public Status playerStatus;

     void Start()
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

    protected override void Init()
    {
    }

    public float CurrentHealth => playerStatus.CurrentHealth;
    public float BaseSpeed =>  playerStatus.BaseSpeed;
    public float RunSpeed => playerStatus.RunSpeed;
}
