using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어가 영역에 진입했을 때 UnityEvent를 발생시키는 범용 트리거.
/// [7-2]에서는 로비 ↔ 연회장 이동 시 왈츠 BGM 재생/정지 연결에 사용.
/// (Sequence7Scene2DialogueController.OnPlayerEnterLobby / OnPlayerEnterBanquet)
/// </summary>
public class PlayerZoneTrigger : MonoBehaviour
{
    [SerializeField] private UnityEvent _onPlayerEnter;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        _onPlayerEnter?.Invoke();
    }
}
