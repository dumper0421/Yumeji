using System.Collections;
using UnityEngine;

public class Room201PhoneEvent : DialogueObject
{
    [Header("Phone")]
    [SerializeField] private Collider2D phoneCollider;


    [Header("Phone Ring")]
    [SerializeField] private AudioSource ringAudioSource;
    [SerializeField] private float ringDelay = 2f;


    [Header("Gunshot")]
    [SerializeField] private AudioSource gunshotAudioSource;
    [SerializeField] private AudioClip gunshotClip;


    [Header("Applause")]
    [SerializeField] private AudioSource applauseAudioSource;
    [SerializeField] private AudioClip applauseClip;


    [Header("TV")]
    [SerializeField] private Room201TVEvent tvEvent;


    private bool isRinging = false;
    private bool phoneCompleted = false;
    private bool waitingForRing = false;

    // 총성 + 박수 연출 중복 실행 방지
    private bool sfxSequenceStarted = false;


    private void Awake()
    {
        // 전화벨이 울리기 전에는 조사 불가능
        if (phoneCollider != null)
        {
            phoneCollider.enabled = false;
        }


        // 박수는 반복하지 않음
        if (applauseAudioSource != null)
        {
            applauseAudioSource.loop = false;
        }
    }


    // =========================================================
    // 방 입장
    // =========================================================

    public void OnRoomEntered()
    {
        // 이미 전화 이벤트를 완료했다면
        // 재입장해도 다시 울리지 않음
        if (phoneCompleted)
        {
            Debug.Log(
                "[Room201 Phone] 이미 완료된 전화 이벤트 - 벨 재생 안 함"
            );

            return;
        }


        // 이미 벨이 울리는 중
        if (isRinging)
            return;


        // 이미 2초 대기 중
        if (waitingForRing)
            return;


        StartCoroutine(StartRingAfterDelay());
    }


    private IEnumerator StartRingAfterDelay()
    {
        waitingForRing = true;


        Debug.Log(
            "[Room201 Phone] 방 입장 - 2초 후 전화벨 시작"
        );


        yield return new WaitForSeconds(ringDelay);


        waitingForRing = false;


        if (phoneCompleted)
            yield break;


        StartPhoneRing();
    }


    private void StartPhoneRing()
    {
        if (isRinging)
            return;


        isRinging = true;


        // 전화벨 재생
        if (ringAudioSource != null)
        {
            ringAudioSource.loop = true;
            ringAudioSource.Play();
        }
        else
        {
            Debug.LogWarning(
                "[Room201 Phone] Ring AudioSource가 연결되지 않았습니다."
            );
        }


        // 전화벨이 울리는 동안만 조사 가능
        if (phoneCollider != null)
        {
            phoneCollider.enabled = true;
        }


        Debug.Log(
            "[Room201 Phone] ★ 전화벨 시작 / 전화 조사 가능 ★"
        );
    }


    // =========================================================
    // 전화 조사
    // =========================================================

    public override void TryInspect()
    {
        // 전화벨이 울릴 때만 조사 가능
        if (!isRinging)
            return;


        if (phoneCompleted)
            return;


        if (DialogueManager.isRunning)
            return;


        base.TryInspect();
    }


    protected override void OnInspect()
    {
        if (!isRinging)
            return;


        phoneCompleted = true;
        isRinging = false;


        // 전화벨 종료
        if (ringAudioSource != null)
        {
            ringAudioSource.Stop();
        }


        // 다시 조사하지 못하게 함
        if (phoneCollider != null)
        {
            phoneCollider.enabled = false;
        }


        Debug.Log(
            "[Room201 Phone] 전화기 조사 / 전화벨 종료"
        );


        // room201_phone_before_shot
        DialogueManager.StartDialogue(StartDialogue);
    }


    // =========================================================
    // 총성 + 박수
    // =========================================================

    // Sequence7Scene3DialogueController에서
    // room201_phone_before_shot 종료 시 호출
    public void PlayGunshotAndApplause()
    {
        if (sfxSequenceStarted)
            return;


        sfxSequenceStarted = true;


        // -------------------------
        // 총성
        // -------------------------

        if (gunshotAudioSource != null &&
            gunshotClip != null)
        {
            gunshotAudioSource.PlayOneShot(
                gunshotClip
            );


            Debug.Log(
                "[Room201 Phone] 총성 SFX 재생"
            );
        }
        else
        {
            Debug.LogWarning(
                "[Room201 Phone] 총성 AudioSource 또는 Clip이 없습니다."
            );
        }


        // -------------------------
        // 박수
        // -------------------------

        if (applauseAudioSource != null &&
            applauseClip != null)
        {
            applauseAudioSource.clip =
                applauseClip;

            // ★ 반복하지 않음
            applauseAudioSource.loop = false;

            applauseAudioSource.Play();


            Debug.Log(
                "[Room201 Phone] 박수 SFX 재생 시작"
            );


            // 박수 종료 감시
            StartCoroutine(
                WaitForApplauseEnd()
            );
        }
        else
        {
            Debug.LogWarning(
                "[Room201 Phone] 박수 AudioSource 또는 Clip이 없습니다."
            );
        }
    }


    // =========================================================
    // 박수 종료 → TV 자동 OFF
    // =========================================================

    private IEnumerator WaitForApplauseEnd()
    {
        // AudioSource가 실제로 재생을 끝낼 때까지 기다림
        // 현재 박수 SFX가 약 8초이므로 약 8초 뒤 실행됨
        yield return new WaitWhile(
            () => applauseAudioSource.isPlaying
        );


        Debug.Log(
            "[Room201 Phone] ★ 박수 SFX 종료 → TV 자동 OFF ★"
        );


        // 대화 진행 중이더라도 즉시 TV OFF
        if (tvEvent != null)
        {
            tvEvent.TurnOffTV();
        }
        else
        {
            Debug.LogWarning(
                "[Room201 Phone] TV Event가 연결되지 않았습니다."
            );
        }
    }
}