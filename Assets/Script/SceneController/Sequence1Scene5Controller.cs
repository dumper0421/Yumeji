using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene5Controller : SceneController
{
    protected override void OnStopIntervalReached()
    {
        playerAnimator.enabled = true;  
        playerMoveTestLerp.enabled = true;
    }
    void Start()
    {

        base.Start();
        playerAnimator.SetFloat("DirY", 1);
        playerAnimator.enabled = false;
        StartCoroutine(StopPlayer());
    }

    void Update()
    {
        
    }
}
