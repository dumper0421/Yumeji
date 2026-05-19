using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum S5S1State
{
    START = 0,
    EVENTHAPPEND = 1
}
public class Sequence5Scene1DialogueController : DialogueController<S5S1State>
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;

    [SerializeField] private SpriteRenderer reiSpriteRenderer;
    [SerializeField] private Sprite _reiFrontStand;

    [SerializeField] private Animator _haruAnimator;
    [SerializeField] private SpriteRenderer _haruSpriteRenderer;
    [SerializeField] private Sprite _haruFrontStand;
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


    protected override void Awake()
    {
        puzzleId = "S5S1";
        base.Awake();
    }

    protected override void ApplyWorldByState()
    {
        if (_director == null) return;

        if (state == S5S1State.EVENTHAPPEND)
        {
            if (_director.state == PlayState.Playing)
                _director.Stop();

            _director.enabled = false;
        }
        else
        {
            _director.enabled = true;
        }
    }

    protected override void DialogueRunning(string dialogueId)
    {
        switch (dialogueId)
        {
            case "Sequence5Scene1_Ray_OnlyToday":
                _haruAnimator.enabled = false;
                _haruSpriteRenderer.sprite = _haruFrontStand;
                break;

        }
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
       if (dialogueId == "Sequence5Scene1_Ray_WhatsHidden")
        {
            _playerMove.ReleaseCompanion();
            MarkEventHappened();
            StartCoroutine(ReiExitCo());
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

    private void OnDirectorStopped(PlayableDirector d)
    {
        // 이미 완료면 중복 처리 방지
        if (state == S5S1State.EVENTHAPPEND) return;

        MarkEventHappened();
    }

    private void MarkEventHappened()
    {
        state = S5S1State.EVENTHAPPEND;

        PersistPuzzleState();
        ApplyWorldByState();
    }

    IEnumerator ReiExitCo()
    {
        reiSpriteRenderer.sprite = _reiFrontStand;
        _haruSpriteRenderer.sprite = _haruFrontStand;
        _playerMove.enabled = false;
        _haruAnimator.enabled = false;
        yield return new WaitForSeconds(0.5f);
        reiSpriteRenderer.gameObject.SetActive(false);
        _playerMove.enabled = true;
        _haruAnimator.enabled = true;
    }
}
