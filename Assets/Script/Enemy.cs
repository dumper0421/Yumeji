using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private enum State
    {
        Chase,
        Move
    }

    public bool isStop = false;
    public GameObject Target;

    [SerializeField]
    private float moveSpeed_ = 2f;

    [SerializeField]
    private float waitInterval_ = 2f;

    private float waitTimer_ = 0f;
    private bool hasReachedTarget_ = false;
    private Vector2 targetPos_;
    private Coroutine moveCoroutine_ = null;
    private EnemyPathfinder pathFinder_;

    void Start()
    {
        pathFinder_ = GetComponent<EnemyPathfinder>();
        pathFinder_.targetPos = new Vector2Int((int)Target.transform.position.x, (int)Target.transform.position.y);
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector2.Distance(transform.position, Target.transform.position) < 0.01f)
        {
            isStop = true;
            if(hasReachedTarget_)
                OnTargetReached();
        }
        else
            isStop = false;

        if (isStop) return;

        waitTimer_ += Time.deltaTime;
        if (waitInterval_ > waitTimer_) return;

        if (moveCoroutine_ == null)
        {
            pathFinder_.targetPos = new Vector2Int((int)Target.transform.position.x, (int)Target.transform.position.y);
            moveCoroutine_ = StartCoroutine(Move());
        }
    }

    IEnumerator Move()
    {
        Vector2Int startPos = new Vector2Int((int)transform.position.x,(int)transform.position.y);
        pathFinder_.startPos = startPos;
        targetPos_ = pathFinder_.PathFinding();

        float elapsedTime = 0;
        float moveDuration = 0.2f / moveSpeed_; // 속도에 따라 이동 시간 조절

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos_, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos_;
        moveCoroutine_ = null;
    }

    void OnTargetReached()
    {
        StatusManager.Instance.playerStatus.TakeDamage(10000f);
        hasReachedTarget_ = true;
    }

 }