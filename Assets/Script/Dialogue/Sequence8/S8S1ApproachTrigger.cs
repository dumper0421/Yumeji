using UnityEngine;

/// <summary>
/// 시퀀스8 씬1의 T1 접근 트리거.
/// 플레이어가 들어오면 루나 대화 이벤트를 시작하고 스스로 꺼진다.
/// (구현 문서 4번: T1은 한 번만 발동하며 이벤트 종료 뒤 재활성화되지 않는다)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class S8S1ApproachTrigger : MonoBehaviour
{
    [SerializeField]
    private Sequence8Scene1DialogueController _controller;

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
    /// 이 트리거는 하루의 첫 대사가 끝난 뒤에 켜진다.
    /// 겹친 상태로 켜지면 Unity가 OnTriggerEnter2D를 그대로 한 번 쏘기 때문에,
    /// 트리거 범위가 플레이어 시작 위치를 덮고 있으면 조작을 넘기자마자 발동해버린다.
    /// 그 경우는 "들어왔다"고 볼 수 없으므로 밖으로 나갈 때까지 발동을 미룬다.
    /// </summary>
    private void OnEnable()
    {
        _waitForExit = IsPlayerInside();

        if (_waitForExit)
            Debug.LogWarning(
                "[S8S1] T1이 켜지는 시점에 플레이어가 이미 트리거 안에 있다. "
                    + "트리거 위치/크기가 플레이어 시작 지점을 덮고 있지 않은지 확인할 것.",
                this
            );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_fired || _waitForExit || !collision.CompareTag("Player"))
            return;

        _fired = true;

        if (_controller != null)
            _controller.OnPlayerEnteredT1();

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
