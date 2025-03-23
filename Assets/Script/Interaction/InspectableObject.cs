using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InspectableObject : MonoBehaviour
{
    //false면 여러번 조사가능 true면 한번만
    public bool IsDisposable = false;
    private bool hasBeenInspected = false; 
    protected abstract void OnInspect();

    public void TryInspect()
    {
        if (IsDisposable && hasBeenInspected)
        {
            return; 
        }

        OnInspect();
        hasBeenInspected = true;
    }

    public virtual void PlaySFX(string name) {
        ;
    }
}