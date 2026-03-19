using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum S4S2State
{

}
public class Sequence4Scene2DialogueController : DialogueController<S4S2State>
{
    [SerializeField] private string _DialogueID;
    [SerializeField] private Animator _haruAnimator;
    [SerializeField] private Animator _reiAnimator;
    [SerializeField] private Sprite _haruRightSprite;
    [SerializeField] private SpriteRenderer _haruSpriteRenderer;

    protected override void ApplyWorldByState()
    {
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch(dialogueId)
        {
            case "Haru_Monologue":
                _haruAnimator.enabled = true;
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

    public void StartDialogue()
    {
        if(dialogueManager != null) 
            dialogueManager.StartDialogue(_DialogueID);

        _haruAnimator.enabled = false;
        _haruSpriteRenderer.sprite = _haruRightSprite;

        _reiAnimator.enabled = false;
    }
}
