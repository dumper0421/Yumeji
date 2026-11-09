using System.Collections;
using UnityEngine;

public class Room201CallSheetEvent : DialogueObject
{
    [Header("Item")]
    [SerializeField] private ItemData ScriptPage;


    [Header("Completion")]
    [SerializeField] private RoomCompletionGate completionGate;


    [Header("Sparkle")]
    [SerializeField] private SpriteRenderer sparkleRenderer;
    [SerializeField] private Collider2D sparkleCollider;


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


        StartCoroutine(
            WaitForDialogueEnd()
        );
    }


    private IEnumerator WaitForDialogueEnd()
    {
        yield return new WaitUntil(
            () => DialogueManager.isRunning
        );


        yield return new WaitUntil(
            () => !DialogueManager.isRunning
        );


        CompleteCallSheetEvent();
    }


    private void CompleteCallSheetEvent()
    {
        // 중복 획득 방지
        if (completionGate != null &&
            completionGate.MainEventCompleted)
        {
            return;
        }


        // 콜시트 조각 - 단 한번의 상영
        if (ScriptPage != null)
        {
            InventoryManager.Instance.AddItem(
                ScriptPage
            );
        }


        // 201호 메인 이벤트 완료
        if (completionGate != null)
        {
            completionGate.MarkMainEventCompleted();
        }


        // 반짝이 제거
        if (sparkleRenderer != null)
        {
            sparkleRenderer.enabled = false;
        }


        if (sparkleCollider != null)
        {
            sparkleCollider.enabled = false;
        }


        Debug.Log(
            "[Room201] 콜시트 - 단 한번의 상영 획득"
        );
    }
}