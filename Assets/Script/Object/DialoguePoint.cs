using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialoguePoint : MonoBehaviour
{
    public string StartDialogue;
    public DialogueManager DialogueManager;
    public bool DisablePoint = false;
    public float DisableTime = 2f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !DisablePoint)
        {
            if (!DialogueManager.transform.GetChild(2).gameObject.activeSelf)
                DialogueManager.StartDialogue(StartDialogue);

            StartCoroutine(Wait());
        }
    }

    IEnumerator Wait()
    {
        DisablePoint = true;
        yield return new WaitForSeconds(DisableTime);
        DisablePoint = false;
    }
}
