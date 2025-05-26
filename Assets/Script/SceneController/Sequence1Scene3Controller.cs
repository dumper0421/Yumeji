using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene3Controller : SceneController
{
    public CinemachineVirtualCameraBase cinemachineBase;
    private bool _firstEnabled = false;
    protected override void OnStopIntervalReached()
    {
        ;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && !_firstEnabled)
        {
            cinemachineBase.Follow = Player.transform;
            Player.GetComponent<SpriteRenderer>().enabled = true;
            Player.GetComponent<PlayerMove_Test_Lerp>().enabled = true;
            _firstEnabled = true;
        }
    }
}
