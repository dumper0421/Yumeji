using System.Collections;
using Cinemachine;
using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    public Vector3 TargetPoint;
    public CinemachineVirtualCamera cinemachine;
    public CinemachineVirtualCameraBase cinemachineBase;
    public Sequence1Scene7Controller controller;

    public bool Stop = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || Stop) return;

        var col = gameObject.GetComponent<Collider2D>();
        collision.transform.position = TargetPoint;

        controller.ChangeCinemachineCamera(cinemachine);
        cinemachineBase.Follow = collision.transform;
        Debug.Log("Á¢±Ù");

        StartCoroutine(StopTeleport());

    }

    IEnumerator StopTeleport()
    {
        Stop = true;
        yield return new WaitForSeconds(0.5f);
        Stop = false;
    }

}
