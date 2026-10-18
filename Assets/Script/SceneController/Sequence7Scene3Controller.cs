using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence7Scene3Controller : SceneController
{
    [Header("Puzzle State")]
    [SerializeField] private bool room204Completed = false;
    [SerializeField] private bool room205Completed = false;

    public bool Room204Completed => room204Completed;
    public bool Room205Completed => room205Completed;


    public void SetRoom204Completed()
    {
        if (room204Completed)
            return;

        room204Completed = true;

        Debug.Log("[Sequence7-3] 204호 퍼즐 완료");
    }


    public void SetRoom205Completed()
    {
        if (room205Completed)
            return;

        room205Completed = true;

        Debug.Log("[Sequence7-3] 205호 퍼즐 완료");
    }


    protected override void OnStopIntervalReached()
    {
    }
}