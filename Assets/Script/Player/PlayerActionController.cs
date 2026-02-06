using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerActionController : MonoBehaviour
{
    [Header("Sit Settings")]
    [SerializeField] private float sitMoveDuration = 0.15f;
    [SerializeField] private bool bypassNoPassOnSit = true;
    //앉기 제어
    public bool IsLockedByEvent { get; set; } = false;

    private bool isSitting = false;
    private Vector3 standReturnPos;
    private SitObject currentSeat = null;

    [Header("Push Settings")]
    // 밀기 자체 시간은 PushableObject.pushDuration 사용

    private Animator animator;
    private BoxCollider2D boxCollider;
    private PlayerMove_Test_Lerp move;

    public bool IsPushing { get; private set; }
    public bool IsSitting => isSitting;
    public SitObject CurrentSeat => currentSeat;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        move = GetComponent<PlayerMove_Test_Lerp>();
    }

    // ======= 앉기 관련 ======= //

    public void SitAt(SitObject seat)
    {
        if (IsLockedByEvent) return;
        if (move == null) return;
        if (IsPushing) return;
        if (isSitting && currentSeat == seat) return;
        if (animator.GetBool("Walking")) return;
        StopAllCoroutines();
        StartCoroutine(CoSitEnterSeat(seat));
    }

    public void StandUp()
    {
        if (IsLockedByEvent) return;
        if (!isSitting) return;
        StopAllCoroutines();
        StartCoroutine(CoSitExit());
    }

    private IEnumerator CoSitEnterSeat(SitObject seat)
    {
        if (move != null)
            move.canMove = false;

        standReturnPos = transform.position;
        currentSeat = seat;

        Vector2 lookDir = GetFacing4Dir();
        if (lookDir == Vector2.zero)
            lookDir = Vector2.down;

        animator.SetFloat("DirX", lookDir.x);
        animator.SetFloat("DirY", lookDir.y);

        animator.ResetTrigger("SitDownEnd");
        animator.SetTrigger("SitDown");

        Vector3 startPos = transform.position;
        Vector3 targetPos = (Vector3)(Vector2)seat.transform.position + (Vector3)seat.sitOffset;

        bool prevEnabled = boxCollider.enabled;
        if (bypassNoPassOnSit) boxCollider.enabled = false;

        float t = 0f;
        while (t < sitMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sitMoveDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, k);
            yield return null;
        }
        transform.position = targetPos;

        if (bypassNoPassOnSit) boxCollider.enabled = prevEnabled;

        isSitting = true;
    }

    private IEnumerator CoSitExit()
    {
        animator.ResetTrigger("SitDown");
        animator.SetTrigger("SitDownEnd");

        Vector3 startPos = transform.position;
        Vector3 targetPos = standReturnPos;

        bool prevEnabled = boxCollider.enabled;
        if (bypassNoPassOnSit) boxCollider.enabled = false;

        float t = 0f;
        while (t < sitMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sitMoveDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, k);
            yield return null;
        }
        transform.position = targetPos;

        if (bypassNoPassOnSit) boxCollider.enabled = prevEnabled;

        isSitting = false;
        currentSeat = null;

        if (move != null)
            move.canMove = true;
    }

    private Vector2 GetFacing4Dir()
    {
        float dx = animator.GetFloat("DirX");
        float dy = animator.GetFloat("DirY");

        if (Mathf.Abs(dx) > Mathf.Abs(dy))
            return new Vector2(Mathf.Sign(dx), 0f);
        else if (Mathf.Abs(dy) > 0f)
            return new Vector2(0f, Mathf.Sign(dy));

        return Vector2.zero;
    }

    // ======= 밀기 관련 ======= //

    public void StartPushMove(Vector2 dir, float duration)
    {
        if (move == null) return;
        if (IsPushing) return;
        if (isSitting) return;

        // ★ 밀기 시작과 동시에 상태 잠굼
        IsPushing = true;
        move.canMove = false;

        StopAllCoroutines();
        StartCoroutine(CoPushWithBox(dir, duration));
    }

    private IEnumerator CoPushWithBox(Vector2 dir, float duration)
    {
        animator.SetBool("Walking", false);
        animator.SetBool("Pushing", true);
        animator.SetFloat("DirX", dir.x);
        animator.SetFloat("DirY", dir.y);

        Vector3 start = transform.position;
        Vector3 target = start + (Vector3)dir;

        bool prevEnabled = boxCollider.enabled;
        boxCollider.enabled = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }

        transform.position = target;
        boxCollider.enabled = prevEnabled;

        animator.SetBool("Pushing", false);

        // ★ 밀기 끝날 때에만 이동/상호작용 해제
        if (move != null)
            move.canMove = true;

        IsPushing = false;
    }
}
