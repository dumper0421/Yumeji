using UnityEngine;

/// <summary>
/// 시퀀스8 씬2의 T2 접촉 트리거.
/// 기획서 4번: 자유 조작 구간의 상호작용은 이것 하나뿐이고,
/// 조사 키가 아니라 접촉 즉시 발동한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class S8S2CurtainTrigger : MonoBehaviour
{
    [SerializeField]
    private Sequence8Scene2DialogueController _controller;

    private Collider2D _collider;
    private bool _fired;

    // 켜지는 순간 이미 플레이어와 겹쳐 있었는지. 겹쳐 있었다면 한 번 나갔다 들어와야 발동한다.
    private bool _waitForExit;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 이 트리거는 연출이 전부 끝난 뒤에 켜진다.
    /// 겹친 상태로 켜지면 Unity가 OnTriggerEnter2D를 그대로 한 번 쏘기 때문에,
    /// 트리거 범위가 하루의 위치를 덮고 있으면 조작을 넘기자마자 시퀀스 9로 넘어가버린다.
    /// </summary>
    private void OnEnable()
    {
        _waitForExit = IsPlayerInside();

        if (_waitForExit)
            Debug.LogWarning(
                "[S8S2] T2가 켜지는 시점에 플레이어가 이미 트리거 안에 있다. "
                    + "트리거 위치/크기가 하루의 위치(H)를 덮고 있지 않은지 확인할 것.",
                this
            );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_fired || _waitForExit || !collision.CompareTag("Player"))
            return;

        _fired = true;

        if (_controller != null)
            _controller.OnPlayerEnteredT2();

        gameObject.SetActive(false);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            _waitForExit = false;
    }

    private bool IsPlayerInside()
    {
        if (_collider == null)
            _collider = GetComponent<Collider2D>();

        // 벽/포인트가 전부 트리거로 되어 있는 씬이라 useTriggers를 켜야 플레이어가 잡힌다
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = true;

        Collider2D[] hits = new Collider2D[8];
        int count = _collider.OverlapCollider(filter, hits);

        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null && hits[i].CompareTag("Player"))
                return true;
        }

        return false;
    }
}
