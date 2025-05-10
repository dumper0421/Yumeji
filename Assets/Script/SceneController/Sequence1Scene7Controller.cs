using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class Sequence1Scene7Controller : SceneController
{

    public List<CinemachineVirtualCamera> cinemachineCameras;
    private void Start()
    {


    }
    protected override void OnStopIntervalReached()
    {

    }

    public void ChangeCinemachineCamera(CinemachineVirtualCamera target)
    {
        foreach(var cam in cinemachineCameras)
        {
            if (target == cam)
                target.gameObject.SetActive(true);
            else
                cam.gameObject.SetActive(false);
        }
    }

}
