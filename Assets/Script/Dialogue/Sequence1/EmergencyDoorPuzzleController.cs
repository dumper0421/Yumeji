using System.Collections;
using UnityEngine;

/// <summary>
/// Emergency Door 퍼즐 상태
/// </summary>
public enum EmergencyDoorState
{
    None,
    HintReceived,
    ScooterHonked,
    CarDTriggered,
    CarBKnocked,
    CarEKnocked,
    BusInteracted,
    DoorOpened
}

/// <summary>
/// Emergency Door 퍼즐 컨트롤러
/// </summary>
public class EmergencyDoorPuzzleController : DialogueController<EmergencyDoorState>
{
    [Header("Bus & Scooter References")]
    public Animator BusAnimator;
    public Animator BloodAnimator;               
    public ScooterObject Scooter;           

    [Header("Audio Clips")]
    public AudioClip KnockClip;                
    public AudioClip BloodClip;                
    public AudioClip HornClip;    
    public AudioClip CarHornClip;
    public AudioClip CrashClip;

    public DialogueObject Bus;
    public GameObject SceneChangeTrigger;

    private int _yellowCarInteractionCount = 0;

    protected override void HandleOption(string text, string nextId)
    {

        switch (nextId)
        {
            case "CarD_LookInside":
                state = EmergencyDoorState.CarDTriggered;
                SoundManager.Instance.PlaySFX(CarHornClip);
                StartCoroutine(CameraManager.Instance.Shake(1f, 1f, 0.3f));
                break;

            case "CarB_Knock":
                state = EmergencyDoorState.CarBKnocked;
                SoundManager.Instance.PlaySFX(KnockClip);
                _yellowCarInteractionCount++;
                StartCoroutine(DelayedKnock());
                break;

            case "CarE_Knock":
                state = EmergencyDoorState.CarEKnocked;
                SoundManager.Instance.PlaySFX(KnockClip);
                StartCoroutine(PlayBloodSequence());
                _yellowCarInteractionCount++;
                break;
        }
    }

    private IEnumerator DelayedKnock()
    {
        yield return new WaitForSeconds(1f);
        SoundManager.Instance.PlaySFX(KnockClip);
        _yellowCarInteractionCount++;
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "EmergencyDoor":
                // 힌트 대화 완료
                state = EmergencyDoorState.HintReceived;
                break;

            case "Scooter":
                // 스쿠터 대화 완료 → 경적 + 깜빡임
                state = EmergencyDoorState.ScooterHonked;
                SoundManager.Instance.PlaySFX(HornClip);
                // TODO: scooterObject 깜빡이는 연출
                Scooter.StartCoroutine(Scooter.Flash());
                break;

            case "CarE_AfterKnock":
                 Bus.StartDialogue = "Bus_After";
                break;

            case "Bus_After":
                state = EmergencyDoorState.BusInteracted;
                StartCoroutine(PlayBusSequence());
                break;

        }

        TryProgress();
    }

    protected override void TryProgress()
    {
        if (state == EmergencyDoorState.BusInteracted)
        {
            state = EmergencyDoorState.DoorOpened;
            OnPuzzleComplete();
        }
    }

    protected override void OnPuzzleComplete()
    {
        Debug.Log("[Puzzle] Emergency Door opened!");
        SceneChangeTrigger.SetActive(true);
        // TODO: 비상문 열리는 연출 추가
    }

    private IEnumerator PlayBloodSequence()
    {
        BloodAnimator.enabled = true;
        yield return new WaitForSeconds(0.2f);
        // 핏소리
        SoundManager.Instance.PlaySFX(BloodClip);
        // 0.5초 뒤 실제 다음 대화로 전환
        yield return new WaitForSeconds(0.5f);
        // 3) 다음 대화
        dialogueManager.StartDialogue("CarE_AfterKnock");
        state = EmergencyDoorState.CarEKnocked;
    }

    private IEnumerator PlayBusSequence()
    {
        BusAnimator.enabled = true;
        SoundManager.Instance.PlaySFX(CrashClip);
        yield return new WaitForSeconds(0.5f);
        dialogueManager.StartDialogue("Bus_DoorOpen");
        state = EmergencyDoorState.BusInteracted;
    }
}
