using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Yarn;

public enum S3S3State
{
    None
}
public class Sequence3Scene3DialogueController : DialogueController<S3S3State>
{
    public PlayableDirector ReiWalkDownDirector;
    public PlayableDirector TurnstileDirector;
    public PlayableDirector HoleDirector;

    public SpriteRenderer HaruSpirteRenderer;
    public SpriteRenderer ReiSpirteRenderer;

    public Animator HaruAnimator;
    public Animator ReiAnimator;

    public Sprite HaruStandRightSprite;
    public Sprite ReiStandLeftSprite;

    public PlayerMove_Test_Lerp HaruMove;
    public CompanionSystem ReiCompanionSystem;

    public List<DialogueObject> Seats;

    public AudioClip WaveBGM;

    public ExitTrigger ExitTrigger;

    public DialogueObject TicketOffice;

    protected override void Awake()
    {
        base.Awake();
        
    }

    private void Start()
    { 
        SoundManager.Instance.StopBGM();
        dialogueManager.StartDialogue("Haru_01");
        SoundManager.Instance.PlayBGM(WaveBGM);
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch(dialogueId)
        {
            case "HaruRei1_02": 
                Utility.PlayDirector(ReiWalkDownDirector);
                SoundManager.Instance.StopBGM();
                HaruMove.enabled = false;
                break;
            case "HaruRei1_11":
                HaruAnimator.enabled = true;
                ReiAnimator.enabled = true;
                HaruMove.SetCompanion(ReiCompanionSystem, "∑π¿Ã");
                break;
            case "HaruRei1_21":
                Utility.PlayDirector(TurnstileDirector);
                HaruMove.ReleaseCompanion();
                ReiCompanionSystem.gameObject.SetActive(false);
                foreach(var seat in Seats)
                {
                    seat.StartDialogue = "stationChairAlone";
                }
                TicketOffice.hasBeenInspected = true;
                break;

            case "HaruRei1_39":
                Utility.PlayDirector(HoleDirector);
                HaruAnimator.enabled = false;
                ExitTrigger.IsClear = true;
                break;
        }
    }
    protected override void DialogueRunning(string dialogueId)
    {
        switch (dialogueId)
        {
            case "HaruRei1_03":
                HaruAnimator.enabled = false;
                ReiAnimator.enabled = false;
                HaruSpirteRenderer.sprite = HaruStandRightSprite;
                ReiSpirteRenderer.sprite = ReiStandLeftSprite;
                break;

        }
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
    public override void OnStartedDialogue(string DialogueID)
    {
        base.OnStartedDialogue(DialogueID);
    }

    public void OnStopPlayer()
    {
        HaruMove.enabled = false;
    }

    public void OnMovePlayer()
    {
        HaruMove.enabled = true;
        HaruAnimator.enabled = true;
    }
}
