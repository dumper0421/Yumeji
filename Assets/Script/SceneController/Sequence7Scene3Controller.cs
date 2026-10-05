using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence7Scene3Controller : SceneController
{
    [Header("Puzzle State")]
    [SerializeField] private bool room204Completed = false;

    public bool Room204Completed => room204Completed;

    public void SetRoom204Completed()
    {
        room204Completed = true;

        Debug.Log("[Sequence7-3] 204호 퍼즐 완료");
    }

    protected override void OnStopIntervalReached()
    {
    }
}