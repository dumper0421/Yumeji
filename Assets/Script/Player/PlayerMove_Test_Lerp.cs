using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove_Test_Lerp : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public LayerMask NoPass;

    [SerializeField] private float speed_ = 1f;
    [SerializeField] private float runSpeed_ = 2f;
    [SerializeField] private float originSpeed = 1f;


    public Vector3 vector;
    [HideInInspector] public bool canMove = true;
    public Animator animator;

    [Header("Flash Settings")]
    [SerializeField] private Vector2Int flashTiles = new Vector2Int(3, 4);
    [SerializeField] private Vector2 tileSize = new Vector2(1f, 1f);
    [SerializeField] private LayerMask flashableLayer;
    [SerializeField] private GameObject flashVFXPrefab;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float flashVFXYOffset = 0.5f;

    [SerializeField] private float shootCooldown = 2f;
    private float nextShootTime = 0f;
    [SerializeField] private AudioClip photoSFX;
    private AudioSource audioSource;

    private enum MoveAxis { None, Horizontal, Vertical }
    private MoveAxis _moveLock = MoveAxis.None;

    public CompanionSystem Companion = null;

    private PlayerActionController actionController;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        actionController = GetComponent<PlayerActionController>();
    }

    IEnumerator MoveCoroutine()
    {
        animator.SetFloat("DirX", vector.x);
        animator.SetFloat("DirY", vector.y);

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed_ : speed_;
        if (Companion != null)
        {
            Companion.MoveSpeed = moveSpeed;
            Companion.GetComponent<Animator>().enabled = true;
        }
        animator.SetFloat("AnimSpeed", moveSpeed);

        Vector2 direction = new Vector2(vector.x, vector.y);
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + direction;

        // 충돌 체크
        boxCollider.enabled = false;
        RaycastHit2D hit = Physics2D.Linecast(startPos, targetPos, NoPass);
        boxCollider.enabled = true;
        if (hit.collider != null)
        {
            if (actionController == null || !actionController.IsPushing)
                canMove = true;
            yield break;
        }

        if (Companion != null)
        {
            Companion.SetPosition(startPos, vector);
        }

        float elapsedTime = 0f;
        float moveDuration = 0.2f / moveSpeed;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        // ★ 밀기 중이 아닐 때만 이동 해제
        if (actionController == null || !actionController.IsPushing)
            canMove = true;
    }
    

    void Update()
    {
        // ★ 밀기/앉기 중이면 이동/촬영 모두 차단
        if (actionController != null)
        {
            if (actionController.IsPushing || actionController.IsSitting)
                return;
        }

        // ★ canMove가 false면 (밀기/촬영/앉기 중) 아무 것도 안 함
        if (!canMove) return;

        // 1) 이동 입력 처리
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
            canMove = false;
            vector = new Vector3(input.x, input.y, 0f);
            StartCoroutine(MoveCoroutine());
            return;
        }

        // 2) 촬영 (정지 상태에서만)
        if (Input.GetKeyDown(KeyCode.C)
            && Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + shootCooldown;
            canMove = false;

            animator.SetFloat("DirX", vector.x);
            animator.SetFloat("DirY", vector.y);
            animator.SetTrigger("TakeShoot");

            if (photoSFX != null)
            {
                audioSource.pitch = 2f;
                audioSource.PlayOneShot(photoSFX);
            }
        }
    }

    // Teleport 호출용
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

    // 촬영 애니메이션 이벤트에서 호출
    public void EndTakeShoot()
    {
        canMove = true;
    }

    public void HandleFlash()
    {
        Vector2 dir = new Vector2(animator.GetFloat("DirX"), animator.GetFloat("DirY"));
        if (dir == Vector2.zero)
            dir = Vector2.down;

        Vector2 boxSize = new Vector2(flashTiles.x * tileSize.x, flashTiles.y * tileSize.y);
        Vector2 boxCenter = (Vector2)transform.position + dir * (boxSize.y / 2f);

        Vector3 spawnPos = new Vector3(
            boxCenter.x,
            boxCenter.y + flashVFXYOffset,
            transform.position.z
        );

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angleOffset = -90f;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle + angleOffset);

        if (flashVFXPrefab != null)
        {
            GameObject vfx = Instantiate(flashVFXPrefab, spawnPos, rot);
            Destroy(vfx, flashDuration);
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, flashableLayer);
        foreach (var col in hits)
        {
            IFlashable flashable = col.GetComponent<IFlashable>();
            if (flashable != null)
                flashable.OnPhotoTaken(false);
        }
    }

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

    public void SetCompanion(CompanionSystem companion,string companionName)
    {
        Companion = companion;
        GameManager.Instance.AddCompanion(companionName);
    }

    public void ReleaseCompanion()
    {
        Companion = null;
        GameManager.Instance.InitCompanion();
    }

    public IEnumerator MoveToPositionCoroutine(Vector3 targetPos, float speed = 1)
    {
        canMove = false;

        Vector3 startPos = transform.position;
        Vector3 dir = (targetPos - startPos).normalized;

        animator.SetFloat("DirX", dir.x);
        animator.SetFloat("DirY", dir.y);

        animator.SetBool("Walking", true);
        animator.SetFloat("AnimSpeed", 1);

        if (Companion != null)
            Companion.MoveSpeed = speed;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            Vector3 moveDir = (targetPos - transform.position).normalized;

            animator.SetFloat("DirX", moveDir.x);
            animator.SetFloat("DirY", moveDir.y);

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;

        animator.SetBool("Walking", false);

        canMove = true;
    }

    public void ChangeSpeed(float speed, float runSpeed)
    {
        originSpeed = speed_;
        speed_ = speed;
        runSpeed_ = runSpeed;
    }

    public void RevertSpeed()
    {
        speed_ = originSpeed;
        runSpeed_ = speed_ * 2;
    }
}
