using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private DialogueObject phoneObject;
    [SerializeField] private DialogueObject frontDoorObject;

    [Header("Door Block Object (Optional)")]
    [SerializeField] private GameObject frontDoorBlockObject;

    [Header("Scene Start Dialogue")]
    [SerializeField] private string wakeUpMonologueId = "WakeUp_Monologue";

    [Header("Non-phone one-liner IDs (UNIQUE)")]
    [SerializeField]
    private List<string> nonPhoneOneLinerIds = new List<string>
    {
        "Bed", "Window", "Drawer", "Wardrobe", "FishTank"
    };

    [SerializeField] private int requiredInspectCountToRing = 5;

    [Header("Phone Dialogue IDs (JSON)")]
    [SerializeField] private string phoneSilentId = "Phone_Silent";
    [SerializeField] private string phoneRingingId = "Phone_Ringing";

    [Header("After Call (JSON)")]
    [SerializeField] private string afterCallMonologueId = "After_Call_Monologue";

    [Header("Door Dialogue IDs (JSON)")]
    [SerializeField] private string doorLockedId = "Haru_Door_Locked";
    [SerializeField] private string doorOpenId = "Haru_Door_Open";

    [Header("Phone Ring Effects (No Animation)")]
    [SerializeField] private AudioSource phoneRingAudio;
    [SerializeField] private GameObject phoneRingVfx;

    [Header("Exit (Optional)")]
    [SerializeField] private bool loadNextSceneOnDoorOpenDialogueEnd = false;
    [SerializeField] private string nextSceneName;

    // runtime
    private readonly HashSet<string> inspectedNonPhoneIds = new HashSet<string>();
    private bool isPhoneRinging;
    private bool hasFinishedCall;

    protected override void Awake()
    {
        base.Awake();
        state = S2S4State.Searching;

        if (phoneObject != null)
        {
            phoneObject.gameObject.SetActive(true);
            var sr = phoneObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            phoneObject.StartDialogue = phoneSilentId;
            phoneObject.hasBeenInspected = false;
        }

        if (frontDoorObject != null)
        {
            frontDoorObject.StartDialogue = doorLockedId;
            frontDoorObject.hasBeenInspected = false;
        }

        StopPhoneRingingEffects();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(wakeUpMonologueId))
        {
            dialogueManager.StartDialogue(wakeUpMonologueId);
        }
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        // 1) 전화기 제외 조사 누적(중복 방지)
        if (!hasFinishedCall && nonPhoneOneLinerIds.Contains(dialogueId))
        {
            inspectedNonPhoneIds.Add(dialogueId);

            if (!isPhoneRinging && inspectedNonPhoneIds.Count >= requiredInspectCountToRing)
            {
                StartPhoneRinging();
            }
        }

        // 2) Phone_Ringing이 끝났을 때: 벨만 끈다.
        //    (통화 시작은 JSON의 nextId 체인으로 자동 진행된다.)
        if (isPhoneRinging && dialogueId == phoneRingingId)
        {
            StopPhoneRingingEffects();
        }

        // 3) After_Call_Monologue가 끝났을 때:
        //    - 전화기는 다시 Phone_Silent로 복귀
        //    - 문을 열림 상태로 바꾸고 블록 삭제
        if (!hasFinishedCall && dialogueId == afterCallMonologueId)
        {
            hasFinishedCall = true;
            state = S2S4State.CallFinished;

            ResetPhoneToSilent();
            UnlockAndClearDoor();

            TryProgress();
            return;
        }

        // 4) (선택) 열린 문 대사 끝나면 씬 이동
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
            phoneObject.hasBeenInspected = false;
        }

        if (phoneRingAudio != null)
        {
            phoneRingAudio.loop = true;
            phoneRingAudio.Play();
        }

        if (phoneRingVfx != null)
        {
            // phoneRingVfx엔 Phone 본체 넣지 말고, 별도 오브젝트만 넣어야 했다.
            phoneRingVfx.SetActive(true);
        }
    }

    private void StopPhoneRingingEffects()
    {
        isPhoneRinging = false;

        if (phoneRingAudio != null) phoneRingAudio.Stop();
        if (phoneRingVfx != null) phoneRingVfx.SetActive(false);
    }

    private void ResetPhoneToSilent()
    {
        StopPhoneRingingEffects();

        if (phoneObject != null)
        {
            phoneObject.StartDialogue = phoneSilentId;
            phoneObject.hasBeenInspected = false;
        }
    }

    private void UnlockAndClearDoor()
    {
        if (frontDoorObject != null)
        {
            frontDoorObject.StartDialogue = doorOpenId;
            frontDoorObject.hasBeenInspected = false;
        }

        if (frontDoorBlockObject != null)
        {
            Destroy(frontDoorBlockObject);
        }
        else if (frontDoorObject != null)
        {
            Destroy(frontDoorObject.gameObject);
        }

        state = S2S4State.DoorCleared;
    }
}
