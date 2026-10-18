using System.Collections;
using UnityEngine;

public enum S7S3State
{
    None,
    ReiExited,
    Finished,
}

public class Sequence7Scene3DialogueController
    : DialogueController<S7S3State>
{
    [Header("Room 201")]
    [SerializeField] private Room201PhoneEvent room201PhoneEvent;

    [SerializeField]
    private float shotToReactionDelay = 0.15f;


    protected override void ApplyWorldByState()
    {
    }


    protected override void DialogueRunning(string dialogueId)
    {
    }


    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == "room201_phone_before_shot")
        {
            StartCoroutine(
                PlayShotAndContinueDialogue()
            );
        }
    }


    private IEnumerator PlayShotAndContinueDialogue()
    {
        // 총성 + 박수 시작
        if (room201PhoneEvent != null)
        {
            room201PhoneEvent.PlayGunshotAndApplause();
        }

        // 총성이 먼저 들리도록 아주 짧게 대기
        yield return new WaitForSecondsRealtime(
            shotToReactionDelay
        );

        // 그 다음 "헉!" 대사 시작
        dialogueManager.StartDialogue(
            "room201_phone_after_shot"
        );
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


    public void StartDialogue()
    {
    }
}