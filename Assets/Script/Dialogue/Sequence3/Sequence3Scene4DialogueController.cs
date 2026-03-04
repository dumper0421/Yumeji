using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Rendering.LookDev;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
public enum S3S4State
{
    None
}
public class Sequence3Scene4DialogueController : DialogueController<S3S4State>
{
    [SerializeField] private ReiMoveController layDialogueMover;


    protected override void Awake()
    {
        base.Awake();
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == "Lay_Dialogue04")
        {
            if (layDialogueMover == null)
            {
                Debug.LogWarning("[S3S4] layDialogueMover가 비었다.");
                return;
            }

            layDialogueMover.Play();
        }
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

    protected override void ApplyWorldByState()
    {
    }
}
