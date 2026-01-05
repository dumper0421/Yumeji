using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대화(Conversation) 기반 퍼즐/상호작용 로직의 공통 베이스 클래스.
/// <para>
/// - 제네릭 상태 TState(열거형)를 사용해 각 퍼즐의 진행 상태를 타입 안전하게 관리합니다.
/// - DialogueManager 이벤트(옵션 선택, 대화 완료, 대화 진행 중)를 구독하여
///   상태 전이(HandleOption/HandleDialogueEnd)와 진행 판단(TryProgress)을 일관되게 수행합니다.
/// - 구체 퍼즐은 이 클래스를 상속하고 추상 메서드들을 구현합니다.
/// </para>
/// </summary>
/// <typeparam name="TState">퍼즐 진행 상태를 나타내는 열거형 타입</typeparam>
public abstract class DialogueController<TState> : MonoBehaviour where TState : struct, Enum
{
    [Header("Dependencies")]
    [Tooltip("씬 내에서 대화 UI/입력을 관리하는 DialogueManager")]
    public DialogueManager dialogueManager;

    [Tooltip("대화 스크립트를 로드하는 로더 ")]
    public DialogueLoader dialogueLoader;

    [Tooltip("이 컨트롤러가 사용할 대화 세트의 Scene/Group ID")]
    public string sceneId;

    /// <summary>
    /// 대화 스크립트 캐시. sceneId로 로드된 Dialogue 집합.
    /// </summary>
    protected Dictionary<string, Dialogue> dialogues;

    /// <summary>
    /// 현재 퍼즐/상호작용의 진행 상태.
    /// </summary>
    public TState state;

    /// <summary>
    /// 초기화 훅.
    /// </summary>
    protected virtual void Awake()
    {
        // 대화 데이터 로드 및 초기화
        dialogues = dialogueLoader.LoadDialogues(sceneId);
        dialogueManager.Initialize(dialogues);

        // 이벤트 구독
        dialogueManager.OnOptionSelected += OptionSelected;
        dialogueManager.OnDialogueComplete += DialogueCompleted;
        dialogueManager.OnDialogueAction += DialogueRunning;
    }

    /// <summary>
    /// 구독 해제로 메모리 누수/중복 호출 방지.
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 이벤트 구독 해제
        dialogueManager.OnOptionSelected -= OptionSelected;
        dialogueManager.OnDialogueComplete -= DialogueCompleted;
        dialogueManager.OnDialogueAction -= DialogueRunning;
    }

    /// <summary>
    /// 대화 옵션이 선택되었을 때 호출되는 기본 핸들러.
    /// </summary>

    protected virtual void OptionSelected(string text, string nextId)
    {
        HandleOption(text, nextId);
        TryProgress();
    }

    /// <summary>
    /// 하나의 대화 블록이 종료되었을 때 호출되는 기본 핸들러.
    /// </summary>
    protected virtual void DialogueCompleted(string dialogueId)
    {
        HandleDialogueEnd(dialogueId);
        TryProgress();
    }

    /// <summary>
    /// 대화 라인이 표시(진행)되는 순간 호출되는 이벤트 훅.
    /// </summary>
    protected virtual void DialogueRunning(string dialogueId)
    {
        // Optional: 진행 중 연출 훅
    }

    /// <summary>
    /// 옵션 선택 시 상태 전이/연출/후속 처리 로직을 구현합니다.
    /// </summary>
    protected abstract void HandleOption(string text, string nextId);

    /// <summary>
    /// 대화 블록 종료 시 상태 전이/연출/후속 처리 로직을 구현합니다.
    /// </summary>
    protected abstract void HandleDialogueEnd(string dialogueId);

    /// <summary>
    /// 현재 상태를 평가하여 퍼즐 진행(완료 조건 충족 여부)을 판별합니다.
    /// </summary>
    protected abstract void TryProgress();

    /// <summary>
    /// 퍼즐 완료 시 호출되는 콜백.
    /// </summary>
    protected abstract void OnPuzzleComplete();
}
