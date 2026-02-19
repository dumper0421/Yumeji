using UnityEngine;
using UnityEngine.Playables;

public enum S1S4State
{
    START = 0,
    EVENTHAPPEND = 1
}

public class Sequence1Scene4PuzzleController : DialogueController<S1S4State>
{
    [SerializeField] private PlayableDirector _director;

    protected override void Awake()
    {
        // ✅ 키 충돌 방지: 씬 내 유니크한 퍼즐 ID를 고정
        puzzleId = "S1S4";
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

    protected override void ApplyWorldByState()
    {
        if (_director == null) return;

        if (state == S1S4State.EVENTHAPPEND)
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
        }
    }

    protected override void HandleDialogueEnd(string dialogueId) { }
    protected override void HandleOption(string text, string nextId) { }
    protected override void TryProgress() { }
    protected override void OnPuzzleComplete() { }

    public void SaveState()
    {
        MarkEventHappened();
    }


    private void OnDirectorStopped(PlayableDirector d)
    {
        // 이미 완료면 중복 처리 방지
        if (state == S1S4State.EVENTHAPPEND) return;

        MarkEventHappened();
    }

    private void MarkEventHappened()
    {
        state = S1S4State.EVENTHAPPEND;

        PersistPuzzleState();
        ApplyWorldByState();
    }
}
