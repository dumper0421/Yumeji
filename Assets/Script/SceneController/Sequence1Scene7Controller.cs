using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class Sequence1Scene7Controller : SceneController
{

    protected override void Start()
    {
        SoundManager.Instance.PlaySFX(bgm);

    }
    protected override void OnStopIntervalReached()
    {

    }


}
