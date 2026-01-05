using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DialogueController<TState> : MonoBehaviour where TState : struct, Enum
{
    [Header("Dependencies")]
    [Tooltip("씬에 배치된 DialogueManager 참조")]
    public DialogueManager dialogueManager;
    [Tooltip("대화 데이터 로더")]
    public DialogueLoader dialogueLoader;
    public string sceneId;

    protected Dictionary<string, Dialogue> dialogues;
    public TState state;

    protected virtual void Awake()
    {
        // 대화 데이터 로드
        dialogues = dialogueLoader.LoadDialogues(sceneId);
        dialogueManager.Initialize(dialogues);

        // 이벤트 구독
        dialogueManager.OnOptionSelected += OptionSelected;
        dialogueManager.OnDialogueComplete += DialogueCompleted;
        dialogueManager.OnDialogueAction += DialogueRunning;
    }

    protected virtual void OnDestroy()                                                  
    {
        // 이벤트 해제
        dialogueManager.OnOptionSelected -= OptionSelected;
        dialogueManager.OnDialogueComplete -= DialogueCompleted;
        dialogueManager.OnDialogueAction -= DialogueRunning;
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

    protected virtual void DialogueRunning(string dialogueId)
    {
   
    }

   
    protected abstract void HandleOption(string text, string nextId);

 
    protected abstract void HandleDialogueEnd(string dialogueId);


    protected abstract void TryProgress();

    protected abstract void OnPuzzleComplete();

}
