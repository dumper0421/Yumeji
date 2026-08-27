using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public enum S7S3State
{
    None,
    ReiExited,
    Finished,
}

public class Sequence7Scene3DialogueController : DialogueController<S7S3State>
{

    protected override void ApplyWorldByState()
    {
    }

    protected override void DialogueRunning(string dialogueId) { }

    protected override void HandleDialogueEnd(string dialogueId)
    {
    
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    public void StartDialogue()
    {
    }
}
