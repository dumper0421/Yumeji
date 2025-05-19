using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum S1S8State
{
    None
}

public class Sequence1Scene8DialogueController : DialogueController<S1S8State>
{
    [Header("Rabbit Man")]
    public DialogueObject RabbitManObject;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "RabbitMan_Hint7":
 
                RabbitManObject.StartDialogue = "RabbitMan_AfterHint";
                RabbitManObject.hasBeenInspected = false;  // 다시 조사 가능하게

                break;
        }

        TryProgress();
    }
    protected override void DialogueRunning(string dialogueId)
    {

    }
    protected override void HandleOption(string text, string nextId)
    {

    }

    protected override void OnPuzzleComplete()
    {

    }

    protected override void TryProgress()
    {


    }
}
