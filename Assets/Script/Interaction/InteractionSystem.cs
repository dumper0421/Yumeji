using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    public float InteractDistance = 1.0f; // 조사거리
    public LayerMask InteractableLayer;   

    private Vector2 lastDirection = Vector2.down; 

    private void Update()
    {
        UpdateFacingDirection(); // 방향 업데이트 (대각선 안되게 수정해야 됨)

        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F 키 입력 감지");
            TryInteract();
        }
    }

    // 방향 업데이트
    private void UpdateFacingDirection()
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        float dirY = Input.GetAxisRaw("Vertical");

        if (dirX != 0 || dirY != 0)
        {
            lastDirection = new Vector2(dirX, dirY).normalized;
        }
    }

    
    private void TryInteract()    {

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, lastDirection, InteractDistance, ~0); // 모든 레이어 감지

        Debug.DrawRay(transform.position, lastDirection * InteractDistance, Color.green, 0.5f);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.CompareTag("Player")||hit.collider.CompareTag("IgnoreRaycast"))
            {
                continue; 
            }
            

            Debug.Log($"Raycast 충돌 감지: {hit.collider.gameObject.name}");
            InteractWithObject(hit.collider.gameObject);
            return; 
        }

        Debug.Log("Raycast가 아무 유효한 오브젝트도 감지하지 못함");
    }

    // 📌 오브젝트와의 상호작용 실행 (태그 없이 컴포넌트 기반)
    private void InteractWithObject(GameObject obj)
    {
        bool interacted = false;

        if (obj.TryGetComponent<InspectableObject>(out var inspectable))
        {
            InspectObject(inspectable);
            interacted = true;
        }

        if (obj.TryGetComponent(out DialogueObject dialogueNPC))
        {
            StartDialogue(dialogueNPC);
            interacted = true;
        }

        if (obj.TryGetComponent(out SaveObject savePoint))
        {
            OpenSaveMenu(savePoint);
            interacted = true;
        }

        if (obj.TryGetComponent(out EventObject eventTrigger))
        {
            TriggerEvent(eventTrigger);
            interacted = true;
        }

        if (obj.TryGetComponent(out PushableObject pushable))
        {
            PushObject(pushable);
            interacted = true;
        }

        if (!interacted)
        {
            Debug.Log("상호작용 오브젝트 없음");
        }
    }

    // 📌 1. 오브젝트 조사
    private void InspectObject(InspectableObject obj)
    {
        Debug.Log("1. 조사 오브젝트 상호작용");
        obj.TryInspect();
    }

    // 📌 2. NPC 대화
    private void StartDialogue(DialogueObject obj)
    {
        Debug.Log("2. 대화 오브젝트 상호작용");
    }

    // 📌 3. 세이브 포인트
    private void OpenSaveMenu(SaveObject obj)
    {
        Debug.Log("3. 세이브 오브젝트 상호작용");
    }

    // 📌 4. 이벤트 트리거
    private void TriggerEvent(EventObject obj)
    {
        Debug.Log("4. 이벤트 오브젝트 상호작용");
    }

    // 📌 5. 밀기 오브젝트
    private void PushObject(PushableObject obj)
    {
        Debug.Log("5. 밀기 오브젝트 상호작용");

        if (obj.TryPush(lastDirection)) 
        {
            Debug.Log("밀기 성공!");
        }
        else
        {
            Debug.Log("밀기 실패 ");
        }
    }

    // 📌 Raycast 디버깅용
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + lastDirection * InteractDistance);
    }
}
