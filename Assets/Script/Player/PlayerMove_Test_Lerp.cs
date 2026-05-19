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

    [Header("Flash VFX Prefabs")]
    [SerializeField] private GameObject flashVFXPrefab;      // 기본 플래시: 아래/좌/우
    [SerializeField] private GameObject flashVFXPrefabUp;    // 위쪽 전용 플래시: 플레이어 부분이 비어 있는 이미지

    [SerializeField] private float flashDuration = 0.3f;

    [Header("Flash VFX Position")]
    [SerializeField] private Vector2 flashVFXSideOffset = new Vector2(0.3f, 0.3f);
    // X: 좌우 촬영 시 옆으로 얼마나 밀지
    // Y: 좌우 촬영 시 아래/위로 얼마나 보정할지

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

        if (actionController == null || !actionController.IsPushing)
            canMove = true;
    }

    void Update()
    {
        if (actionController != null)
        {
            if (actionController.IsPushing || actionController.IsSitting)
                return;
        }

        if (PopupUIManager.Instance.SaveLoadPopup.gameObject.activeSelf)
            return;

        if (!canMove)
            return;

        Vector2 rawInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        Vector2 input = rawInput;

        if (_moveLock == MoveAxis.None)
        {
            if (rawInput.x != 0f)
                _moveLock = MoveAxis.Horizontal;
            else if (rawInput.y != 0f)
                _moveLock = MoveAxis.Vertical;
        }

        if (_moveLock == MoveAxis.Horizontal)
            input.y = 0f;
        else if (_moveLock == MoveAxis.Vertical)
            input.x = 0f;

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

        if (Input.GetKeyDown(KeyCode.C) && Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + shootCooldown;
            canMove = false;

            animator.SetFloat("DirX", vector.x);
            animator.SetFloat("DirY", vector.y);
            animator.SetTrigger("TakeShoot");

            if (photoSFX != null && audioSource != null)
            {
                audioSource.pitch = 2f;
                audioSource.PlayOneShot(photoSFX);
            }
        }
    }

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

    public void EndTakeShoot()
    {
        canMove = true;
    }

    public void HandleFlash()
    {
        Vector2 dir = new Vector2(
            animator.GetFloat("DirX"),
            animator.GetFloat("DirY")
        );

        if (dir == Vector2.zero)
            dir = Vector2.down;

        Vector2 boxSize = new Vector2(
            flashTiles.x * tileSize.x,
            flashTiles.y * tileSize.y
        );

        Vector2 boxCenter = (Vector2)transform.position + dir * (boxSize.y / 2f);

        Vector3 spawnPos = new Vector3(
            boxCenter.x,
            boxCenter.y,
            transform.position.z
        );

        if (Mathf.Abs(dir.x) > 0f)
        {
            spawnPos.x += flashVFXSideOffset.x * Mathf.Sign(dir.x);
            spawnPos.y += flashVFXSideOffset.y;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angleOffset = -90f;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle + angleOffset);

        GameObject selectedFlashPrefab = flashVFXPrefab;

        if (dir.y > 0f && flashVFXPrefabUp != null)
        {
            selectedFlashPrefab = flashVFXPrefabUp;
        }

        if (selectedFlashPrefab != null)
        {
            GameObject vfx = Instantiate(selectedFlashPrefab, spawnPos, rot);
            Destroy(vfx, flashDuration);
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            boxCenter,
            boxSize,
            0f,
            flashableLayer
        );

        foreach (var col in hits)
        {
            IFlashable flashable = col.GetComponent<IFlashable>();

            if (flashable != null)
            {
                flashable.OnPhotoTaken(false);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 dir = Vector2.down;

        if (animator != null)
        {
            dir = new Vector2(
                animator.GetFloat("DirX"),
                animator.GetFloat("DirY")
            );

            if (dir == Vector2.zero)
                dir = Vector2.down;
        }

        Vector2 boxSize = new Vector2(
            flashTiles.x * tileSize.x,
            flashTiles.y * tileSize.y
        );

        Vector2 boxCenter = (Vector2)transform.position + dir * (boxSize.y / 2f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawCube(boxCenter, boxSize);
    }

    public void SetCompanion(CompanionSystem companion, string companionName)
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