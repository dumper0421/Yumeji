using System.Collections;
using UnityEngine;

public class Room205CallSheetEvent : DialogueObject
{
    [Header("Item")]
    public ItemData ScriptPage;


    [Header("Completion")]
    public RoomCompletionGate CompletionGate;


    [Header("TV")]
    public RoomTVEvent TVEvent;


    [Header("Sparkle")]
    public SpriteRenderer SparkleRenderer;


    public override void TryInspect()
    {
        if (DialogueManager.isRunning)
            return;

        base.TryInspect();
    }


    protected override void OnInspect()
    {
        if (DialogueManager.isRunning)
            return;


        DialogueManager.StartDialogue(StartDialogue);

        StartCoroutine(WaitForDialogueEnd());
    }


    private IEnumerator WaitForDialogueEnd()
    {
        // 대화 시작 대기
        yield return new WaitUntil(
            () => DialogueManager.isRunning
        );

        // 대화 종료 대기
        yield return new WaitUntil(
            () => !DialogueManager.isRunning
        );


        CompleteCallSheetEvent();
    }


    private void CompleteCallSheetEvent()
    {
        // 중복 획득 방지
        if (CompletionGate != null &&
            CompletionGate.MainEventCompleted)
        {
            return;
        }


        // 마지막 촬영의 기억 콜시트 획득
        if (ScriptPage != null)
        {
            InventoryManager.Instance.AddItem(ScriptPage);
        }


        // 스파클 이벤트 완료
        if (CompletionGate != null)
        {
            CompletionGate.MarkMainEventCompleted();
        }


        // 조사 완료 후 스파클 이미지 숨기기
        if (SparkleRenderer != null)
        {
            SparkleRenderer.enabled = false;
        }


        Debug.Log("[Room205] 스파클 / 콜시트 이벤트 완료");


        // TV를 먼저 조사했을 수도 있으므로
        // TV 조건 재확인
        if (TVEvent != null)
        {
            TVEvent.CheckTVOffCondition();
        }
    }
}