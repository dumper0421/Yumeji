using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum S2S1State
{
    None
}

public class Sequence2Scene1DialogueController : DialogueController<S2S1State>
{
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;
    [SerializeField]
    private string _guestDialougeID;
    [SerializeField]
    private GameObject _badCustomer;
    [SerializeField]
    private Sprite _badCustomerBackSprite;

    protected override void HandleDialogueEnd(string dialogueId)
    {
       

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

    public void OnPlayerStoped()
    {
        _playerMove.enabled = false;
    }

    public void OnStartedDialogue()
    {
        dialogueManager.StartDialogue(_guestDialougeID);
        _badCustomer.GetComponent<Animator>().enabled = false;
        _badCustomer.GetComponent<SpriteRenderer>().sprite = _badCustomerBackSprite;
    }
}

