using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;
using UnityEngine.EventSystems;

public class InteractionSystem : MonoBehaviour
{
    public float interactDistance = 1.5f;
    public LayerMask interactableLayer;  

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            TryInteract();
        }
    }


    private void TryInteract()
    {
        Vector2 direction = GetFacingDirection(); 
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, interactDistance, interactableLayer);

        if (hit.collider != null)
        {
            InteractWithObject(hit.collider.gameObject);
        }
    }

    //방향
    private Vector2 GetFacingDirection()
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        float dirY = Input.GetAxisRaw("Vertical");

        if (dirX != 0)
        {
            return new Vector2(dirX, 0).normalized;
        }
        else if (dirY != 0)
        {
            return new Vector2(0, dirY).normalized;
        }

        return Vector2.zero; 
    }

    private void InteractWithObject(GameObject obj)
    {
        if (obj.CompareTag("Inspectable"))
        {
            InspectObject(obj);
        }
        /*
        else if (obj.CompareTag("DialogueNPC"))
        {
            StartDialogue(obj);
        }
        else if (obj.CompareTag("Item"))
        {
            PickUpItem(obj);
        }
        else if (obj.CompareTag("SavePoint"))
        {
            OpenSaveMenu(obj);
        }
        else if (obj.CompareTag("Trigger"))
        {
            TriggerEvent(obj);
        }
        else if (obj.CompareTag("Pushable"))
        {
            PushObject(obj);
        }
    */
    }

    // 📌 1. 오브젝트 조사
    private void InspectObject(GameObject obj)
    {
       
        Debug.Log("오브젝트 조사 상호작용(RayCast)");
    }
    /*
    private void StartDialogue(GameObject obj)
    {
        DialogueNPC npc = obj.GetComponent<DialogueNPC>();
        if (npc != null)
        {
            npc.StartDialogue();
        }
    }

 
    private void PickUpItem(GameObject obj)
    {
        Item item = obj.GetComponent<Item>();
        if (item != null)
        {
            InventorySystem.Instance.AddItem(item);
            Destroy(obj); // 아이템 삭제
        }
    }

    private void OpenSaveMenu(GameObject obj)
    {
        SavePoint save = obj.GetComponent<SavePoint>();
        if (save != null)
        {
            save.OpenSaveUI();
        }
    }

    private void TriggerEvent(GameObject obj)
    {
        EventTrigger eventTrigger = obj.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.TriggerEvent();
        }
    }

    private void PushObject(GameObject obj)
    {
        PushableObject pushable = obj.GetComponent<PushableObject>();
        if (pushable != null)
        {
            pushable.Push(transform.position);
        }
    }
    */

    private void OnDrawGizmosSelected()
    {
        Vector2 direction = GetFacingDirection();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * interactDistance);
    }
}
