using UnityEngine;

public enum S6S2State
{
    None,
    IntroComplete
}

public class Sequence6Scene2DialogueController
    : DialogueController<S6S2State>
{
    [SerializeField]
    private Sequence6Scene2Controller sceneController;

    public void OnStartedDialogue(string dialogueID)
    {
        dialogueManager.StartDialogue(dialogueID);
    }

    protected override void ApplyWorldByState()
    {
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        Debug.Log($"대화 종료 ID: {dialogueId}");

        if (dialogueId == "S6S2_Intro_Haru9")
        {
            sceneController.FinishIntroSequence();
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