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

        if (StatusManager.Instance != null && StatusManager.Instance.playerStatus != null)
        {
            speed_ = StatusManager.Instance.playerStatus.BaseSpeed;
            runSpeed_ = StatusManager.Instance.playerStatus.RunSpeed;
        }
    }

    IEnumerator MoveCoroutine()
    {
        vector.Set(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);

        // 방향키 2개 입력 방지 (대각선 이동 방지)
        if (vector.x != 0)
            vector.y = 0;

        animator.SetFloat("DirX", vector.x);
        animator.SetFloat("DirY", vector.y);
        float moveSpeed = speed_;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = runSpeed_;
        }

        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + new Vector2(vector.x, vector.y);

        // 이동 불가 지역 체크
        RaycastHit2D hit;
        boxCollider.enabled = false;
        hit = Physics2D.Linecast(startPos, targetPos, NoPass);
        boxCollider.enabled = true;

        if (hit.collider != null)
        {
            canMove = true;
            yield break; // 이동 불가 시 종료
        }

        animator.SetBool("Walking", true);

        // 목표 위치까지 이동 (Lerp 사용)
        float elapsedTime = 0;
        float moveDuration = 0.2f / moveSpeed; // 속도에 따라 이동 시간 조절

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        animator.SetBool("Walking", false);

        canMove = true; // 이동 완료 후 다시 입력 가능
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

        #region 플레이어 체력 깎기
        if (Input.GetKeyUp(KeyCode.Z))
            StatusManager.Instance.playerStatus.TakeDamage(100);
        #endregion
    }
}
