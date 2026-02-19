using System.Collections;
using UnityEngine;

/// <summary>
/// Emergency Door 퍼즐의 상태를 표현하는 열거형.
/// 순차적으로 단서 획득 → 스쿠터 경적 → 차량 상호작용 → 버스 상호작용 → 문 개방으로 진행된다.
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
/// Emergency Door 퍼즐 컨트롤러.
/// 대화(Conversation) 이벤트를 트리거로 애니메이션/사운드/상태 전이를 관리하고,
/// 최종적으로 문이 열리면 씬 전환 트리거를 활성화한다.
/// </summary>
public class EmergencyDoorPuzzleController : DialogueController<EmergencyDoorState>
{
    [Header("Bus & Scooter References")]
    [SerializeField] private Animator _bustAnimator;
    [SerializeField] private Animator _bloodAnimator;
    [SerializeField] private ScooterObject _scooter;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _knockClip;
    [SerializeField] private AudioClip _bloodClip;
    [SerializeField] private AudioClip _hornClip;
    [SerializeField] private AudioClip _carHornClip;
    [SerializeField] private AudioClip _crashClip;

    [SerializeField] private DialogueObject _bus;
    [SerializeField] private DialogueObject _yellowCar;
    [SerializeField] private GameObject _sceneChangeTrigger;

    private int _yellowCarInteractionCount = 0;

    /// <summary>
    /// 대화 선택지(옵션) 처리 훅.
    /// 선택된 nextId에 따라 사운드/카메라 쉐이크/상태 전이/후속 코루틴을 실행한다.
    /// </summary>
    /// <param name="text">선택지 텍스트(표시용)</param>
    /// <param name="nextId">선택 시 진행될 다음 대화 ID</param>
    protected override void HandleOption(string text, string nextId)
    {
        switch (nextId)
        {
            case "CarD_LookInside":
                state = EmergencyDoorState.CarDTriggered;
                SoundManager.Instance.PlaySFX(_carHornClip);
                StartCoroutine(CameraManager.Instance.Shake(1f, 1f, 0.3f));
                break;

            case "CarB_Knock":
                state = EmergencyDoorState.CarBKnocked;
                SoundManager.Instance.PlaySFX(_knockClip);
                _yellowCarInteractionCount++;
                StartCoroutine(DelayedKnock());
                break;

            case "CarE_Knock":
                state = EmergencyDoorState.CarEKnocked;
                SoundManager.Instance.PlaySFX(_knockClip);
                StartCoroutine(PlayBloodSequence());
                _yellowCarInteractionCount++;
                break;
        }
    }

    /// <summary>
    /// 두 번째 노크를 약간의 딜레이 후 재생.
    /// 상호작용의 피드백을 단계적으로 주어 몰입감을 높인다.
    /// </summary>
    private IEnumerator DelayedKnock()
    {
        yield return new WaitForSeconds(1f);
        SoundManager.Instance.PlaySFX(_knockClip);
        _yellowCarInteractionCount++;
    }

    /// <summary>
    /// 대화 블록 종료 시점에 상태 전이/후속 처리.
    /// - "EmergencyDoor": 단서 획득 상태
    /// - "Scooter": 스쿠터 경적 및 점멸
    /// - "CarE_AfterKnock": 버스 대화 상태로 전환 준비
    /// - "Bus_After": 버스 시퀀스 재생(충돌 SFX → 문 개방 대화)
    /// - "Wall_Hint": 스쿠터 점멸 & 경적
    /// 종료 처리 후 TryProgress로 퍼즐 진행 조건을 평가한다.
    /// </summary>
    /// <param name="dialogueId">종료된 대화의 ID</param>
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "EmergencyDoor":
                // 힌트 대화 완료
                state = EmergencyDoorState.HintReceived;
                break;

            case "Scooter":
                // 스쿠터 대화 완료 → 경적 + 점멸
                state = EmergencyDoorState.ScooterHonked;
                SoundManager.Instance.PlaySFX(_hornClip);
                _scooter.StartCoroutine(_scooter.Flash());
                break;

            case "CarE_AfterKnock":
                // CarE 상호작용 이후 버스 후속 대화로 연결
                _bus.StartDialogue = "Bus_After";
                break;

            case "Bus_After":
                state = EmergencyDoorState.BusInteracted;
                StartCoroutine(PlayBusSequence());
                break;

            case "Wall_Hint":
                // 벽 힌트 이후 스쿠터 피드백
                _scooter.StartCoroutine(_scooter.Flash());
                SoundManager.Instance.PlaySFX(_hornClip);
                break;
        }

        TryProgress();
    }

    /// <summary>
    /// 퍼즐 진행 조건 평가.
    /// 현재 상태가 BusInteracted면 문 개방 상태로 전이하고 퍼즐 완료 콜백을 호출한다.
    /// </summary>
    protected override void TryProgress()
    {
        if (state == EmergencyDoorState.BusInteracted)
        {
            state = EmergencyDoorState.DoorOpened;
            OnPuzzleComplete();
        }
    }

    /// <summary>
    /// 퍼즐 완료 처리.
    /// 로그 출력 및 씬 전환 트리거 활성화.
    /// </summary>
    protected override void OnPuzzleComplete()
    {
        Debug.Log("[Puzzle] Emergency Door opened!");
        _sceneChangeTrigger.SetActive(true);
        // TODO: 추가 후속 로직(보상, 체크포인트 등)
    }

    /// <summary>
    /// 혈흔 연출 시퀀스.
    /// 1) 혈흔 애니메이션 활성화 → 2) 혈흔 SFX → 3) 짧은 딜레이 후 후속 대화 진입.
    /// 옐로우 카(일회용 상호작용)도 소모 처리한다.
    /// </summary>
    private IEnumerator PlayBloodSequence()
    {
        _bloodAnimator.enabled = true;
        _bloodAnimator.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f);

        SoundManager.Instance.PlaySFX(_bloodClip);

        yield return new WaitForSeconds(0.5f);

        dialogueManager.StartDialogue("CarE_AfterKnock");
        state = EmergencyDoorState.CarEKnocked;

        _yellowCar.IsDisposable = true;
    }

    /// <summary>
    /// 버스 시퀀스.
    /// 1) 버스 애니메이션 활성화 → 2) 충돌 SFX → 3) 짧은 딜레이 후 문 개방 대화 진입.
    /// </summary>
    private IEnumerator PlayBusSequence()
    {
        _bustAnimator.enabled = true;
        SoundManager.Instance.PlaySFX(_crashClip);
        yield return new WaitForSeconds(0.5f);

        dialogueManager.StartDialogue("Bus_DoorOpen");
        state = EmergencyDoorState.BusInteracted;
    }

    protected override void ApplyWorldByState()
    {
        ;
    }
}
