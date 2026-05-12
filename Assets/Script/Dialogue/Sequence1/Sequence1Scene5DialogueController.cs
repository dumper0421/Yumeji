using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;


public enum S1S5State
{
    None = 0,
    EVENTHAPPEND = 1
}

public class Sequence1Scene5DialogueController : DialogueController<S1S5State>
{
    [SerializeField] private PlayableDirector _director;

    public ItemData TornTrianTicketData;
    public DialogueObject StationChairObject;
    public DialogueObject TrainDoorObject;
    public GameObject InteractionLight;
    public GameObject teleport;
    
    protected override void Awake()
    {
         base.Awake();
    }
    private void OnEnable()
    {
        if (_director != null)
            _director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (_director != null)
            _director.stopped -= OnDirectorStopped;
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "StationChair_Ticket":
                InventoryManager.Instance.AddItem(TornTrianTicketData);
                InteractionLight.SetActive(false);
                StationChairObject.StartDialogue = "StationChair";
                StationChairObject.hasBeenInspected = false;

                TrainDoorObject.StartDialogue = "TrainDoor_WithTicket";
                TrainDoorObject.hasBeenInspected = false;
                break;


            case "TrainDoor_WithTicket":
                TornTrianTicketData.Use();
                teleport.SetActive(true);
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

    protected override void ApplyWorldByState()
    {
        if (_director == null) return;

        if (state == S1S5State.EVENTHAPPEND)
        {
            if (_director.state == PlayState.Playing)
                _director.Stop();


            _director.time = _director.duration;
            _director.Evaluate();

            _director.enabled = false;
        }
        else
        {
            _director.enabled = true;
            _director.Play();
        }
    }

    public void SaveState()
    {
        MarkEventHappened();
    }


    private void OnDirectorStopped(PlayableDirector d)
    {
        // 이미 완료면 중복 처리 방지
        if (state == S1S5State.EVENTHAPPEND) return;

        MarkEventHappened();
    }

    private void MarkEventHappened()
    {
        state = S1S5State.EVENTHAPPEND;

        PersistPuzzleState();
        ApplyWorldByState();
    }
}

