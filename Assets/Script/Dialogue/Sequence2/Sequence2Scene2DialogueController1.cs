using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum S2S2State1
{
    None
}
public class Sequence2Scene2DialogueController1 : DialogueController<S2S2State1>

{


    private ItemData selectedTicket;

    [Header("SFX")]
    [SerializeField] private AudioClip haruCarSFX;

    [SerializeField] private string StartDialogueId;
    [SerializeField] private string nextSceneName;
    private void Start()
    {
        if (!string.IsNullOrEmpty(StartDialogueId))
        {
            dialogueManager.StartDialogue(StartDialogueId);
        }
    }
    protected override void HandleOption(string text, string nextId)
    {
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == "Car_After_Movie")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);

        }
    }

    protected override void TryProgress()
    {
    }

    protected override void OnPuzzleComplete() { }

    protected override void ApplyWorldByState()
    {
    }
}