using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : InspectableObject
{
    public string StartDialogue;
    public DialogueManager DialogueManager;

    protected override void OnInspect()
    {
        if (!DialogueManager.isRunning)
            DialogueManager.StartDialogue(StartDialogue);

    }
}
