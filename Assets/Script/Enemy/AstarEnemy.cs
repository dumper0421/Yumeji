// AstarEnemy.cs
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

    Coroutine moveCoroutine;

    public bool isMove = false;

    void Start()
    {
        originalMoveSpeed = moveSpeed;
        pathFinder = GetComponent<EnemyPathfinder>();

        if (Target != null)
        {
            pathFinder.targetPos = pathFinder.WorldToGrid(Target.transform.position);
        }
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

        if (Vector2.Distance(transform.position, Target.transform.position) > fastDistance)
        {
            moveSpeed = farMoveSpeed;
        }
        else moveSpeed = originalMoveSpeed;

        if (isStop || !isMove) return;

        waitTimer += Time.deltaTime;
        if (waitTimer < waitInterval) return;

        if (moveCoroutine == null)
        {
            pathFinder.targetPos = pathFinder.WorldToGrid(Target.transform.position);
            moveCoroutine = StartCoroutine(Move());
        }
    }

    IEnumerator Move()
    {
        pathFinder.startPos = pathFinder.WorldToGrid(transform.position);

        Vector2 nextWorld = pathFinder.PathFinding();
        targetPos = nextWorld;

        if (Vector2.Distance(transform.position, targetPos) < 0.001f)
        {
            moveCoroutine = null;
            waitTimer = 0f;
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.2f / Mathf.Max(0.0001f, moveSpeed);
        Vector2 start = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector2.Lerp(start, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

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
