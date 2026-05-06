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

    private Animator animator;

    private static readonly int HashIsWalk = Animator.StringToHash("isWalk");
    private static readonly int HashDirX   = Animator.StringToHash("DirX");
    private static readonly int HashDirY   = Animator.StringToHash("DirY");

    void Start()
    {
        originalMoveSpeed = moveSpeed;
        pathFinder = GetComponent<EnemyPathfinder>();
        animator = GetComponent<Animator>();

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

        if (!_hasLastTargetGrid || currentTargetGrid != _lastTargetGrid)
        {
            _lastTargetGrid = currentTargetGrid;
            _hasLastTargetGrid = true;

            pathFinder.targetPos = currentTargetGrid;
            CancelMovement();

            waitTimer = waitInterval;
        }

        if (isStop || !isMove)
        {
            if (animator != null)
            {
                Vector2 dirToTarget = (Target.transform.position - transform.position).normalized;
                bool isHorizontal = Mathf.Abs(dirToTarget.x) >= Mathf.Abs(dirToTarget.y);
                if (isHorizontal)
                {
                    animator.SetFloat(HashDirX, dirToTarget.x > 0 ? 1f : -1f);
                    animator.SetFloat(HashDirY, 0f);
                }
                else
                {
                    animator.SetFloat(HashDirX, 0f);
                    animator.SetFloat(HashDirY, dirToTarget.y > 0 ? 1f : -1f);
                }
            }
            return;
        }

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
        Vector2 actualStart = transform.position;
        Vector2 snappedStart = pathFinder.GridToWorld(pathFinder.WorldToGrid(actualStart));
        pathFinder.startPos = pathFinder.WorldToGrid(actualStart);
        pathFinder.targetPos = currentTargetGrid;

        if (!pathFinder.TryGetNextWorld(out Vector2 nextWorld))
        {
            moveCoroutine = null;
            waitTimer = 0f;
            yield break;
        }

        targetPos = nextWorld;

        if (Vector2.Distance(actualStart, targetPos) < 0.001f)
        {
            moveCoroutine = null;
            waitTimer = 0f;
            yield break;
        }

        Vector2 moveDir = targetPos - snappedStart;
        bool isHorizontal = Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y);

        // 애니메이션: 가로/세로 중 이동량이 큰 축을 우선
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool(HashIsWalk, true);
            if (isHorizontal)
            {
                animator.SetFloat(HashDirX, moveDir.x > 0 ? 1f : -1f);
                animator.SetFloat(HashDirY, 0f);
            }
            else
            {
                animator.SetFloat(HashDirX, 0f);
                animator.SetFloat(HashDirY, moveDir.y > 0 ? 1f : -1f);
            }
        }

        float elapsed = 0f;
        float duration = 0.2f / Mathf.Max(0.0001f, moveSpeed);

        while (elapsed < duration)
        {
            transform.position = Vector2.Lerp(actualStart, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        if (animator != null)
            animator.SetBool(HashIsWalk, false);

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

        if (animator != null)
            animator.SetBool(HashIsWalk, false);

        waitTimer = 0f;
    }

    // �ڷ���Ʈ �ڵ忡�� ���� ȣ���ϸ� �� Ȯ����
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