using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public enum S7S1State
{
    None,
    ReiExited,
    Finished,
}

public class Sequence7Scene1DialogueController : DialogueController<S7S1State>
{
    [Header("Haru")]
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [SerializeField]
    private Animator _playerAnimator;

    [Header("Rei")]
    [SerializeField]
    private GameObject _reiObject;

    [SerializeField]
    private PlayableDirector _director;

    [Header("Door / Scene Exit")]
    [SerializeField]
    private GameObject _sceneChangeTrigger;

    [Header("Dialogue IDs")]
    [SerializeField]
    private string _reiPartyEndId = "dialogue_9";

    [SerializeField]
    private string _haruMonologueStartId = "dialogue_10";

    [SerializeField]
    private string _haruMonologueEndId = "narration_2";

    private bool _reiHasLeft = false;

    protected override void ApplyWorldByState()
    {
        if (state == S7S1State.ReiExited || state == S7S1State.Finished)
        {
            _reiHasLeft = true;
            if (_reiObject != null)
                _reiObject.SetActive(false);
        }

        if (state == S7S1State.Finished)
        {
            if (_sceneChangeTrigger != null)
                _sceneChangeTrigger.SetActive(true);
        }
    }

    protected override void DialogueRunning(string dialogueId) { }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == _reiPartyEndId && !_reiHasLeft)
        {
            _reiHasLeft = true;
            _playerMove.enabled = false;
            _director.Play();
        }
        else if (dialogueId == _haruMonologueEndId)
        {
            state = S7S1State.Finished;
            PersistPuzzleState();

            if (_sceneChangeTrigger != null)
                _sceneChangeTrigger.SetActive(true);

            _playerMove.enabled = true;
            _playerAnimator.SetFloat("DirY", 1f);
            _playerAnimator.enabled = true;
        }
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    public void StartDialogue()
    {
        dialogueManager.StartDialogue(_haruMonologueStartId);
    }
}
