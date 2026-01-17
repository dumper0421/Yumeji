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
    public PlayableDirector ReyWalkDownDirector;
    public PlayableDirector TurnstileDirector;
    public PlayableDirector HoleDirector;

    public SpriteRenderer HaruSpirteRenderer;
    public SpriteRenderer ReySpirteRenderer;

    public Animator HaruAnimator;
    public Animator ReyAnimator;

    public Sprite HaruStandRightSprite;
    public Sprite ReyStandLeftSprite;

    public PlayerMove_Test_Lerp HaruMove;
    public CompanionSystem ReyCompanionSystem;
    protected override void Awake()
    {
        base.Awake();
        
    }

    private void Start()
    {
        dialogueManager.StartDialogue("Haru_01");
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch(dialogueId)
        {
            case "HaruRey1_02": 
                Utility.PlayDirector(ReyWalkDownDirector);
                break;
            case "HaruRey1_11":
                HaruAnimator.enabled = true;
                ReyAnimator.enabled = true;
                HaruMove.SetCompanion(ReyCompanionSystem, "∑π¿Ã");
                break;
            case "HaruRey1_21":
                Utility.PlayDirector(TurnstileDirector);
                HaruMove.ReleaseCompanion();
                ReyCompanionSystem.gameObject.SetActive(false);
                break;

            case "HaruRey1_39":
                Utility.PlayDirector(HoleDirector);
                HaruAnimator.enabled = false;
                break;
        }
    }
    protected override void DialogueRunning(string dialogueId)
    {
        switch (dialogueId)
        {
            case "HaruRey1_03":
                HaruAnimator.enabled = false;
                ReyAnimator.enabled = false;
                HaruSpirteRenderer.sprite = HaruStandRightSprite;
                ReySpirteRenderer.sprite = ReyStandLeftSprite;
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
