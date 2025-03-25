using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1CarObject : InspectableObject
{
    [SerializeField]
    private List< AudioClip> SFXList_ = new List<AudioClip>();
    [SerializeField]
    private Animator animator;
    protected override void OnInspect()
    {
        foreach (AudioClip clip in SFXList_)
        {
            SoundManager.Instance.EnqueueSFX(clip);
        }

        if (animator != null)
            animator.enabled = true;
    }
}
