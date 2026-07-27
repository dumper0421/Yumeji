using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected float moveSpeed = 2f;

    [Header("닿았을 때 동작")]
    [Tooltip("연결하면 게임오버 대신 이 이벤트가 실행됨 (컷신/대사/씬전환 등). 비워두면 기존처럼 죽음.")]
    [SerializeField] private UnityEvent _onReached;


    public GameObject Target;
    public bool isStop = false;
    protected bool hasReachedTarget = false;
    protected Coroutine moveCoroutine_ = null;


    // 목표 지점에 도달했을 때 실행할 기본 동작
    protected virtual void OnTargetReached() {
        //SceneManager.LoadScene("Sequence2S#3_7");
        hasReachedTarget = true;   // 재진입 방지 먼저

        if (_onReached.GetPersistentEventCount() > 0)
            _onReached.Invoke();                    // 인스펙터에 이벤트가 연결돼 있으면 실행
        else
            UIManager.Instance?.OpenGameOverUI();   // 아니면 기존 게임오버
    }
}
