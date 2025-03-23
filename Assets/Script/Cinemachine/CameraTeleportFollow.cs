using UnityEngine;
using Cinemachine;

public class CameraTeleportFollow : MonoBehaviour
{
    public Transform Player;               
    public float TeleportThreshold = 5f;     

    private Vector3 lastPlayerPosition_;     
    private CinemachineBrain cinemachineBrain_;

    void Start()
    {
        if (Player == null)
        {
            Debug.LogError("Player not assigned!");
            return;
        }
        lastPlayerPosition_ = Player.position;
        cinemachineBrain_ = Camera.main.GetComponent<CinemachineBrain>();
    }

    void Update()
    {
        if (Player == null || cinemachineBrain_ == null)
            return;

        float distance = Vector3.Distance(Player.position, lastPlayerPosition_);
        if (distance > TeleportThreshold)
        {
            cinemachineBrain_.enabled = false;
            Camera.main.transform.position = Player.position;
            cinemachineBrain_.enabled = true;
        }
        lastPlayerPosition_ = Player.position;
    }
}
