using System.Collections;
using UnityEngine;

public class Room204DressEvent : DialogueObject
{
    [Header("Item")]
    public ItemData ScriptPage;


    [Header("Completion")]
    public RoomCompletionGate CompletionGate;


    [Header("TV")]
    public RoomTVEvent TVEvent;


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
        yield return new WaitUntil(
            () => DialogueManager.isRunning
        );

        yield return new WaitUntil(
            () => !DialogueManager.isRunning
        );


        CompleteDressEvent();
    }


    private void CompleteDressEvent()
    {
        // 이미 메인 이벤트가 완료됐다면 중복 방지
        if (CompletionGate != null &&
            CompletionGate.MainEventCompleted)
        {
            return;
        }


        // 콜시트 획득
        if (ScriptPage != null)
        {
            InventoryManager.Instance.AddItem(ScriptPage);
        }


        // 드레스 이벤트 완료
        if (CompletionGate != null)
        {
            CompletionGate.MarkMainEventCompleted();
        }


        Debug.Log("[Room204] 드레스 이벤트 완료");


        // TV를 먼저 조사했을 수도 있으므로
        // TV OFF 조건 재검사
        if (TVEvent != null)
        {
            TVEvent.CheckTVOffCondition();
        }
    }
}