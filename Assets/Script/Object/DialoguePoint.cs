using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialoguePoint : MonoBehaviour
{
    public string StartDialogue;
    public DialogueManager DialogueManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!DialogueManager.transform.GetChild(2).gameObject.activeSelf)
                DialogueManager.StartDialogue(StartDialogue);
        }
    }
}
