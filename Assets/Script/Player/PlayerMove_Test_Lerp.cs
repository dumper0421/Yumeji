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

    //촬영
    [Header("Flash Settings")]
    [SerializeField] private Vector2Int flashTiles = new Vector2Int(3, 4);  // (가로 × 세로) 타일 수
    [SerializeField] private Vector2 tileSize = new Vector2(1f, 1f);  // 한 타일의 월드 크기
    [SerializeField] private LayerMask flashableLayer;                    // 피사체 탐지용 LayerMask
    [SerializeField] private GameObject flashVFXPrefab;                    // 플래시 VFX 프리팹
    [SerializeField] private float flashDuration = 0.3f;              // VFX 유지 시간(초)
    [SerializeField] private float flashVFXYOffset = 0.5f; // y축 오프셋 추가

    [SerializeField] private float shootCooldown = 2f;  // 쿨타임 (초)
    private float nextShootTime = 0f;
    [SerializeField] private AudioClip photoSFX;      // ① 인스펙터에 셔터음 클립 할당
    private AudioSource audioSource;

    //앉기
    [SerializeField] private float tileStepDuration = 0.12f;  // 1칸 스텝 시간
    [SerializeField] private bool bypassNoPassOnSit = true;    // 앉기 진입/복귀 시 NoPass 무시
    private bool isSitting = false;
    private Vector2 sitDir = Vector2.down;     // 앉을 때 바라본 방향(4방)
    private Vector3 standReturnPos;            // 서있던 자리(복귀 지점)

    private enum MoveAxis { None, Horizontal, Vertical }
    private MoveAxis _moveLock = MoveAxis.None;

    public CompanionSystem Companion = null;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }
    IEnumerator MoveCoroutine()
    {
        // 방향 입력은 Update()에서 설정된 vector 사용
        animator.SetFloat("DirX", vector.x);
        animator.SetFloat("DirY", vector.y);

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed_ : speed_;
        if (Companion != null) 
            Companion.MoveSpeed = moveSpeed;
        animator.SetFloat("AnimSpeed", moveSpeed);

        Vector2 direction = new Vector2(vector.x, vector.y);
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + direction;

        // 1. 자동 밀기 시도
        /*
        RaycastHit2D pushHit = Physics2D.Raycast(startPos, direction, 1f, LayerMask.GetMask("Pushable"));
        if (pushHit.collider != null)
        {
            var box = pushHit.collider.GetComponent<PushableObject>();
            if (box != null && box.TryPush(direction))
            {
                // 플레이어는 밀기 애니만 재생하고, 실제 이동은 PushableObject가 처리
                animator.SetBool("Pushing", true);
                yield break;
            }
        }
        */
        // 2. 이동 가능 체크
        boxCollider.enabled = false;
        RaycastHit2D hit = Physics2D.Linecast(startPos, targetPos, NoPass);
        boxCollider.enabled = true;
        if (hit.collider != null)
        {
            canMove = true;
            yield break;
        }

        if (Companion != null)
        {
            Companion.SetPosition(startPos, vector);
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
        Vector2 rawInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        Vector2 input = rawInput;

        if (_moveLock == MoveAxis.None)
        {
            if (rawInput.x != 0f) _moveLock = MoveAxis.Horizontal;
            else if (rawInput.y != 0f) _moveLock = MoveAxis.Vertical;
        }

                if (_moveLock == MoveAxis.Horizontal) input.y = 0f;
        else if (_moveLock == MoveAxis.Vertical) input.x = 0f;

        if (_moveLock == MoveAxis.Horizontal && rawInput.x == 0f)
            _moveLock = MoveAxis.None;
        else if (_moveLock == MoveAxis.Vertical && rawInput.y == 0f)
            _moveLock = MoveAxis.None;

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



        /////////  Take Shoot 애니메이션 트리 다시 만들 때
        /////////  EndTakeShoot 애니메이션 이벤트로 호출해야함
        /////////
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

            // 4) 셔터음 재생
            if (photoSFX != null)
            {
                audioSource.pitch = 2f;
                audioSource.PlayOneShot(photoSFX);
            }
        }

        // 앉기애니메이션
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (!isSitting && canMove)
            {
                StartCoroutine(CoSitEnter());   // 1칸 전진 + SitDown
                return;                         // 아래 입력 로직 차단
            }
            else if (isSitting)
            {
                StartCoroutine(CoSitExit());    // 1칸 후퇴 + SitDownEnd→Idle
                return;
            }
        }

        if (!canMove && !isSitting) return;

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
        // 1) 플레이어가 바라보는 방향 구하기
        Vector2 dir = new Vector2(animator.GetFloat("DirX"), animator.GetFloat("DirY"));
        if (dir == Vector2.zero)
            dir = Vector2.down;  // 기본 아래 방향

        // 2) OverlapBox 크기 및 중심 계산 (2D)
        Vector2 boxSize = new Vector2(flashTiles.x * tileSize.x, flashTiles.y * tileSize.y);
        Vector2 boxCenter = (Vector2)transform.position + dir * (boxSize.y / 2f);

        // 3) VFX 생성 위치: boxCenter에서 y축 오프셋
        Vector3 spawnPos = new Vector3(
            boxCenter.x,
            boxCenter.y + flashVFXYOffset,
            transform.position.z
        );

        // 4) 회전 각도 계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angleOffset = -90f; // 프리팹 기본이 아래(▼)라면 -90, 오른쪽(▶)이면 0, 위(▲)면 +90, 왼쪽(◀)면 +180
        Quaternion rot = Quaternion.Euler(0f, 0f, angle + angleOffset);

        // 5) VFX 생성 및 소멸 예약
        if (flashVFXPrefab != null)
        {
            GameObject vfx = Instantiate(flashVFXPrefab, spawnPos, rot);
            Destroy(vfx, flashDuration);
        }

        // 6) Flashable 레이어 탐지
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, flashableLayer);
        foreach (var col in hits)
        {
            IFlashable flashable = col.GetComponent<IFlashable>();
            if (flashable != null)
                flashable.OnPhotoTaken(false);
        }

    }

    // 4방으로 정규화 (애니 파라미터 기반)
    private Vector2 GetFacing4Dir()
    {
        float dx = animator.GetFloat("DirX");
        float dy = animator.GetFloat("DirY");
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
            return new Vector2(Mathf.Sign(dx), 0f);
        else
            return new Vector2(0f, Mathf.Sign(dy));
    }

    // 공용: 1칸 이동(선택적으로 충돌 무시)
    private IEnumerator CoStepOneTile(Vector2 dir, bool ignoreBlock, float duration)
    {
        Vector3 start = transform.position;
        Vector3 target = start + (Vector3)dir;

        // 평소 이동은 NoPass 체크하지만, 앉기/복귀는 좌석에 들어가야 하므로 옵션으로 무시
        bool prevEnabled = boxCollider.enabled;
        if (ignoreBlock) boxCollider.enabled = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(start, target, k);
            yield return null;
        }
        transform.position = target;

        if (ignoreBlock) boxCollider.enabled = prevEnabled;
    }

    // 앉기: 앞칸으로 1칸 들어가면서 SitDown 재생, 마지막 프레임에서 정지
    private IEnumerator CoSitEnter()
    {
       // canMove = false;

        // 바라보는 4방 추출
        sitDir = GetFacing4Dir();
        if (sitDir == Vector2.zero) sitDir = Vector2.down;

        // 복귀 지점 기록
        standReturnPos = transform.position;

        // 애니 트리거
        animator.ResetTrigger("SitDownEnd");
        animator.SetTrigger("SitDown");

        // 1칸 전진(좌석은 NoPass일 수 있으므로 무시)
        yield return StartCoroutine(CoStepOneTile(sitDir, bypassNoPassOnSit, tileStepDuration));

        isSitting = true;     // 앉은 상태 유지 (canMove=false 유지 → 이동/촬영 차단)
    }

    // 일어나기: 뒤칸(원래 자리)로 1칸 후퇴 후 Idle
    private IEnumerator CoSitExit()
    {
        // Idle로 바로 튀게 SitDownEnd 전이(Has Exit Time Off, Duration 0) 전제
        animator.ResetTrigger("SitDown");
        animator.SetTrigger("SitDownEnd");

        // 뒤로 1칸(복귀 지점으로)
        Vector3 cur = transform.position;
        Vector3 target = standReturnPos;
        // 안전: 복귀 방향은 -sitDir로 계산해도 동일
        yield return StartCoroutine(CoStepOneTile(-sitDir, bypassNoPassOnSit, tileStepDuration));

        // 상태 복구
        isSitting = false;
        canMove = true;
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