using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene5Controller : SceneController
{
    [SerializeField]
    Sequence1Scene5DialogueController _dialogueController;

    protected override void OnStopIntervalReached()
    {
        playerAnimator.enabled = true;
        playerMoveTestLerp.enabled = true;
    }

    void Start()
    {
        base.Start();
        if (_dialogueController.state == S1S5State.None)
        {
            playerAnimator.SetFloat("DirY", 1);
            playerAnimator.enabled = false;
            StartCoroutine(StopPlayer());
        }
    }

    void Update() { }
}
