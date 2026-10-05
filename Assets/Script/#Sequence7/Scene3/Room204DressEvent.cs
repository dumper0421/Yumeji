using System.Collections;
using UnityEngine;

public class Room204DressEvent : DialogueObject
{
    [Header("Item")]
    public ItemData ScriptPage;

    [Header("Scene Controller")]
    public Sequence7Scene3Controller SceneController;

    [Header("Room 204")]
    public Collider2D ExitBlockCollider;
    public GameObject Room204HallLight;

    [Header("TV")]
    public Room204TVEvent TVEvent;


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
        yield return new WaitUntil(() => DialogueManager.isRunning);
        yield return new WaitUntil(() => !DialogueManager.isRunning);

        CompleteDressEvent();
    }

    private void CompleteDressEvent()
    {
        if (SceneController.Room204Completed)
            return;

        // 콜시트 조각 - 캐스팅 획득
        InventoryManager.Instance.AddItem(ScriptPage);

        // 204호 퍼즐 완료
        SceneController.SetRoom204Completed();

        // 출구 콜라이더 해제
        if (ExitBlockCollider != null)
            ExitBlockCollider.enabled = false;

        // 원맵 204호 문 앞 빛 제거
        if (Room204HallLight != null)
            Room204HallLight.SetActive(false);

        // TV 조건 다시 검사
        if (TVEvent != null)
            TVEvent.CheckTVOffCondition();

        Debug.Log("[Room204] 드레스 이벤트 완료");
    }
}