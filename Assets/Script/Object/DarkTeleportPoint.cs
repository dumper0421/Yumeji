using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class DarkTeleportPoint : TeleportPointLegacy
{
    public Vector3 ReturnPos;
    public float WaitSeconds = 2f;

    public CinemachineVirtualCamera ReturnCamera;
    public CinemachineVirtualCameraBase ReturnCameraBase;

    public AudioClip DarkRoomSFX;
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (!collision.CompareTag("Player")) return;
        StartCoroutine(Return(collision.gameObject));
    }

    IEnumerator Return(GameObject target)
    {
        target.GetComponent<PlayerMove_Test_Lerp>().enabled = false;
        StartCoroutine(CameraManager.Instance.CameraZoomIn(cinemachine, 5, 1, 2, 2));
        SoundManager.Instance.PlaySFX(DarkRoomSFX);
        yield return new WaitForSeconds(WaitSeconds);
        target.GetComponent<PlayerMove_Test_Lerp>().enabled = true;
        var mover = target.GetComponent<PlayerMove_Test_Lerp>();
        mover.Teleport(ReturnPos);

        controller.ChangeCinemachineCamera(ReturnCamera);
        ReturnCameraBase.Follow = target.transform;
    }


}
