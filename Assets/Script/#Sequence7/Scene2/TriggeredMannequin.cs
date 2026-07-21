using System.Collections;
using UnityEngine;

/// <summary>
/// 기믹 2) 마네킹 미로 — 기믹 마네킹.
/// 플레이어가 트리거 타일을 밟으면:
///   1) 마네킹들이 기믹 위치로 이동 (딜레이 없음, 이동시간 0.25초, 이동 SFX 재생)
///   2) 이동 완료 후 3초간 홀드
///   3) 다시 제자리로 이동 (이동시간 0.4초, 이동 SFX 재생)
///   4) 플레이어가 트리거 타일에서 나가면 기믹 종료 → 재발동 가능
/// 기믹이 진행 중일 때 트리거 입력은 무시한다.
/// 카메라 촬영으로는 정지시킬 수 없다. (IFlashable 미구현)
/// 게임오버 판정은 마네킹 오브젝트의 MannequinObstacle 컴포넌트가 담당.
/// </summary>
public class TriggeredMannequin : MonoBehaviour
{
    [System.Serializable]
    public class MannequinMove
    {
        [Tooltip("트리거를 밟으면 이동할 마네킹")]
        public Transform mannequin;

        [Tooltip("마네킹의 기믹 위치(이동 목표 지점)")]
        public Transform targetPoint;

        [HideInInspector] public Vector3 homePosition;
    }

    [Header("Mannequins")]
    [Tooltip("트리거 작동 시 동시에 이동할 마네킹 목록 (2개 이상 가능)")]
    [SerializeField] private MannequinMove[] _moves;

    [Header("시간 설정 (초)")]
    [Tooltip("기믹 위치로 이동하는 시간. 기획서 기준 0.25초")]
    [Range(0.05f, 3f)]
    [SerializeField] private float _moveOutDuration = 0.25f;

    [Tooltip("이동 완료 후 멈춰 있는 시간. 기획서 기준 3초")]
    [Range(0f, 10f)]
    [SerializeField] private float _holdDuration = 3f;

    [Tooltip("제자리로 복귀하는 시간. 기획서 기준 0.4초")]
    [Range(0.05f, 3f)]
    [SerializeField] private float _returnDuration = 0.4f;

    [Header("SFX")]
    [Tooltip("마네킹이 이동할 때 효과음 (7-2_SFX_mannequin_move)")]
    [SerializeField] private AudioClip _moveSfx;

    private bool _running = false;
    private bool _homeSaved = false;

    private Collider2D _collider;
    private Collider2D _playerCollider;

    // ---------- 테스트 패널에서 실시간으로 바꾸는 값 ----------
    public float MoveOutDuration { get => _moveOutDuration; set => _moveOutDuration = value; }
    public float HoldDuration { get => _holdDuration; set => _holdDuration = value; }
    public float ReturnDuration { get => _returnDuration; set => _returnDuration = value; }
    public bool IsRunning => _running;
    public int MannequinCount => _moves != null ? _moves.Length : 0;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerCollider = player.GetComponent<Collider2D>();

        // 제자리(원위치) 저장
        if (_moves != null)
        {
            foreach (var move in _moves)
            {
                if (move != null && move.mannequin != null)
                    move.homePosition = move.mannequin.position;
            }
        }

        _homeSaved = true;
    }

    private void OnDisable()
    {
        // 코루틴이 중간에 끊기면 진행 중 상태가 남아 재발동되지 않으므로 초기화
        StopAllCoroutines();
        _running = false;

        // 원위치를 저장하기 전이라면 건드리지 않는다 (0,0으로 순간이동 방지)
        if (_homeSaved && _moves != null)
        {
            foreach (var move in _moves)
            {
                if (move != null && move.mannequin != null)
                    move.mannequin.position = move.homePosition;
            }
        }
    }

    private void Update()
    {
        // 기믹이 진행 중이면 입력 무시
        if (_running) return;

        if (PlayerOnTile())
            StartCoroutine(GimmickCycle());
    }

    /// <summary>플레이어에게 RB2D가 없어도 동작하도록 콜라이더 겹침을 직접 검사</summary>
    private bool PlayerOnTile()
    {
        if (_collider == null || _playerCollider == null) return false;
        return _collider.bounds.Intersects(_playerCollider.bounds);
    }

    private IEnumerator GimmickCycle()
    {
        if (_moves == null || _moves.Length == 0) yield break;

        _running = true;

        // 1) 기믹 위치로 이동 (딜레이 없음)
        PlayMoveSfx();
        yield return MoveAll(toTarget: true, _moveOutDuration);

        // 2) 이동 완료 후 홀드
        yield return new WaitForSeconds(_holdDuration);

        // 3) 다시 제자리로 이동
        PlayMoveSfx();
        yield return MoveAll(toTarget: false, _returnDuration);

        // 4) 플레이어가 트리거 타일에서 나갈 때까지 대기 → 기믹 종료(재발동 가능)
        while (PlayerOnTile())
            yield return null;

        _running = false;
    }

    /// <summary>모든 마네킹을 동시에 지정 시간 동안 이동시킨다.</summary>
    private IEnumerator MoveAll(bool toTarget, float duration)
    {
        int count = _moves.Length;
        var fromPos = new Vector3[count];
        var toPos = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            var move = _moves[i];
            if (move == null || move.mannequin == null) continue;

            fromPos[i] = move.mannequin.position;
            toPos[i] = (toTarget && move.targetPoint != null)
                ? move.targetPoint.position
                : move.homePosition;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < count; i++)
            {
                var move = _moves[i];
                if (move == null || move.mannequin == null) continue;
                move.mannequin.position = Vector3.Lerp(fromPos[i], toPos[i], t);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            var move = _moves[i];
            if (move == null || move.mannequin == null) continue;
            move.mannequin.position = toPos[i];
        }
    }

    private void PlayMoveSfx()
    {
        if (_moveSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(_moveSfx);
    }

    // ---------- Scene 뷰 표시 ----------
    private void OnDrawGizmos()
    {
        // 트리거 타일 범위
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = _running
                ? new Color(1f, 0.3f, 0.3f, 0.35f)
                : new Color(0.3f, 0.7f, 1f, 0.35f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }

        if (_moves == null) return;

        // 마네킹 → 기믹 위치 화살표
        foreach (var move in _moves)
        {
            if (move == null || move.mannequin == null || move.targetPoint == null) continue;

            Gizmos.color = new Color(0.2f, 1f, 0.4f);
            Gizmos.DrawLine(move.mannequin.position, move.targetPoint.position);
            Gizmos.DrawWireSphere(move.targetPoint.position, 0.2f);

            // 트리거와 마네킹 연결선(어떤 트리거가 어떤 마네킹을 움직이는지)
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.3f);
            Gizmos.DrawLine(transform.position, move.mannequin.position);
        }
    }
}
