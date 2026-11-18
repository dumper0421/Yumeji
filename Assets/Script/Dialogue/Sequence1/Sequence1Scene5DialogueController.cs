
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

    // 추가: 세이브 구역 출구
    [Header("Exit Wall")]
    [SerializeField] private SaveRoomExitObject exitWall;

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

    // 추가: 출구 선택지 처리
    protected override void HandleOption(string text, string nextId)
    {
        if (text == "이동" && nextId == "SaveRoom_Exit_Move")
        {
            if (exitWall != null)
            {
                exitWall.MoveToNextScene();
            }
            else
            {
                Debug.LogWarning("ExitWall이 연결되지 않았습니다.");
            }
        }

        // 뒤로가기는 nextId가 "End"이므로
        // DialogueManager에서 기존 방식대로 대화 종료
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
        if (state == S1S5State.EVENTHAPPEND)
            return;

        MarkEventHappened();
    }

    private void MarkEventHappened()
    {
        state = S1S5State.EVENTHAPPEND;

        PersistPuzzleState();
        ApplyWorldByState();
    }
}
