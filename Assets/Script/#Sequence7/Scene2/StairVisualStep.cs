using UnityEngine;

/// <summary>
/// 계단 구역 — 캐릭터가 계단 단(段) 위에 서 있는 것처럼 보이게 한다.
///
/// 플레이어는 1칸(1유닛)씩 움직이는데 계단 그림의 한 단은 그보다 넓어서,
/// 그냥 걸으면 발이 단을 밟지 않고 붕 떠 보인다.
/// 이 컴포넌트는 이동 로직은 건드리지 않고 '보이는 위치'만 지금 밟고 있는
/// 단 높이에 맞춰 끌어내려 준다.
///
/// 사용법
/// 1) 계단 위에 빈 오브젝트를 만들고 BoxCollider2D를 계단 범위만큼 씌운다
/// 2) 이 스크립트를 붙인다
/// 3) '한 단 높이'를 계단 그림에 맞게 조절한다 (Scene 뷰에 가로선으로 표시됨)
///
/// ※ 한 단 높이가 1 또는 2처럼 딱 떨어지는 값일 때 가장 자연스럽다.
/// </summary>
public class StairVisualStep : MonoBehaviour
{
    [Header("계단 그림에 맞추기")]
    [Tooltip("계단 한 단의 높이(유닛). Scene 뷰의 노란 가로선이 계단 단과 겹치도록 맞추세요.\n" +
             "PPU가 64이므로 64픽셀 = 1유닛입니다")]
    [Range(0.25f, 4f)]
    [SerializeField] private float _treadHeight = 1f;

    [Tooltip("노란 선 전체를 위아래로 밀어서 계단 그림에 맞춥니다.\n" +
             "한 단 높이(간격)를 먼저 맞춘 뒤, 이걸로 위치를 맞추세요")]
    [Range(-3f, 3f)]
    [SerializeField] private float _lineOffset = 0f;

    [Tooltip("첫 번째 단의 바닥 높이. 비워두면 이 오브젝트 콜라이더의 아래쪽 끝을 씁니다")]
    [SerializeField] private Transform _stairBase;

    [Tooltip("보정 방향을 뒤집습니다 (내려가는 계단에서 어색하면 켜보세요)")]
    [SerializeField] private bool _invert = false;

    [Header("표시")]
    [Tooltip("Scene 뷰에 단 위치를 가로선으로 표시")]
    [SerializeField] private bool _showGuideLines = true;

    [SerializeField] private int _guideLineCount = 12;

    private Collider2D _zone;
    private Transform _player;
    private Collider2D _playerCollider;
    private PlayerVisualOffset _visual;

    private bool _playerInside;

    private void Awake()
    {
        _zone = GetComponent<Collider2D>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        _player = player.transform;
        _playerCollider = player.GetComponent<Collider2D>();
        _visual = PlayerVisualOffset.GetOrCreate(player);
    }

    private void Update()
    {
        if (_zone == null || _player == null || _visual == null) return;

        // 플레이어에게 Rigidbody2D가 없어서 물리 이벤트 대신 직접 겹침 검사
        bool inside = _playerCollider != null
            && _zone.bounds.Intersects(_playerCollider.bounds);

        if (inside)
        {
            _visual.SetTargetOffset(CalculateOffset());
            _playerInside = true;
        }
        else if (_playerInside)
        {
            // 계단을 벗어나면 보정을 풀어 원래 모습으로
            _visual.ClearOffset();
            _playerInside = false;
        }
    }

    /// <summary>
    /// 지금 플레이어가 밟고 있는 단의 높이로 끌어내리는 보정값을 구한다.
    /// 결과는 항상 0 이하(또는 _invert면 0 이상)이고 한 단 높이를 넘지 않는다.
    /// </summary>
    private float CalculateOffset()
    {
        if (_treadHeight <= 0.01f) return 0f;

        float baseY = GetBaseY();
        float relative = _player.position.y - baseY;

        // 밟고 있는 단의 바닥 높이
        float steppedY = Mathf.Floor(relative / _treadHeight) * _treadHeight;
        float offset = steppedY - relative;

        return _invert ? -offset : offset;
    }

    private float GetBaseY()
    {
        float baseY = _stairBase != null
            ? _stairBase.position.y
            : (_zone != null ? _zone.bounds.min.y : transform.position.y);

        return baseY + _lineOffset;
    }

    private void OnDisable()
    {
        if (_visual != null && _playerInside)
        {
            _visual.ClearOffset();
            _playerInside = false;
        }
    }

    // ---------- Scene 뷰 안내선 ----------
    private void OnDrawGizmos()
    {
        if (!_showGuideLines || _treadHeight <= 0.01f) return;

        var zone = _zone != null ? _zone : GetComponent<Collider2D>();
        if (zone == null) return;

        Bounds b = zone.bounds;
        float baseY = (_stairBase != null ? _stairBase.position.y : b.min.y) + _lineOffset;

        // 계단 범위
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(b.center, b.size);

        // 단 위치 가로선 — 캐릭터의 발이 닿을 높이
        for (int i = 0; i <= _guideLineCount; i++)
        {
            float y = baseY + i * _treadHeight;
            if (y > b.max.y + _treadHeight) break;

            // 기준선(첫 단)은 빨간색으로 구분
            Gizmos.color = i == 0
                ? new Color(1f, 0.35f, 0.3f, 1f)
                : new Color(1f, 0.9f, 0.2f, 0.9f);

            Gizmos.DrawLine(
                new Vector3(b.min.x, y, 0f),
                new Vector3(b.max.x, y, 0f));
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            new Vector3(b.min.x, baseY - 0.3f, 0f),
            $"한 단 {_treadHeight:0.##} 유닛 ({_treadHeight * 64f:0} px)");
#endif
    }
}
