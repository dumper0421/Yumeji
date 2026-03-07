using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private string dialogueId = "Lay_Dialogue01";

    [Header("Options")]
    [SerializeField] private bool once = true;

    private bool _triggered = false;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered && once) {
                return; 
        }
        if (!other.CompareTag("Player"))
        {

      
            return;
        }
        if (dialogueManager == null)
        {
            return;
        }

        Debug.Log("대화실행 ");
        _triggered = true;
        dialogueManager.StartDialogue(dialogueId);
    }
}