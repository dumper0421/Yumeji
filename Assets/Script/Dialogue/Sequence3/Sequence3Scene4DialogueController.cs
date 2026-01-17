using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
public enum S3S4State
{
    None
}
public class Sequence3Scene4DialogueController : DialogueController<S3S4State>
{

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
    }

    public void StopPlayer()
    {
    }

    public override void OnStartedDialogue(string DialogueID)
    {
    }

    protected override void HandleOption(string text, string nextId)
    {
    }

    protected override void TryProgress()
    {
    }

    protected override void OnPuzzleComplete()
    {
    }

    private void PlayDirector(PlayableDirector _director)
    {

    }

    
}
