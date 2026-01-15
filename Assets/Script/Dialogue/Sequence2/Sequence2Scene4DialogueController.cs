using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum S2S4State
{
    None,
    PhoneRinging,
    CallFinished,
    DoorOpen
}

public class Sequence2Scene4DialogueController : DialogueController<S2S4State>
{
    [Header("Dialogue Objects")]
    [SerializeField] private DialogueObject phoneObject;
    [SerializeField] private DialogueObject frontDoorObject;

    [Header("Dialogue IDs")]
    [SerializeField] private string wakeUpMonologueId = "WakeUp_Monologue";

    [Tooltip("전화기 제외 오브젝트들의 1줄 대사 ID들(침대/창문/서랍/옷장/어항/초인종 등)")]
    [SerializeField]
    private List<string> nonPhoneOneLinerIds = new List<string>
    {
        "Bed", "Window", "Drawer", "Closet", "FishTank", "Doorbell"
    };

    [Header("Phone IDs")]
    [SerializeField] private string phoneSilentId = "Phone_Silent";
    [SerializeField] private string phoneRingingId = "Phone_Ringing";
    [SerializeField] private string phoneCallId = "Phone_Call";

    [Header("After Call")]
    [SerializeField] private string afterCallMonologueId = "After_Call_Monologue";

    [Header("Door IDs")]
    [SerializeField] private string frontDoorLockedId = "FrontDoor_Locked";
    [SerializeField] private string frontDoorOpenId = "FrontDoor_Open";

    [Header("Phone Ring Effects (Optional)")]
    [SerializeField] private AudioSource phoneRingAudio;
    [SerializeField] private GameObject phoneRingVfx;
    [SerializeField] private Animator phoneAnimator;

    [Header("Door Effects (Optional)")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private Collider2D doorBlockCollider;

    [Header("Exit")]
    [Tooltip("문이 열린 상태에서 문 대화가 끝나면 다음 씬으로 이동하고 싶으면 체크")]
    [SerializeField] private bool loadNextSceneOnDoorOpenDialogueEnd = false;
    [SerializeField] private string nextSceneName;

    private bool hasTriggeredPhone;   // 전화기 제외 오브젝트 1회라도 조사했는지
    private bool isPhoneRinging;      // 전화벨 울리는 중인지
    private bool hasFinishedCall;     // 루나 통화 끝났는지

    protected override void Awake()
    {
        base.Awake();
        state = S2S4State.None;

        // 초기 상태 세팅
        StopPhoneRinging();

        if (phoneObject != null)
        {
            phoneObject.StartDialogue = phoneSilentId;
            phoneObject.hasBeenInspected = false; // 언제든 볼 수 있게(원하면 true로 막아도 됨)
        }

        if (frontDoorObject != null)
        {
            frontDoorObject.StartDialogue = frontDoorLockedId;
            frontDoorObject.hasBeenInspected = false;
        }
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
        // 1) 전화기 제외 오브젝트 중 "어떤 것이든" 1회 상호작용 -> 전화기 울림 시작
        if (!hasTriggeredPhone && nonPhoneOneLinerIds.Contains(dialogueId))
        {
            hasTriggeredPhone = true;
            StartPhoneRinging();
        }

        // 2) 전화기가 울리는 상태에서 전화기(울림) 대화가 끝나면 -> 본 통화 시작
        if (isPhoneRinging && dialogueId == phoneRingingId)
        {
            StopPhoneRinging();
            // 루나 통화 시작
            dialogueManager.StartDialogue(phoneCallId);
        }

        // 3) 루나 통화 끝나면 -> 독백 + 문 열림
        if (!hasFinishedCall && dialogueId == phoneCallId)
        {
            hasFinishedCall = true;
            state = S2S4State.CallFinished;

            if (!string.IsNullOrEmpty(afterCallMonologueId))
            {
                dialogueManager.StartDialogue(afterCallMonologueId);
            }

            OpenDoor();
        }

        // 4) 문 열림 상태에서 문 대사가 끝났을 때 다음 씬으로 이동(선택)
        if (loadNextSceneOnDoorOpenDialogueEnd && dialogueId == frontDoorOpenId)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        TryProgress();
    }

    protected override void DialogueRunning(string dialogueId)
    {
        // 대사 진행 중 특정 타이밍 연출이 필요하면 여기서 처리
    }

    protected override void HandleOption(string text, string nextId)
    {
        // 이 씬은 옵션 분기 없다고 했으니 비워둠
    }

    protected override void TryProgress()
    {
        // 퍼즐 완료 조건을 "통화 완료 + 문 열림"로 잡고 싶으면 이렇게
        if (hasFinishedCall)
        {
            OnPuzzleComplete();
        }
    }

    protected override void OnPuzzleComplete()
    {
        // 필요하면 여기서 저장/트리거/연출 추가
    }

    private void StartPhoneRinging()
    {
        if (isPhoneRinging) return;

        isPhoneRinging = true;
        state = S2S4State.PhoneRinging;

        if (phoneObject != null)
        {
            phoneObject.StartDialogue = phoneRingingId;
            phoneObject.hasBeenInspected = false; // 이전에 봤어도 다시 상호작용 가능하게
        }

        if (phoneRingAudio != null) phoneRingAudio.Play();
        if (phoneRingVfx != null) phoneRingVfx.SetActive(true);
        if (phoneAnimator != null) phoneAnimator.enabled = true;
    }

    private void StopPhoneRinging()
    {
        isPhoneRinging = false;

        if (phoneRingAudio != null) phoneRingAudio.Stop();
        if (phoneRingVfx != null) phoneRingVfx.SetActive(false);
        if (phoneAnimator != null) phoneAnimator.enabled = false;

        if (phoneObject != null && !hasFinishedCall)
        {
            // 울림이 끝났지만 아직 통화 전이면(여기선 울림 끝=통화 시작이라 거의 안탐),
            // 기본 상태로 돌리고 싶으면 사용
            // phoneObject.StartDialogue = phoneSilentId;
        }
    }

    private void OpenDoor()
    {
        state = S2S4State.DoorOpen;

        if (frontDoorObject != null)
        {
            frontDoorObject.StartDialogue = frontDoorOpenId;
            frontDoorObject.hasBeenInspected = false;
        }

        if (doorAnimator != null) doorAnimator.SetTrigger("Open");
        if (doorBlockCollider != null) doorBlockCollider.enabled = false;
    }
}
