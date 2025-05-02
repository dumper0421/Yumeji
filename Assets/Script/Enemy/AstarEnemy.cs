using System.Collections;
using UnityEngine;

/// <summary>
/// 2D 격자 기반 적 이동 스크립트 (A* Pathfinding)
/// 타겟과 적 모두 좌표가 셀 중심(0.5 단위)에서 시작하여 1씩 변화할 때 정상 동작합니다.
/// </summary>
public class AstarEnemy : Enemy
{
    [SerializeField] private float waitInterval = 2f;
    private float waitTimer;
    private Vector2 targetPos;
    private EnemyPathfinder pathFinder;

    [SerializeField] private AudioClip appearedSFX;
    [SerializeField] private float attackAllowedDistance = 0.5f;

    Coroutine moveCoroutine;

    void Start()
    {
        pathFinder = GetComponent<EnemyPathfinder>();
        if (Target != null)
        {
            // world pos at cell center, convert to grid index
            float tx = Target.transform.position.x - 0.5f;
            float ty = Target.transform.position.y - 0.5f;
            pathFinder.targetPos = new Vector2Int(
                Mathf.RoundToInt(tx),
                Mathf.RoundToInt(ty)
            );
        }
        SoundManager.Instance.PlaySFX(appearedSFX);
    }

    void Update()
    {
        if (Target == null) return;

        if (Vector2.Distance(transform.position, Target.transform.position) < attackAllowedDistance)
        {
            isStop = true;
            if (!hasReachedTarget) OnTargetReached();
        }
        else isStop = false;

        if (isStop) return;

        waitTimer += Time.deltaTime;
        if (waitTimer < waitInterval) return;

        if (moveCoroutine == null)
        {
            float tx = Target.transform.position.x - 0.5f;
            float ty = Target.transform.position.y - 0.5f;
            pathFinder.targetPos = new Vector2Int(
                Mathf.RoundToInt(tx),
                Mathf.RoundToInt(ty)
            );
            moveCoroutine = StartCoroutine(Move());
        }
    }

    IEnumerator Move()
    {
        // convert current world to grid
        float sx = transform.position.x - 0.5f;
        float sy = transform.position.y - 0.5f;
        pathFinder.startPos = new Vector2Int(
            Mathf.RoundToInt(sx),
            Mathf.RoundToInt(sy)
        );

        // compute next world center
        Vector2 raw = pathFinder.PathFinding();
        targetPos = raw; // raw already at cell center x+0.5, y+0.5

        if ((Vector2)transform.position == targetPos)
        {
            moveCoroutine = null;
            waitTimer = 0f;
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.2f / moveSpeed;
        Vector2 start = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector2.Lerp(start, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        Debug.Log($"Moved to {targetPos}");

        // flip sprite
        if (targetPos.x > start.x)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        moveCoroutine = null;
        waitTimer = 0f;
    }

    public void CancelMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        waitTimer = 0f;
    }

    protected override void OnTargetReached()
    {
        base.OnTargetReached();
    }
}
