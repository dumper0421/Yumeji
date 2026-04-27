using System.Collections;
using UnityEngine;

public class AstarEnemy : Enemy
{
    [SerializeField] private float waitInterval = 2f;
    private float waitTimer;
    private Vector2 targetPos;
    private EnemyPathfinder pathFinder;

    [SerializeField] private float attackAllowedDistance = 1f;
    [SerializeField] private float fastDistance = 20f;
    [SerializeField] private float farMoveSpeed = 1.25f;

    private float originalMoveSpeed = 1f;

    private Coroutine moveCoroutine;

    public bool isMove = false;

    private Vector2Int _lastTargetGrid;
    private bool _hasLastTargetGrid;

    void Start()
    {
        originalMoveSpeed = moveSpeed;
        pathFinder = GetComponent<EnemyPathfinder>();

        if (Target != null && pathFinder != null)
        {
            _lastTargetGrid = pathFinder.WorldToGrid(Target.transform.position);
            _hasLastTargetGrid = true;
            pathFinder.targetPos = _lastTargetGrid;
        }
    }

    void Update()
    {
        if (Target == null || pathFinder == null)
            return;

        float distanceToTarget = Vector2.Distance(transform.position, Target.transform.position);

        if (distanceToTarget < attackAllowedDistance)
        {
            isStop = true;
            if (!hasReachedTarget)
                OnTargetReached();
        }
        else
        {
            isStop = false;
        }

        moveSpeed = distanceToTarget > fastDistance ? farMoveSpeed : originalMoveSpeed;

        Vector2Int currentTargetGrid = pathFinder.WorldToGrid(Target.transform.position);

        // 텔레포트 포함, 타겟 그리드가 바뀌면 즉시 재탐색
        if (!_hasLastTargetGrid || currentTargetGrid != _lastTargetGrid)
        {
            _lastTargetGrid = currentTargetGrid;
            _hasLastTargetGrid = true;

            pathFinder.targetPos = currentTargetGrid;
            CancelMovement();

            // 바로 다시 추적 시작하게 강제
            waitTimer = waitInterval;
        }

        if (isStop || !isMove)
            return;

        waitTimer += Time.deltaTime;
        if (waitTimer < waitInterval)
            return;

        if (moveCoroutine == null)
        {
            pathFinder.targetPos = currentTargetGrid;
            moveCoroutine = StartCoroutine(Move(currentTargetGrid));
        }
    }

    IEnumerator Move(Vector2Int currentTargetGrid)
    {
        Vector2 startWorld = transform.position;
        pathFinder.startPos = pathFinder.WorldToGrid(startWorld);
        pathFinder.targetPos = currentTargetGrid;

        if (!pathFinder.TryGetNextWorld(out Vector2 nextWorld))
        {
            moveCoroutine = null;
            waitTimer = 0f;
            yield break;
        }

        targetPos = nextWorld;

        if (Vector2.Distance(startWorld, targetPos) < 0.001f)
        {
            moveCoroutine = null;
            waitTimer = 0f;
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.2f / Mathf.Max(0.0001f, moveSpeed);

        while (elapsed < duration)
        {
            transform.position = Vector2.Lerp(startWorld, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        if (targetPos.x > startWorld.x)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
        }

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

    // 텔레포트 코드에서 직접 호출하면 더 확실함
    public void ForceRepathNow()
    {
        if (pathFinder == null || Target == null)
            return;

        _lastTargetGrid = pathFinder.WorldToGrid(Target.transform.position);
        _hasLastTargetGrid = true;

        pathFinder.targetPos = _lastTargetGrid;
        CancelMovement();
        waitTimer = waitInterval;
    }

    protected override void OnTargetReached()
    {
        base.OnTargetReached();
    }
}