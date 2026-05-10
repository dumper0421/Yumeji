using UnityEngine;

public class RayTalkTrigger : MonoBehaviour
{
    public Sequence6Scene1DialogueController dialogueController;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (dialogueController != null)
            {
                dialogueController.TryTalkToRay();
            }
        }
    }
}