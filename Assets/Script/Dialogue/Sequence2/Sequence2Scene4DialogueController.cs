using System;
using System.Collections;
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

    // ✅ 씬 시작 연출(누워있기 → 기상)
    [Header("Scene Start Cut")]
    [SerializeField] private GameObject lieObject;          // 누워있는 하루(더미/스프라이트)
    [SerializeField] private GameObject playerObject;       // 실제 플레이어 루트 오브젝트
    [SerializeField] private Transform playerSpawnPoint;    // (선택) 침대 앞 위치
    [SerializeField] private float introDelay = 2f;         // 2초

    // ✅ 추가: 씬 시작 즉시 페이드 인
    [Header("Fade In")]
    [SerializeField] private CanvasGroup fadeCanvas;        // 전체화면 검정 Image의 CanvasGroup
    [SerializeField] private float fadeInDuration = 1f;     // 페이드 시간

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

        // ✅ 시작 상태: 플레이어 숨기고 lieObject만 보이게
        if (playerObject != null) playerObject.SetActive(false);
        if (lieObject != null) lieObject.SetActive(true);

        // ✅ 시작 즉시 검정 덮개(혹시 인스펙터가 0으로 되어 있어도 안전)
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 1f;
        }
    }

    private void Start()
    {
        // ✅ 페이드인 -> 인트로(2초) -> 독백 시작
        StartCoroutine(CoFadeInThenIntro());
    }

    private IEnumerator CoFadeInThenIntro()
    {
        // 0) 씬 시작 즉시 페이드 인 (검정 -> 투명)
        if (fadeCanvas != null)
        {
            yield return StartCoroutine(CoFade(1f, 0f, fadeInDuration));
            fadeCanvas.alpha = 0f;
            fadeCanvas.gameObject.SetActive(false); // 완전히 꺼도 됨(원하면 유지해도 됨)
        }

        // 1) 2초 동안 누워있는 하루만 보여주기
        yield return new WaitForSeconds(introDelay);

        // 2) 누운 오브젝트 삭제 + 플레이어 등장
        if (lieObject != null) Destroy(lieObject);

        if (playerObject != null)
        {
            playerObject.SetActive(true);

            // (선택) 침대 앞으로 위치 잡기
            if (playerSpawnPoint != null)
                playerObject.transform.position = playerSpawnPoint.position;
        }

        // 3) 독백 시작
        if (!string.IsNullOrEmpty(wakeUpMonologueId))
        {
            dialogueManager.StartDialogue(wakeUpMonologueId);
        }
    }

    private IEnumerator CoFade(float from, float to, float duration)
    {
        if (fadeCanvas == null) yield break;

        float t = 0f;
        fadeCanvas.alpha = from;

        // duration이 0이면 즉시 전환
        if (duration <= 0f)
        {
            fadeCanvas.alpha = to;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeCanvas.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        fadeCanvas.alpha = to;
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
            // phoneRingVfx엔 Phone 본체 넣지 말고, 별도 오브젝트만 넣어야 한다.
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

    protected override void ApplyWorldByState()
    {
    }
}
