using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 대화 기반 퍼즐/상호작용 공통 베이스.
/// - 상태(enum)를 관리
/// - 대화 이벤트 구독
/// </summary>
public abstract class DialogueController<TState> : MonoBehaviour where TState : struct, Enum
{
    [Header("Dependencies")]
    public DialogueManager dialogueManager;
    public DialogueLoader dialogueLoader;
    public string sceneId;

    [Header("Save")]
    [Tooltip("씬 내에서 유니크한 퍼즐 ID (예: EmergencyDoor, RosePuzzle 등)")]
    [SerializeField] protected string puzzleId = "Puzzle";

    protected Dictionary<string, Dialogue> dialogues;
    public TState state;

    private bool _restoredOnce = false;

    protected virtual void Awake()
    {
        dialogues = dialogueLoader.LoadDialogues(sceneId);
        dialogueManager.Initialize(dialogues);

        dialogueManager.OnOptionSelected += OptionSelected;
        dialogueManager.OnDialogueComplete += DialogueCompleted;
        dialogueManager.OnDialogueAction += DialogueRunning;
    }

    protected virtual void Start()
    {
        RestorePuzzleState();
        ApplyWorldByState();
    }

    protected virtual void OnDestroy()
    {
        if (dialogueManager == null) return;

        dialogueManager.OnOptionSelected -= OptionSelected;
        dialogueManager.OnDialogueComplete -= DialogueCompleted;
        dialogueManager.OnDialogueAction -= DialogueRunning;
    }

    // ---------- 공통 저장 키 ----------
    protected string Key(string suffix)
        => $"{SceneManager.GetActiveScene().name}/{puzzleId}/{suffix}";

    protected const string KEY_STATE = "State";

    // ---------- 공통 Persist/Restore ----------
    protected void PersistPuzzleState()
    {
        if (GameManager.Instance == null) return;

        // enum -> int 저장
        GameManager.Instance.SetInt(Key(KEY_STATE), Convert.ToInt32(state));

        // 퍼즐별 추가 데이터 저장 (카운트 등)
        PersistExtra();
    }

    protected void RestorePuzzleState()
    {
        if (_restoredOnce) return;
        _restoredOnce = true;

        if (GameManager.Instance == null) return;

        int saved = GameManager.Instance.GetInt(Key(KEY_STATE), Convert.ToInt32(default(TState)));
        state = (TState)Enum.ToObject(typeof(TState), saved);

        RestoreExtra();
    }

    /// <summary>
    /// 로드 직후 "연출 재생 없이" 결과만 월드에 적용.
    /// 퍼즐마다 구현.
    /// </summary>
    protected abstract void ApplyWorldByState();

    /// <summary>
    /// 퍼즐별 추가 저장 데이터가 있으면 override.
    /// (기본: 없음)
    /// </summary>
    protected virtual void PersistExtra() { }

    /// <summary>
    /// 퍼즐별 추가 복원 데이터가 있으면 override.
    /// (기본: 없음)
    /// </summary>
    protected virtual void RestoreExtra() { }

    // ---------- 대화 이벤트 ----------
    protected virtual void OptionSelected(string text, string nextId)
    {
        HandleOption(text, nextId);
        TryProgress();
    }

    protected virtual void DialogueCompleted(string dialogueId)
    {
        HandleDialogueEnd(dialogueId);
        TryProgress();
    }

    protected virtual void DialogueRunning(string dialogueId) { }

    protected abstract void HandleOption(string text, string nextId);
    protected abstract void HandleDialogueEnd(string dialogueId);
    protected abstract void TryProgress();
    protected abstract void OnPuzzleComplete();

    public virtual void OnStartedDialogue(string DialogueID)
    {
        dialogueManager.StartDialogue(DialogueID);
    }
}
