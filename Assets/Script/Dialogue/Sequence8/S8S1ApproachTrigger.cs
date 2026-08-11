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

    private bool _fired;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_fired || !collision.CompareTag("Player"))
            return;

        _fired = true;

        if (_controller != null)
            _controller.OnPlayerEnteredT1();

        gameObject.SetActive(false);
    }
}
