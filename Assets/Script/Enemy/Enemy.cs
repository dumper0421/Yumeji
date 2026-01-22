using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected float moveSpeed = 2f;


    public GameObject Target;
    public bool isStop = false;
    protected bool hasReachedTarget = false;
    protected Coroutine moveCoroutine_ = null;


    // 목표 지점에 도달했을 때 실행할 기본 동작
    protected virtual void OnTargetReached() {
        SceneManager.LoadScene("Sequence2S#3_7");
    //UIManager.Instance.OpenGameOverUI();
    //    hasReachedTarget = true;
    }
}
