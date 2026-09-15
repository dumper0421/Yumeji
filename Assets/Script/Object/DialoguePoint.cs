using UnityEngine;

public class DialoguePoint : MonoBehaviour
{
    public string StartDialogue;
    public DialogueManager DialogueManager;
    public bool DisablePoint = false;
    public bool isDisposable = false;

    private Collider2D _collider;
    private ContactFilter2D _filter;
    private readonly Collider2D[] _overlapBuffer = new Collider2D[8];

    // 직전 칸 도착 시점에 플레이어가 범위 안에 있었는지. 들어오는 순간(false -> true)에만 발동시킨다.
    private bool _wasInside = false;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();

        _filter = new ContactFilter2D();
        _filter.NoFilter();
        _filter.useTriggers = true;
    }

    private void OnEnable()
    {
        _wasInside = false;
        PlayerMove_Test_Lerp.OnTileArrived += OnTileArrived;
    }

    private void OnDisable() => PlayerMove_Test_Lerp.OnTileArrived -= OnTileArrived;

    // 칸 이동이 완전히 끝났을 때만 발동합니다.
    private void OnTileArrived(Vector2 playerPos)
    {
        bool isInside = IsPlayerInside();
        bool entered = isInside && !_wasInside;
        _wasInside = isInside;

        // 나갈 때 / 범위 안에서 계속 움직일 때는 발동하지 않는다.
        if (!entered)
            return;
        if (DisablePoint)
            return;
        if (DialogueManager.transform.GetChild(2).gameObject.activeSelf)
            return;

        DialogueManager.StartDialogue(StartDialogue);

        if (isDisposable)
            DisablePoint = true;
    }

    // OnTriggerEnter/Exit 콜백은 물리 스텝 기준이라 칸 도착 시점의 상태와 한 프레임 어긋난다.
    // (이동 중에 이미 Enter가 들어오고, 벗어나는 중에도 Exit가 아직 안 들어옴)
    // 그래서 도착한 그 순간의 위치로 직접 겹침 검사를 한다.
    private bool IsPlayerInside()
    {
        if (_collider == null)
            return false;

        // 코루틴에서 방금 옮긴 transform을 물리에 반영한 뒤 검사한다.
        Physics2D.SyncTransforms();

        int count = _collider.OverlapCollider(_filter, _overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            if (_overlapBuffer[i] != null && _overlapBuffer[i].CompareTag("Player"))
                return true;
        }

        return false;
    }
}
