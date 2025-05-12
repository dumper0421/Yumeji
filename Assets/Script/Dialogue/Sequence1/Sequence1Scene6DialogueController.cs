using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum S1S6State
{
    None
}

public class Sequence1Scene6DialogueController : DialogueController<S1S6State>
{
    public AstarEnemy Enemy;
    public AudioClip Bgm;
    public Animator PlayAnimaotr;
    [SerializeField] private AudioClip _appearedSFX;

    protected override void HandleDialogueEnd(string dialogueId)
    {
       if (dialogueId == "CrewMember")
        {
            Enemy.isMove = true;
            SoundManager.Instance.PlayBGM(Bgm);
            SoundManager.Instance.PlaySFX(_appearedSFX);
            PlayAnimaotr.enabled = true;
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
        
    }

    protected override void OnPuzzleComplete()
    {
        
    }

    protected override void TryProgress()
    {


    }
}
