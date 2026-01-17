using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimeLinePoint : MonoBehaviour
{

    public PlayableDirector Director;
    public bool IsDisposable = true;
    public bool hasBeenInspected = false;
    virtual public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (hasBeenInspected && IsDisposable) return;

        Utility.PlayDirector(Director);
        hasBeenInspected = true;
    }
}
