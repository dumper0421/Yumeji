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


    public void OnStartedDialogue(
        string dialogueID
    )
    {
        dialogueManager.StartDialogue(
            dialogueID
        );
    }


    protected override void ApplyWorldByState()
    {
    }


    protected override void HandleDialogueEnd(
        string dialogueId
    )
    {
        Debug.Log(
            $"대화 종료 ID: {dialogueId}"
        );


        // =================================================
        // 인트로 끝
        // =================================================

        if (
            dialogueId ==
            "S6S2_Intro_Haru9"
        )
        {
            sceneController
                .FinishIntroSequence();

            return;
        }


        // =================================================
        // 케이 접근 대화 끝
        // → 첫 번째 총격
        // =================================================

        if (
            dialogueId ==
            "S6S2_KeiApproach"
        )
        {
            sceneController
                .TurnKeiDown();

            return;
        }


        // =================================================
        // 총격 이후 대화 끝
        //
        // 케이:
        // "다시 한번 해봐."
        //
        // → 두 번째 총격
        // =================================================

        if (
            dialogueId ==
            "S6S2_AfterShot_Kei2"
        )
        {
            sceneController
                .StartFinalShotSequence();

            return;
        }
    }


    protected override void HandleOption(
        string text,
        string nextId
    )
    {
    }


    protected override void OnPuzzleComplete()
    {
    }


    protected override void TryProgress()
    {
    }
}