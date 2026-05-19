using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InspectableObject : MonoBehaviour
{
    //false면 여러번 조사가능 true면 한번만
    public bool IsDisposable = false;
    public bool hasBeenInspected = false; 
    protected abstract void OnInspect();

    public virtual void TryInspect()
    {
        if (IsDisposable && hasBeenInspected)
        {
            return; 
        }

        hasBeenInspected = true;
        OnInspect();
    }

    public virtual void PlaySFX(string name) {
        ;
    }
}