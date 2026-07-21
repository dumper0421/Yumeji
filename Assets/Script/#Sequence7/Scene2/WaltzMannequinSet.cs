using System.Collections;
using UnityEngine;

/// <summary>
/// 기믹 1) 마네킹 왈츠 — 마네킹 한 쌍(세트)의 이동/정지/충돌 처리.
/// - 웨이포인트를 따라 일정한 동선으로 순환 이동 (포인트마다 대기 시간 지정 가능)
/// - 카메라 촬영(IFlashable) 시 즉시 정지: 2초 정지 → 1초 좌우 흔들림(예고 SFX) → 이동 재개
/// - 정지 상태에서 재촬영은 무시, 충돌 판정은 유지
/// - 플레이어와 콜라이더 접촉 시 즉시 게임오버
/// - '씬 시작' 대사 종료 후 Activate()로 활성화 (Sequence7Scene2DialogueController가 호출)
/// </summary>
public class WaltzMannequinSet : MonoBehaviour, IFlashable
{
    [Header("동선 (Scene 뷰에 노란 선으로 표시됩니다)")]
    [Tooltip("이동할 포인트들을 순서대로 넣으세요. 마지막 포인트에서 첫 포인트로 돌아갑니다")]
    [SerializeField] private Transform[] _waypoints;

    [Tooltip("이동 속도 (숫자가 클수록 빠름). 기본 2")]
    [Range(0.2f, 10f)]
    [SerializeField] private float _moveSpeed = 2f;

    [Tooltip("체크하면 마지막 포인트에서 왔던 길을 되돌아옵니다 (왕복)")]
    [SerializeField] private bool _pingPong = false;

    [Header("촬영으로 멈추기")]
    [Tooltip("완전히 멈춰 있는 시간(초). 기획서 기준 2초")]
    [Range(0f, 10f)]
    [SerializeField] private float _stillDuration = 2f;

    [Tooltip("다시 움직이기 전 흔들리는 시간(초). 기획서 기준 1초")]
    [Range(0f, 5f)]
    [SerializeField] private float _shakeDuration = 1f;

    [SerializeField] private float _shakeAmplitude = 0.06f;
    [SerializeField] private float _shakeFrequency = 10f;

    [Tooltip("정지 해제 전 흔들림과 함께 재생 (7-2_SFX_Mannequin_move_notice)")]
    [SerializeField] private AudioClip _moveNoticeSfx;

    [Header("테스트")]
    [Tooltip("체크하면 시작 대사를 기다리지 않고 바로 움직입니다 (테스트용, 최종 빌드에서는 해제)")]
    [SerializeField] private bool _startWithoutDialogue = false;

    [Tooltip("체크하면 위의 속도·시간 값 대신 저장된 설정 파일(S7S2GimmickSettings)의 값을 사용합니다.\n" +
             "이 마네킹만 다른 속도로 움직이게 하려면 체크를 해제하세요")]
    [SerializeField] private bool _useSharedSettings = true;

    [Header("연결")]
    [Tooltip("대화 중에는 이동/충돌 판정을 일시 정지")]
    [SerializeField] private DialogueManager _dialogueManager;

    private bool _active = false;
    private bool _frozen = false;
    private bool _waiting = false;
    private int _waypointIndex = 0;
    private int _direction = 1;

    private Collider2D _collider;
    private Collider2D _playerCollider;

    // ---------- 테스트 패널에서 실시간으로 바꾸는 값 ----------
    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
    public float StillDuration { get => _stillDuration; set => _stillDuration = value; }
    public float ShakeDuration { get => _shakeDuration; set => _shakeDuration = value; }
    public bool IsFrozen => _frozen;
    public bool IsActive => _active;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerCollider = player.GetComponent<Collider2D>();

        ApplySettings();
    }

    /// <summary>
    /// 저장된 수치 설정(S7S2GimmickSettings.asset)이 있으면 그 값을 사용한다.
    /// 설정 파일이 없으면 인스펙터에 입력된 값을 그대로 쓴다.
    /// </summary>
    private void ApplySettings()
    {
        if (!_useSharedSettings) return;

        var settings = S7S2GimmickSettings.Get();
        if (settings == null) return;

        _moveSpeed = settings.waltzMoveSpeed;
        _stillDuration = settings.waltzStillDuration;
        _shakeDuration = settings.waltzShakeDuration;
    }

    private void Start()
    {
        if (_startWithoutDialogue)
            Activate();
    }

    private void OnDisable()
    {
        // 코루틴이 중간에 끊기면 정지/대기 상태가 남아 다시 켰을 때 안 움직이므로 초기화
        StopAllCoroutines();
        _frozen = false;
        _waiting = false;
    }

    /// <summary>씬 시작 대사 종료 직후 호출되어 왈츠를 시작한다.</summary>
    public void Activate()
    {
        _active = true;
    }

    private bool DialogueBlocking =>
        !S7S2Test.IgnoreDialoguePause
        && _dialogueManager != null
        && _dialogueManager.isRunning;

    private bool IsPaused => !_active || _frozen || _waiting || DialogueBlocking;

    /// <summary>테스트 패널용: 지금 안 움직이는 이유를 한 줄로 알려준다.</summary>
    public string GetBlockReason()
    {
        if (!gameObject.activeSelf) return "꺼져 있음";
        if (_waypoints == null || _waypoints.Length == 0) return "동선 포인트 없음!";

        int emptyCount = 0;
        foreach (var wp in _waypoints)
        {
            if (wp == null) emptyCount++;
        }
        if (emptyCount > 0) return $"포인트 {emptyCount}칸 비어있음!";

        if (!_active) return "시작 안 됨";
        if (DialogueBlocking) return "대사 재생 중";
        if (_frozen) return "촬영으로 정지";
        if (_waiting) return "포인트에서 대기";
        return "이동 중";
    }

    public int WaypointCount => _waypoints != null ? _waypoints.Length : 0;

    private void Update()
    {
        // 플레이어에게 RB2D가 없어도 확실히 판정되도록 물리 이벤트 대신 직접 겹침 검사
        CheckPlayerOverlap();

        if (IsPaused) return;
        if (_waypoints == null || _waypoints.Length == 0) return;

        Transform target = FindNextValidWaypoint();
        if (target == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            _moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.01f)
            OnArrived(target);
    }

    /// <summary>
    /// 현재 목표 포인트를 반환한다. 포인트가 삭제되어 비어 있으면
    /// 그 자리를 건너뛰고 다음 포인트로 넘어간다. (멈춰버리지 않도록)
    /// 전부 비어 있으면 null.
    /// </summary>
    private Transform FindNextValidWaypoint()
    {
        for (int attempt = 0; attempt < _waypoints.Length; attempt++)
        {
            Transform candidate = _waypoints[_waypointIndex];
            if (candidate != null)
                return candidate;

            AdvanceIndex();
        }

        return null;
    }

    private void OnArrived(Transform point)
    {
        // 포인트에 WaltzPoint가 붙어 있고 대기 시간이 있으면 그만큼 멈춘다
        var waltzPoint = point.GetComponent<WaltzPoint>();
        if (waltzPoint != null && waltzPoint.waitSeconds > 0f)
            StartCoroutine(WaitAtPoint(waltzPoint.waitSeconds));

        AdvanceIndex();
    }

    private IEnumerator WaitAtPoint(float seconds)
    {
        _waiting = true;
        yield return new WaitForSeconds(seconds);
        _waiting = false;
    }

    private void AdvanceIndex()
    {
        if (_waypoints.Length <= 1) return;

        if (_pingPong)
        {
            if (_waypointIndex + _direction >= _waypoints.Length || _waypointIndex + _direction < 0)
                _direction = -_direction;

            _waypointIndex += _direction;
        }
        else
        {
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
        }
    }

    // ---------- 충돌 (정지 상태여도 판정 유지) ----------
    private void CheckPlayerOverlap()
    {
        if (!_active) return;
        if (S7S2Test.Invincible) return;
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
        if (S7S2Test.Invincible) return;
        if (_dialogueManager != null && _dialogueManager.isRunning) return;
        if (!collision.CompareTag("Player")) return;

        UIManager.Instance?.OpenGameOverUI();
    }

    // ---------- Scene 뷰 동선 표시 ----------
    private void OnDrawGizmos()
    {
        if (_waypoints == null || _waypoints.Length == 0) return;

        Gizmos.color = _frozen ? Color.cyan : new Color(1f, 0.92f, 0.2f);

        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform current = _waypoints[i];
            if (current == null) continue;

            Gizmos.DrawSphere(current.position, 0.12f);

            // 다음 포인트로 선 긋기 (왕복이면 마지막→처음 선은 생략)
            int nextIndex = (i + 1) % _waypoints.Length;
            if (_pingPong && nextIndex == 0) continue;

            Transform next = _waypoints[nextIndex];
            if (next != null)
                Gizmos.DrawLine(current.position, next.position);
        }

        // 현재 위치에서 목표 포인트까지 표시
        if (Application.isPlaying && _waypointIndex < _waypoints.Length)
        {
            Transform target = _waypoints[_waypointIndex];
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] == null) continue;
            UnityEditor.Handles.Label(
                _waypoints[i].position + Vector3.up * 0.25f,
                $"p{i + 1}"
            );
        }
#endif
    }
}
