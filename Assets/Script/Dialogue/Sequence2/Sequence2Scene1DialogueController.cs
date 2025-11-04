using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
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
    private GameObject _badCustomer;

    [SerializeField]
    private FilmStageBox _filmStageBox;

    [SerializeField]
    private Sprite _badCustomerBackSprite;

    [SerializeField]
    private GameObject _Ken;
    [SerializeField]
    private Sprite _KenBackSprite;

    [SerializeField]
    private ItemData _yellowPhotoEnvelopeItemData;

    [SerializeField]
    private ItemData _SlideFilmItemData;

    [SerializeField]
    private InteractionSystem _interactionSystem;

    [SerializeField]
    private PlayableDirector _director;

    [SerializeField]
    private PlayableDirector _director2;


    [SerializeField]
    private ChangeLightTrigger _changeLightTrigger;

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "Storage_Box":
                InventoryManager.Instance.AddItem(_yellowPhotoEnvelopeItemData);
                _interactionSystem.InteractDistance = 3f;
                break;
            case "Guest_Dialogue17":
                _badCustomer.GetComponent<Animator>().enabled = true;
                _interactionSystem.InteractDistance = 1f;
                PlayDirector(_director);
                _filmStageBox.IsFinshBadGuest = true;
                _filmStageBox.hasBeenInspected = false;
                break;
            case "Storage_Box2":
                InventoryManager.Instance.AddItem(_SlideFilmItemData);
                _interactionSystem.InteractDistance = 3f;
                break;

            case "Ken_Dialogue28":
                _Ken.GetComponent<Animator>().enabled = true;
                PlayDirector(_director2);
                break;
            case "Ken_Dialogue29":
                _director2.Stop();
                break;
            case "Ken_Dialogue32":
                _changeLightTrigger.CanLeave = true;
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

    public void OnPlayerStoped()
    {
        _playerMove.enabled = false;
    }

    public void OnStartedDialogue(string DialogueID)
    {
        dialogueManager.StartDialogue(DialogueID);

        if (DialogueID == "Guest_Dialogue1")
        {
            _badCustomer.GetComponent<Animator>().enabled = false;
            _badCustomer.GetComponent<SpriteRenderer>().sprite = _badCustomerBackSprite;
        }
        else
        {
            _Ken.GetComponent<Animator>().enabled = false;
            _Ken.GetComponent<SpriteRenderer>().sprite = _KenBackSprite;
        }
    }

    private void PlayDirector(PlayableDirector _director)
    {
        _director.time = 0;
        _director.RebuildGraph();
        _director.Play();
    }
}

