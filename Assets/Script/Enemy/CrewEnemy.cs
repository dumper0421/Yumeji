using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrewEnemy : Enemy
{
    private Vector2 targetPos_;

    private void Update()
    {
        if (moveCoroutine_ == null)
        {
            moveCoroutine_ = StartCoroutine(Move());
        }

        if (Vector2.Distance(transform.position, Target.transform.position) < 0.01f)
        {
            isStop = true;
            if (!hasReachedTarget)
            {
                OnTargetReached();
            }
        }
    }

    IEnumerator Move()
    {
        targetPos_ = new Vector2Int(
                (int)Target.transform.position.x,
                (int)Target.transform.position.y
        );
        Debug.Log(targetPos_);
        float elapsedTime = 0f;
        float moveDuration = 0.2f / moveSpeed; // 속도에 따른 이동 시간
        Vector2 initialPos = transform.position;
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(initialPos, targetPos_, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos_;
        moveCoroutine_ = null;
    }
}
