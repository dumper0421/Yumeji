using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    public float InteractDistance = 1.0f;
    public LayerMask InteractableLayer;

    [Tooltip("비워두면 씬에서 자동으로 찾음. 대화 중 상호작용 차단에 사용된다.")]
    [SerializeField] private DialogueManager dialogueManager;

    private Vector2 lastDirection = Vector2.down;
    private PlayerActionController actionController;
    private PlayerMove_Test_Lerp move;

    // 대화가 Space를 쓰고 있는 동안에는 상호작용 금지.
    // ConsumedInputThisFrame이 없으면, 마지막 줄을 넘긴 Space가 같은 프레임에
    // (EndDialogue가 move를 다시 켠 뒤) 상호작용을 재발동해 대사가 무한 반복된다.
    private bool IsDialogueUsingInput =>
        dialogueManager != null
        && (dialogueManager.isRunning || dialogueManager.ConsumedInputThisFrame);

    private void Awake()
    {
        actionController = GetComponent<PlayerActionController>();
        move = GetComponent<PlayerMove_Test_Lerp>();

        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();
    }

    private void Start()
    {
        // 아직 입력이 없었을 때의 상호작용 방향을 시작 방향과 맞춘다
        // (PlayerMove_Test_Lerp.Awake에서 Facing이 이미 정해져 있음)
        if (move != null)
            lastDirection = move.Facing;
    }

    private void Update()
    {
        UpdateFacingDirection();

        if (IsDialogueUsingInput)
            return;

        // ★ 밀기 중이면 상호작용 완전 차단
        if (actionController != null && actionController.IsPushing)
            return;

        // ★ 앉아 있는 동안에는 F = 일어나기 전용
        if (actionController != null && actionController.IsSitting)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                actionController.StandUp();
            return;
        }

        // ★ 이동 불가 상태 또는 컴포넌트 비활성(대화 중 등) 차단
        if (move != null && (!move.canMove || !move.enabled))
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryInteract();
        }
    }

    private void UpdateFacingDirection()
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        float dirY = Input.GetAxisRaw("Vertical");

        if (dirX != 0 || dirY != 0)
        {
            lastDirection = new Vector2(dirX, dirY).normalized;
        }
    }

    private void TryInteract()
    {
        Vector2 origin = transform.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            lastDirection,
            InteractDistance,
            InteractableLayer
        );

        Debug.DrawRay(origin, lastDirection * InteractDistance, Color.green, 0.2f);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("IgnoreRaycast"))
                 continue;

            GameObject obj = hit.collider.gameObject;
             InteractWithObject(obj);
        }
    }

    private void InteractWithObject(GameObject obj)
    {
        bool interacted = false;

        if (obj.TryGetComponent<InspectableObject>(out var inspectable))
        {
            inspectable.TryInspect();
            interacted = true;
        }

        if (obj.TryGetComponent(out DialogueObject dialogueNPC))
        {
            Debug.Log("대화 상호작용");
            interacted = true;
        }

        if (obj.TryGetComponent(out SitObject seat))
        {
            Debug.Log("앉기 상호작용");
            if (actionController != null)
            {
                if (!actionController.IsSitting)
                    actionController.SitAt(seat);
                else if (actionController.CurrentSeat == seat)
                    actionController.StandUp();
            }
            interacted = true;
        }

        if (obj.TryGetComponent(out SaveObject savePoint))
        {
            Debug.Log("세이브 상호작용");
            interacted = true;
        }

        if (obj.TryGetComponent(out EventObject eventTrigger))
        {
            Debug.Log("이벤트 상호작용");
            interacted = true;
        }

        if (obj.TryGetComponent(out PushableObject pushable))
        {
            Debug.Log("밀기 상호작용");
            TryPush(pushable);
            interacted = true;
        }

        if (!interacted)
        {
            Debug.Log("상호작용 가능한 컴포넌트 없음");
        }
    }

    private void TryPush(PushableObject box)
    {
        if (actionController != null && actionController.IsSitting)
        {
            Debug.Log("앉아 있는 동안에는 밀 수 없음");
            return;
        }

        Vector2 dir = lastDirection;
        if (dir == Vector2.zero) dir = Vector2.down;

        if (box.TryPush(dir))
        {
            if (actionController != null)
            {
                actionController.StartPushMove(dir, box.pushDuration);
            }
        }
        else
        {
            Debug.Log("밀기 실패");
        }
    }
}
