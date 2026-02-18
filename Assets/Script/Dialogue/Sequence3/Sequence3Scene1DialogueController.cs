using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum S3S1State
{
    None
}
public class Sequence3Scene1DialogueController : DialogueController<S3S1State>
{
    [SerializeField]
    private Animator _haruAnimator;
    [SerializeField]
    private Animator _kenAnimator;
    [SerializeField]
    private ExitTrigger ExitTrigger;


    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "KenHaru_16":
                _haruAnimator.enabled = true;
                _kenAnimator.enabled = true;
                _haruAnimator.GetComponent<InteractionSystem>().enabled = true; 
                _kenAnimator.SetFloat("DirX", -1);
                _kenAnimator.SetFloat("DirY", 0);
                ExitTrigger.IsClear = true;
                break;
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

    public void OnStartedDialogue(string DialogueID)
    {
        dialogueManager.StartDialogue(DialogueID);
        _haruAnimator.enabled = false;
        _kenAnimator.enabled = false;
    }
}