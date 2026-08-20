using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터마다 애니메이터 파라미터 이름이 달라서 따로 받는다.
/// 하루: DirX / DirY / Walking / AnimSpeed
/// 루나: DirX / Diry / IsWalking  (AnimSpeed 없음)
/// </summary>
[System.Serializable]
public class ActorAnimParams
{
    public string dirX = "DirX";
    public string dirY = "DirY";
    public string walk = "Walking";

    [Tooltip("애니메이션 재생 속도 Float. 컨트롤러에 없으면 비워둘 것.")]
    public string animSpeed = "AnimSpeed";
}

/// <summary>
/// 연출용 타일 이동. 대각선이 없는 게임이라 목표까지 직선 Lerp하지 않고
/// 한 축씩 한 칸씩 움직인다. 꺾이는 지점에 장애물이 있으면 웨이포인트를 나눠 찍어야 한다.
///
/// 걷기 애니메이션은 "경로 전체가 끝날 때" 한 번만 끈다.
/// 축이 바뀔 때마다 끄면 ㄱ자로 꺾이는 지점에서 한 프레임씩 서 있는 게 보인다.
/// </summary>
public static class TileActorMover
{
    /// <summary>타일 한 칸 이동에 걸리는 시간. PlayerMove_Test_Lerp / AstarEnemy와 같은 기준.</summary>
    public const float StepSeconds = 0.2f;

    /// <summary>웨이포인트를 순서대로 따라간다. 마지막 지점에 닿으면 걷기를 끈다.</summary>
    public static IEnumerator MovePath(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        Transform[] path,
        float speed
    )
    {
        if (mover == null || path == null)
            yield break;

        foreach (Transform wp in path)
        {
            if (wp == null)
                continue;

            yield return MoveStraight(mover, animator, anim, wp.position, speed);
        }

        SetWalking(animator, anim, false, speed);
    }

    /// <summary>한 지점까지 이동하고 걷기를 끈다.</summary>
    public static IEnumerator MoveTo(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        Vector3 destination,
        float speed
    )
    {
        yield return MoveStraight(mover, animator, anim, destination, speed);
        SetWalking(animator, anim, false, speed);
    }

    /// <summary>
    /// 가로를 먼저 맞춘 뒤 세로를 맞추는 ㄱ자 경로. 걷기는 켠 채로 남긴다.
    /// </summary>
    private static IEnumerator MoveStraight(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        Vector3 destination,
        float speed
    )
    {
        if (mover == null)
            yield break;

        yield return MoveAxis(mover, animator, anim, destination.x, true, speed);
        yield return MoveAxis(mover, animator, anim, destination.y, false, speed);
    }

    private static IEnumerator MoveAxis(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        float targetValue,
        bool horizontal,
        float speed
    )
    {
        float current = horizontal ? mover.position.x : mover.position.y;
        float diff = targetValue - current;

        if (Mathf.Abs(diff) < 0.001f)
            yield break;

        float sign = Mathf.Sign(diff);
        Vector3 stepDir = horizontal ? new Vector3(sign, 0f, 0f) : new Vector3(0f, sign, 0f);

        SetFacing(animator, anim, stepDir);
        SetWalking(animator, anim, true, speed);

        float stepSeconds = StepSeconds / Mathf.Max(0.01f, speed);
        int fullSteps = Mathf.FloorToInt(Mathf.Abs(diff) + 0.001f);

        for (int i = 0; i < fullSteps; i++)
            yield return SingleStep(mover, mover.position + stepDir, stepSeconds);

        // 타일에 딱 떨어지지 않는 나머지가 있으면 마지막에 한 번 더 보정
        Vector3 finalPos = mover.position;
        if (horizontal)
            finalPos.x = targetValue;
        else
            finalPos.y = targetValue;

        if ((finalPos - mover.position).sqrMagnitude > 0.000001f)
            yield return SingleStep(mover, finalPos, stepSeconds);
    }

    private static IEnumerator SingleStep(Transform mover, Vector3 to, float duration)
    {
        Vector3 from = mover.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            mover.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mover.position = to;
    }

    public static void SetFacing(Animator animator, ActorAnimParams anim, Vector2 dir)
    {
        if (animator == null || anim == null || dir == Vector2.zero)
            return;

        Vector2 snapped = SnapTo4Dir(dir);

        if (!string.IsNullOrEmpty(anim.dirX))
            animator.SetFloat(anim.dirX, snapped.x);

        if (!string.IsNullOrEmpty(anim.dirY))
            animator.SetFloat(anim.dirY, snapped.y);
    }

    public static void SetWalking(Animator animator, ActorAnimParams anim, bool walking, float speed)
    {
        if (animator == null || anim == null)
            return;

        // 컨트롤러에 없는 파라미터를 건드리면 경고만 나고 아무 일도 일어나지 않는다
        if (!string.IsNullOrEmpty(anim.animSpeed))
            animator.SetFloat(anim.animSpeed, speed);

        if (!string.IsNullOrEmpty(anim.walk))
            animator.SetBool(anim.walk, walking);
    }

    /// <summary>애니메이터가 4방향 기준이라 대각선은 허용하지 않는다.</summary>
    public static Vector2 SnapTo4Dir(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return Vector2.down;

        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }
}
