using UnityEngine;

/// <summary>
/// 기믹 2) 마네킹 미로 — 마네킹 게임오버 판정.
/// 플레이어에게 Rigidbody2D가 없어도 동작하도록 물리 이벤트 대신
/// 매 프레임 콜라이더 겹침을 직접 검사한다. (기존 Obstacle 대체)
/// 기본 마네킹/기믹 마네킹 모두 이 컴포넌트를 사용.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MannequinObstacle : MonoBehaviour
{
    [Tooltip("대화 중에는 게임오버 판정을 일시 정지 (선택)")]
    [SerializeField] private DialogueManager _dialogueManager;

    private Collider2D _collider;
    private Collider2D _playerCollider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerCollider = player.GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (_collider == null || _playerCollider == null) return;
        if (S7S2Test.Invincible) return;
        if (_dialogueManager != null && _dialogueManager.isRunning) return;

        if (_collider.bounds.Intersects(_playerCollider.bounds))
            UIManager.Instance?.OpenGameOverUI();
    }
}
