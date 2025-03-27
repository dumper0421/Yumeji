using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableObject : MonoBehaviour
{
    public LayerMask obstacleLayer; // 장애물 레이어
    public float moveDuration = 1f;

    private bool isMoving = false;

    public bool TryPush(Vector2 direction)
    {
        if (isMoving) return false;

        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + direction;

        // 충돌 체크
        Collider2D hit = Physics2D.OverlapBox(targetPos, Vector2.one * 0.8f, 0f, obstacleLayer);
        if (hit != null) return false;

        StartCoroutine(MoveToPosition(targetPos));
        return true;
    }

    private IEnumerator MoveToPosition(Vector2 targetPos)
    {
        isMoving = true;
        Vector2 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
    }
}
