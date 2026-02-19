using System.Collections;
using Cinemachine;
using UnityEngine;

public class TeleportPointLegacy : MonoBehaviour
{
    public Vector3 TargetPoint;
    public CinemachineVirtualCamera cinemachine;
    public CinemachineVirtualCameraBase cinemachineBase;
    public SceneController controller;


    virtual public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var col = gameObject.GetComponent<Collider2D>();
        collision.transform.position = TargetPoint;
        var mover = collision.GetComponent<PlayerMove_Test_Lerp>();
        if (mover != null)
        mover.Teleport(TargetPoint);
        controller.ChangeCinemachineCamera(cinemachine);
        cinemachineBase.Follow = collision.transform;
    }
}
