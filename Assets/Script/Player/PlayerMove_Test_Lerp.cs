using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove_Test_Lerp : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public LayerMask NoPass;

    [SerializeField]
    private float speed_ = 1f;
    [SerializeField]
    private float runSpeed_ = 2f;

    public Vector3 vector;
    private bool canMove = true;
    public Animator animator;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    IEnumerator MoveCoroutine()
    {
        // 방향 입력은 Update()에서 설정된 vector 사용
        animator.SetFloat("DirX", vector.x);
        animator.SetFloat("DirY", vector.y);

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed_ : speed_;
        animator.SetFloat("AnimSpeed", moveSpeed);

        Vector2 direction = new Vector2(vector.x, vector.y);
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + direction;

        // 1. 자동 밀기 시도
        RaycastHit2D pushHit = Physics2D.Raycast(startPos, direction, 1f, LayerMask.GetMask("Pushable"));
        if (pushHit.collider != null && pushHit.collider.CompareTag("Pushable"))
        {
            var pushable = pushHit.collider.GetComponent<PushableObject>();
            if (pushable != null && pushable.TryPush(direction))
            {
                animator.SetBool("Pushing", true);

                float elapsed = 0f;
                float duration = 0.5f / speed_;
                Vector2 origin = transform.position;
                Vector2 dest = origin + direction;

                while (elapsed < duration)
                {
                    transform.position = Vector2.Lerp(origin, dest, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                transform.position = dest;
                animator.SetBool("Pushing", false);
                canMove = true;
                yield break;
            }
        }

        // 2. 이동 가능 체크
        boxCollider.enabled = false;
        RaycastHit2D hit = Physics2D.Linecast(startPos, targetPos, NoPass);
        boxCollider.enabled = true;
        if (hit.collider != null)
        {
            canMove = true;
            yield break;
        }

        // 3. 실제 이동
        float elapsedTime = 0f;
        float moveDuration = 0.2f / moveSpeed;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        canMove = true;
    }

    void Update()
    {
        // 1) 입력 처리
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        // 대각선 이동 방지
        if (input.x != 0) input.y = 0;

        // 2) 걷기 애니메이션 제어
        bool isWalking = input != Vector2.zero;
        animator.SetBool("Walking", isWalking);

        // 3) 이동 코루틴 시작
        if (canMove && isWalking)
        {
            canMove = false;
            vector = new Vector3(input.x, input.y, 0f);
            StartCoroutine(MoveCoroutine());
        }
    }

    // Teleport 호출용 공개 메서드
    public void Teleport(Vector3 pos)
    {
        StopAllCoroutines();
        transform.position = pos;
        if (animator != null)
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Pushing", false);
        }
        canMove = true;
    }
}
