using System.Collections;
using UnityEngine;

/// <summary>
/// 기믹 1) 마네킹 왈츠 — 마네킹 한 쌍(세트)의 이동/정지/충돌 처리.
/// - 웨이포인트를 따라 일정한 동선으로 순환 이동
/// - 카메라 촬영(IFlashable) 시 즉시 정지: 2초 정지 → 1초 좌우 흔들림(예고 SFX) → 이동 재개
/// - 정지 상태에서 재촬영은 무시, 충돌 판정은 유지
/// - 플레이어와 콜라이더 접촉 시 즉시 게임오버
/// - '씬 시작' 대사 종료 후 Activate()로 활성화 (Sequence7Scene2DialogueController가 호출)
/// </summary>
public class WaltzMannequinSet : MonoBehaviour, IFlashable
{
    [Header("Movement")]
    [Tooltip("이동 동선 웨이포인트(p1, p2, ... 순서). 마지막에서 처음으로 순환")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _moveSpeed = 2f;

    [Header("Freeze (촬영)")]
    [Tooltip("흔들림 전까지 완전히 정지하는 시간")]
    [SerializeField] private float _stillDuration = 2f;

    [Tooltip("이동 재개 전 좌우로 흔들리는 시간")]
    [SerializeField] private float _shakeDuration = 1f;

    [SerializeField] private float _shakeAmplitude = 0.06f;
    [SerializeField] private float _shakeFrequency = 10f;

    [Tooltip("정지 해제 전 흔들림과 함께 재생 (7-2_SFX_Mannequin_move_notice)")]
    [SerializeField] private AudioClip _moveNoticeSfx;

    [Header("Dependencies")]
    [Tooltip("대화 중에는 이동/충돌 판정을 일시 정지")]
    [SerializeField] private DialogueManager _dialogueManager;

    private bool _active = false;
    private bool _frozen = false;
    private int _waypointIndex = 0;

    private Collider2D _collider;
    private Collider2D _playerCollider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerCollider = player.GetComponent<Collider2D>();
    }

    /// <summary>씬 시작 대사 종료 직후 호출되어 왈츠를 시작한다.</summary>
    public void Activate()
    {
        _active = true;
    }

    private bool IsPaused =>
        !_active || _frozen || (_dialogueManager != null && _dialogueManager.isRunning);

    private void Update()
    {
        // 플레이어에게 RB2D가 없어도 확실히 판정되도록 물리 이벤트 대신 직접 겹침 검사
        CheckPlayerOverlap();

        if (IsPaused) return;
        if (_waypoints == null || _waypoints.Length == 0) return;

        Transform target = _waypoints[_waypointIndex];
        if (target == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            _moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.01f)
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
    }

    // ---------- 충돌 (정지 상태여도 판정 유지) ----------
    private void CheckPlayerOverlap()
    {
        if (!_active) return;
        if (_dialogueManager != null && _dialogueManager.isRunning) return;
        if (_collider == null || _playerCollider == null) return;

        if (_collider.bounds.Intersects(_playerCollider.bounds))
            UIManager.Instance?.OpenGameOverUI();
    }

    // ---------- 촬영 (IFlashable) ----------
    public void OnPhotoTaken(bool isEnhanced)
    {
        if (!_active) return;

        // 정지 상태의 마네킹은 재촬영을 무시한다
        if (_frozen) return;

        StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        _frozen = true;

        // 1) 제자리 정지
        yield return new WaitForSeconds(_stillDuration);

        // 2) 이동 재개 예고: 좌우 흔들림 + SFX
        if (_moveNoticeSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(_moveNoticeSfx);

        Vector3 basePos = transform.position;
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Mathf.Sin(elapsed * _shakeFrequency * Mathf.PI * 2f) * _shakeAmplitude;
            transform.position = basePos + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }
        transform.position = basePos;

        // 3) 진행하던 동선을 이어서 이동
        _frozen = false;
    }

    private void OnTriggerEnter2D(Collider2D collision) => HandleHit(collision);

    private void OnCollisionEnter2D(Collision2D collision) => HandleHit(collision.collider);

    private void HandleHit(Collider2D collision)
    {
        if (!_active) return;
        if (_dialogueManager != null && _dialogueManager.isRunning) return;
        if (!collision.CompareTag("Player")) return;

        UIManager.Instance?.OpenGameOverUI();
    }
}
