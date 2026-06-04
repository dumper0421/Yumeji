using UnityEngine;

public class DialoguePoint : MonoBehaviour
{
    public string StartDialogue;
    public DialogueManager DialogueManager;
    public bool DisablePoint = false;
    public bool isDisposable = false;

    private bool _playerInTrigger = false;

    private void OnEnable() => PlayerMove_Test_Lerp.OnTileArrived += OnTileArrived;

    private void OnDisable() => PlayerMove_Test_Lerp.OnTileArrived -= OnTileArrived;

    // 트리거는 플레이어가 타일 위에 있는지만 추적합니다.
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            _playerInTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            _playerInTrigger = false;
    }

    // 칸 이동이 완전히 끝났을 때만 발동합니다.
    private void OnTileArrived(Vector2 playerPos)
    {
        if (!_playerInTrigger)
            return;
        if (DisablePoint)
            return;
        if (DialogueManager.transform.GetChild(2).gameObject.activeSelf)
            return;

        DialogueManager.StartDialogue(StartDialogue);

        if (isDisposable)
            DisablePoint = true;
    }
}
