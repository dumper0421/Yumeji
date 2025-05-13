using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum S1S5State
{
    None
}

public class Sequence1Scene5DialogueController : DialogueController<S1S5State>
{
    public ItemData TornTrianTicketData;
    public DialogueObject StationChairObject;
    public DialogueObject TrainDoorObject;

    public string nextSceneName;

    // Start is called before the first frame update
    void Start()
    {
        
    }
        
    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "StationChair_Ticket":
                InventoryManager.Instance.AddItem(TornTrianTicketData);

                StationChairObject.StartDialogue = "StationChair";
                StationChairObject.hasBeenInspected = false;

                TrainDoorObject.StartDialogue = "TrainDoor_WithTicket";
                TrainDoorObject.hasBeenInspected = false;
                break;


            case "TrainDoor_WithTicket":
                TornTrianTicketData.Use();
                SceneManager.LoadScene(nextSceneName);
                break;

            case "TrainDoor_NoTicket":
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

