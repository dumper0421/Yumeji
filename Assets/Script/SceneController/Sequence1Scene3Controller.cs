using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene3Controller : SceneController
{
    public CinemachineVirtualCameraBase cinemachineBase;
    protected override void OnStopIntervalReached()
    {
        ;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            cinemachineBase.Follow = Player.transform;
            Debug.Log("F 키 입력 감지");
            Player.GetComponent<SpriteRenderer>().enabled = true;
            Player.GetComponent<PlayerMove_Test_Lerp>().enabled = true;
        }
    }
}
