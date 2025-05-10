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
    private Animator animator;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    IEnumerator MoveCoroutine()
    {
        vector.Set(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);

        if (vector.x != 0)
            vector.y = 0;

        animator.SetFloat("DirX", vector.x);
        animator.SetFloat("DirY", vector.y);

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed_ : speed_;
        animator.SetFloat("AnimSpeed", moveSpeed);

        Vector2 direction = new Vector2(vector.x, vector.y);
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + direction;

        // 🔶 1. 자동 밀기 시도
        RaycastHit2D pushHit = Physics2D.Raycast(startPos, direction, 1f, LayerMask.GetMask("Pushable"));
        if (pushHit.collider != null && pushHit.collider.CompareTag("Pushable"))
        {
            Debug.Log("Pushing감지");
            var pushable = pushHit.collider.GetComponent<PushableObject>();
            if (pushable != null && pushable.TryPush(direction))
            {
                Debug.Log("Push 성공 - 플레이어도 이동 시작");
                animator.SetBool("Pushing", true);

                // ✅ 변수 이름 변경: pushElapsedTime
                float pushElapsedTime = 0f;
                float pushMoveDuration = 0.5f / speed_;
                Vector2 playerStart = transform.position;
                Vector2 playerTarget = playerStart + direction;

                while (pushElapsedTime < pushMoveDuration)
                {
                    transform.position = Vector2.Lerp(playerStart, playerTarget, pushElapsedTime / pushMoveDuration);
                    pushElapsedTime += Time.deltaTime;
                    yield return null;
                }

                transform.position = playerTarget;
                animator.SetBool("Pushing", false);
                canMove = true;
                yield break;
            }

        }

        // 🔶 2. 이동 가능 체크
        boxCollider.enabled = false;
        RaycastHit2D hit = Physics2D.Linecast(startPos, targetPos, NoPass);
        boxCollider.enabled = true;

        if (hit.collider != null)
        {
            canMove = true;
            yield break;
        }

        // 🔶 3. 실제 이동
        animator.SetBool("Walking", true);

        float elapsedTime = 0;
        float moveDuration = 0.2f / moveSpeed;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        animator.SetBool("Walking", false);

        canMove = true;
    }


    void Update()
    {
        if (canMove)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            {
                canMove = false;
                StartCoroutine(MoveCoroutine());
            }
        }
    }
}
