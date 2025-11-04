using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilmStageBox : DialogueObject
{

    [SerializeField]
    private string _slideFilmDialogue;

    public bool IsFinshBadGuest = false;

    [SerializeField]
    protected override void OnInspect()
    {
        if (!DialogueManager.isRunning)
            DialogueManager.StartDialogue(StartDialogue);
        if (IsFinshBadGuest)
            DialogueManager.StartDialogue(_slideFilmDialogue);
    }
}
