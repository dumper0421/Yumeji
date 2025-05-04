using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DialogueController<TState> : MonoBehaviour where TState : struct, Enum
{
    [Header("Dependencies")]
    [Tooltip("씬에 배치된 DialogueManager 참조")]
    public DialogueManager dialogueManager;
    [Tooltip("대화 데이터 로더 (ScriptableObject 등)")]
    public DialogueLoader dialogueLoader;
    [Tooltip("이 컨트롤러가 담당할 씬 ID")]
    public string sceneId;

    protected Dictionary<string, Dialogue> dialogues;
    public                                                                       TState state;

    protected virtual void Awake()
    {
        // 대화 데이터 로드
        dialogues = dialogueLoader.LoadDialogues(sceneId);
        dialogueManager.Initialize(dialogues);

        // 이벤트 구독
        dialogueManager.OnOptionSelected += OptionSelected;
        dialogueManager.OnDialogueComplete += DialogueCompleted;
    }

    protected virtual void OnDestroy()                                                  
    {
        // 이벤트 해제
        dialogueManager.OnOptionSelected -= OptionSelected;
        dialogueManager.OnDialogueComplete -= DialogueCompleted;                                                                                            
    }

    /// <summary>
    /// 옵션 선택 시 호출됩니다.
    /// </summary>
    protected virtual void OptionSelected(string text, string nextId)
    {
        HandleOption(text, nextId);
        TryProgress();
    }

    /// <summary>
    /// 대화 완료 시 호출됩니다.
    /// </summary>
    protected virtual void DialogueCompleted(string dialogueId)
    {
        HandleDialogueEnd(dialogueId);
        TryProgress();
    }

    /// <summary>
    /// 옵션 선택에 따른 상태 전환 로직을 구현하세요.
    /// </summary>
    protected abstract void HandleOption(string text, string nextId);

    /// <summary>
    /// 대화 완료에 따른 상태 전환 로직을 구현하세요.
    /// </summary>
    protected abstract void HandleDialogueEnd(string dialogueId);

    /// <summary>
    /// 상태가 변화한 후 퍼즐 완료 조건을 검사하고, 완료 시 OnPuzzleComplete을 호출합니다.
    /// </summary>
    protected abstract void TryProgress();

    /// <summary>
    /// 퍼즐 완료 시 호출됩니다.
    /// </summary>
    protected abstract void OnPuzzleComplete();

}
