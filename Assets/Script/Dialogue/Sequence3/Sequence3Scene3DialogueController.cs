using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum S3S3State
{
    None
}
public class Sequence3Scene3DialogueController : DialogueController<S3S3State>
{
    protected override void Awake()
    {
        base.Awake();
        
    }

    private void Start()
    {
        dialogueManager.StartDialogue("Haru_01");
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
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
    public override void OnStartedDialogue(string DialogueID)
    {
        base.OnStartedDialogue(DialogueID);
    }
}
