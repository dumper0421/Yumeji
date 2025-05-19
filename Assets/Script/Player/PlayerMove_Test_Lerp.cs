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

    [Header("Flash Settings")]
    [SerializeField] private Vector2Int flashTiles = new Vector2Int(3, 4);  // (가로 × 세로) 타일 수
    [SerializeField] private Vector2 tileSize = new Vector2(1f, 1f);  // 한 타일의 월드 크기
    [SerializeField] private LayerMask flashableLayer;                    // 피사체 탐지용 LayerMask
    [SerializeField] private GameObject flashVFXPrefab;                    // 플래시 VFX 프리팹
    [SerializeField] private float flashDuration = 0.3f;              // VFX 유지 시간(초)
    [SerializeField] private float shootCooldown = 2f;  // 쿨타임 (초)
    private float nextShootTime = 0f;
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
    IEnumerator ShootingCoroutine()
    {
      yield return null;    
    }


    void Update()
    {
        // 1) 이미 멈춰있다면(이동 중이거나 촬영 중이면) 아무 것도 안 함
        if (!canMove) return;

        // 2) 이동 입력 처리
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        if (input.x != 0) input.y = 0;  // 대각선 방지

        bool isWalking = input != Vector2.zero;
        animator.SetBool("Walking", isWalking);

        if (isWalking)
        {
            // 이동 코루틴 시작
            canMove = false;
            vector = new Vector3(input.x, input.y, 0f);
            StartCoroutine(MoveCoroutine());
            return;
        }

        // 3) 촬영 입력 처리 (이동 입력이 없을 때만)
        if (Input.GetKeyDown(KeyCode.C)
         && Time.time >= nextShootTime)
        {
            // 1) 다음 촬영 가능 시각 갱신
            nextShootTime = Time.time + shootCooldown;

            // 2) 이동 잠금
            canMove = false;

            // 3) Animator 파라미터 세팅 및 트리거
            animator.SetFloat("DirX", vector.x);
            animator.SetFloat("DirY", vector.y);
            animator.SetTrigger("TakeShoot");
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
    
    //촬영 에니메이션 종료시 호출
    public void EndTakeShoot()
    {
 
        canMove = true;
    }
    public void HandleFlash()
    {
        // 1) 바라보는 방향 구하기
        Vector2 dir = new Vector2(animator.GetFloat("DirX"), animator.GetFloat("DirY"));
        if (dir == Vector2.zero)
            dir = Vector2.down;  // 기본 아래

        // 2) OverlapBox 크기 및 중심 계산
        Vector2 boxSize = new Vector2(flashTiles.x * tileSize.x, flashTiles.y * tileSize.y);
        Vector2 boxCenter = (Vector2)transform.position + dir * (boxSize.y / 2f);

        // 3) 회전 각도 계산 (Atan2 → 도 단위)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angleOffset = -90f;  // ▼ 방향 기준이면 -90, → 기준이면 0, ▲이면 +90, ←이면 +180 등
        Quaternion rot = Quaternion.Euler(0f, 0f, angle + angleOffset);

        // 4) VFX 생성 및 파괴 예약
        if (flashVFXPrefab != null)
        {
            var vfx = Instantiate(flashVFXPrefab, boxCenter, rot);
            Destroy(vfx, flashDuration);
        }

        // 5) Flashable 레이어만 OverlapBoxAll
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            boxCenter,
            boxSize,
            0f,
            flashableLayer
        );

        // 6) IFlashable 호출
        foreach (var col in hits)
        {
            var flashable = col.GetComponent<IFlashable>();
            if (flashable != null)
                flashable.OnPhotoTaken(false);
        }
    }


    // 디버그용: 씬 뷰에서 플래시 범위를 시각화
    void OnDrawGizmosSelected()
    {
        Vector2 dir = Vector2.down;
        if (animator != null)
        {
            dir = new Vector2(animator.GetFloat("DirX"), animator.GetFloat("DirY"));
            if (dir == Vector2.zero) dir = Vector2.down;
        }
        Vector2 boxSize = new Vector2(flashTiles.x * tileSize.x, flashTiles.y * tileSize.y);
        Vector2 boxCenter = (Vector2)transform.position + dir * (boxSize.y / 2f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawCube(boxCenter, boxSize);
    }
}

