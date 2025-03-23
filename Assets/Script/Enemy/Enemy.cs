using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected float moveSpeed = 2f;


    public GameObject Target;
    public bool isStop = false;
    protected bool hasReachedTarget = false;
    protected Coroutine moveCoroutine_ = null;


    // 목표 지점에 도달했을 때 실행할 기본 동작
    protected virtual void OnTargetReached()
    {
        // 예시: 플레이어에게 큰 피해를 주는 처리
        StatusManager.Instance.playerStatus.TakeDamage(10000f);
        hasReachedTarget = true;
    }
}
