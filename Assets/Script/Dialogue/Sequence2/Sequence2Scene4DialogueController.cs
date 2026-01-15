using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// S2S4: (1) 기상 독백 -> (2) 방 오브젝트 5개 조사(서로 다른 ID) -> (3) 전화 울림 -> (4) 전화 상호작용 후 루나 통화 -> (5) 통화 후 문 오브젝트(또는 블록) 삭제
/// - 전화기/문 애니메이션 없음
/// - JSON id는 사용자 제공 버전(Wardrobe, Haru_Door_Locked/Open) 기준으로 맞춘다.
/// </summary>
public enum S2S4State
{
    None,
    Searching,
    PhoneRinging,
    CallFinished,
    DoorCleared
}

public class Sequence2Scene4DialogueController : DialogueController<S2S4State>
{
    [Header("Dialogue Objects")]
    [SerializeField] private DialogueObject phoneObject;      // Phone DialogueObject
    [SerializeField] private DialogueObject frontDoorObject;  // Door DialogueObject (대사 담당)

    [Header("Door Block Object (Optional)")]
    [Tooltip("문을 막는 물체(콜라이더/스프라이트). 통화 이후 이 오브젝트를 삭제한다. 비워두면 frontDoorObject.gameObject를 삭제한다.")]
    [SerializeField] private GameObject frontDoorBlockObject;

    [Header("Scene Start Dialogue")]
    [SerializeField] private string wakeUpMonologueId = "WakeUp_Monologue";

    [Header("Non-phone one-liner IDs (UNIQUE)")]
    [Tooltip("전화기 제외 조사 대상 1줄 대사 ID들. 이 리스트에 있는 서로 다른 ID를 N개 완료하면 전화가 울린다.")]
    [SerializeField]
    private List<string> nonPhoneOneLinerIds = new List<string>
    {
        "Bed", "Window", "Drawer", "Wardrobe", "FishTank"
    };

    [SerializeField] private int requiredInspectCountToRing = 5;

    [Header("Phone Dialogue IDs (JSON)")]
    [SerializeField] private string phoneSilentId = "Phone_Silent";
    [SerializeField] private string phoneRingingId = "Phone_Ringing";
    [SerializeField] private string phoneCallId = "Phone_Call";
    [SerializeField] private string afterCallMonologueId = "After_Call_Monologue";

    [Header("Door Dialogue IDs (JSON)")]
    [SerializeField] private string doorLockedId = "Haru_Door_Locked";
    [SerializeField] private string doorOpenId = "Haru_Door_Open";

    [Header("Phone Ring Effects (No Animation)")]
    [Tooltip("전화벨 효과음. Play On Awake는 꺼두는 게 좋다.")]
    [SerializeField] private AudioSource phoneRingAudio;
    [Tooltip("전화벨 시각 효과(선택). Phone 본체가 아니라 별도 오브젝트를 연결해야 한다.")]
    [SerializeField] private GameObject phoneRingVfx;

    [Header("Exit (Optional)")]
    [Tooltip("문이 열린 상태에서 'Haru_Door_Open' 대화가 끝나면 다음 씬으로 이동하고 싶으면 체크")]
    [SerializeField] private bool loadNextSceneOnDoorOpenDialogueEnd = false;
    [SerializeField] private string nextSceneName;

    // ===== runtime =====
    private readonly HashSet<string> inspectedNonPhoneIds = new HashSet<string>();
    private bool isPhoneRinging;
    private bool hasFinishedCall;

    protected override void Awake()
    {
        base.Awake();
        state = S2S4State.Searching;

        // ✅ 전화기 본체는 항상 존재/활성
        if (phoneObject != null)
        {
            phoneObject.gameObject.SetActive(true);
            var sr = phoneObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            phoneObject.StartDialogue = phoneSilentId; // Phone_Silent에서도 대사 존재
            phoneObject.hasBeenInspected = false;
        }

        if (frontDoorObject != null)
        {
            frontDoorObject.StartDialogue = doorLockedId; // 통화 전에는 나가지 못함
            frontDoorObject.hasBeenInspected = false;
        }

        StopPhoneRingingEffects();
    }

    private void Start()
    {
        // 씬 시작 독백 자동 재생
        if (!string.IsNullOrEmpty(wakeUpMonologueId))
        {
            dialogueManager.StartDialogue(wakeUpMonologueId);
        }
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        // 1) 전화기 제외 조사 카운트 누적(중복 방지)
        if (!hasFinishedCall && nonPhoneOneLinerIds.Contains(dialogueId))
        {
            inspectedNonPhoneIds.Add(dialogueId);

            // N개(기본 5개) 달성 시에만 울림 시작
            if (!isPhoneRinging && inspectedNonPhoneIds.Count >= requiredInspectCountToRing)
            {
                StartPhoneRinging();
            }
        }

        // 2) 울리는 상태에서 전화기 "울림 대사"가 끝나면 => 본 통화 시작
        if (isPhoneRinging && dialogueId == phoneRingingId)
        {
            StopPhoneRingingEffects();
            dialogueManager.StartDialogue(phoneCallId);
        }

        // 3) 통화 종료 => 통화 후 독백 + 문 해금(대사 변경) + 문 블록 삭제
        if (!hasFinishedCall && dialogueId == phoneCallId)
        {
            hasFinishedCall = true;
            state = S2S4State.CallFinished;

            if (!string.IsNullOrEmpty(afterCallMonologueId))
            {
                dialogueManager.StartDialogue(afterCallMonologueId);
            }

            UnlockAndClearDoor();
        }

        // 4) (선택) 열린 문 대사가 끝나면 씬 이동
        if (loadNextSceneOnDoorOpenDialogueEnd && dialogueId == doorOpenId)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        TryProgress();
    }

    protected override void DialogueRunning(string dialogueId) { }
    protected override void HandleOption(string text, string nextId) { }

    protected override void TryProgress()
    {
        if (state == S2S4State.DoorCleared)
        {
            OnPuzzleComplete();
        }
    }

    protected override void OnPuzzleComplete() { }

    private void StartPhoneRinging()
    {
        isPhoneRinging = true;
        state = S2S4State.PhoneRinging;

        if (phoneObject != null)
        {
            phoneObject.StartDialogue = phoneRingingId;
            phoneObject.hasBeenInspected = false; // 이미 봤어도 다시 상호작용 가능
        }

        if (phoneRingAudio != null)
        {
            phoneRingAudio.loop = true;
            phoneRingAudio.Play();
        }

        // ⚠️ phoneRingVfx에 Phone 본체 넣으면 시작하자마자 꺼져서 "전화기 없음"처럼 보일 수 있다.
        if (phoneRingVfx != null)
        {
            phoneRingVfx.SetActive(true);
        }
    }

    private void StopPhoneRingingEffects()
    {
        isPhoneRinging = false;

        if (phoneRingAudio != null) phoneRingAudio.Stop();
        if (phoneRingVfx != null) phoneRingVfx.SetActive(false);
    }

    private void UnlockAndClearDoor()
    {
        // 문 대사를 "열림"으로 교체 (문 오브젝트가 남아있다면 열림 대사 출력 가능)
        if (frontDoorObject != null)
        {
            frontDoorObject.StartDialogue = doorOpenId;
            frontDoorObject.hasBeenInspected = false;
        }

        // 실제로 막는 물체 삭제
        if (frontDoorBlockObject != null)
        {
            Destroy(frontDoorBlockObject);
        }
        else if (frontDoorObject != null)
        {
            // 문 오브젝트 자체가 길을 막고 있다면 삭제
            Destroy(frontDoorObject.gameObject);
        }

        state = S2S4State.DoorCleared;
    }
}
