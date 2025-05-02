using System.Collections;
using UnityEngine;

public class AstarEnemy : Enemy
{
    [SerializeField] private float waitInterval = 2f;
    private float waitTimer_ = 0f;
    private Vector2 targetPos_;
    private EnemyPathfinder pathFinder_;
    public bool IsSameLocation = true;

    [SerializeField]
    private AudioClip appearedSFX_;

    [SerializeField]
    private float _attackAllowedDistance = 0.5f;

    void Start()
    {
        pathFinder_ = GetComponent<EnemyPathfinder>();
        if (Target != null)
        {
            pathFinder_.targetPos = new Vector2Int(
                (int)Target.transform.position.x,
                (int)Target.transform.position.y
            );
        }

        SoundManager.Instance.PlaySFX(appearedSFX_);
    }

    void Update()
    {
        if (Target == null)
            return;

        if (Vector2.Distance(transform.position, Target.transform.position) < _attackAllowedDistance)
        {
            isStop = true;
            if (!hasReachedTarget)
            {
                //OnTargetReached();
            }
        }
        else
        {
            isStop = false;
        }

        if (isStop)
            return;

        waitTimer_ += Time.deltaTime;
        if (waitTimer_ < waitInterval)
            return;

        if (moveCoroutine_ == null)
        {
            pathFinder_.targetPos = new Vector2Int(
                (int)Target.transform.position.x,
                (int)Target.transform.position.y
            );
            moveCoroutine_ = StartCoroutine(Move());
        }
    }

    IEnumerator Move()
    {
        Vector2Int startPos = new Vector2Int(
            (int)transform.position.x,
            (int)transform.position.y
        );
        pathFinder_.startPos = startPos;

        targetPos_ = pathFinder_.PathFinding();

        float elapsedTime = 0f;
        float moveDuration = 0.2f / moveSpeed; 
        Vector2 initialPos = transform.position;
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(initialPos, targetPos_, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos_;
        Debug.Log(targetPos_);
        if (targetPos_.x < transform.position.x)
            transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
        else
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        moveCoroutine_ = null;
    }

    public void CancelMovement()
    {
        if (moveCoroutine_ != null)
        {
            StopCoroutine(moveCoroutine_);
            moveCoroutine_ = null;
        }

        waitTimer_ = 0f;
    }

    
    protected override void OnTargetReached()
    {
        base.OnTargetReached();
    }
}
