using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public enum S7S3State
{
    None,
    ReiExited,
    Finished
}


public class Sequence7Scene3DialogueController
    : DialogueController<S7S3State>
{
    // =====================================================
    // Room 201
    // =====================================================

    [Header("Room 201")]
    [SerializeField]
    private Room201PhoneEvent room201PhoneEvent;

    [SerializeField]
    private float shotToReactionDelay = 0.15f;


    // =====================================================
    // Manager Desk
    // =====================================================

    [Header("Manager Desk")]
    [SerializeField]
    private DialogueObject managerDeskDrawer;

    [SerializeField]
    private string deskAfterSolvedDialogueId =
        "desk_afterSolved";


    // =====================================================
    // Call Sheet Puzzle
    // =====================================================

    [Header("Call Sheet Puzzle")]
    [SerializeField]
    private CallSheetPuzzleController
        callSheetPuzzleController;


    // =====================================================
    // Emergency Door
    // =====================================================

    [Header("Emergency Door")]
    [SerializeField]
    private ThirdFloorEmergencyDoorEvent
        emergencyDoorEvent;

    [SerializeField]
    private ItemData thirdFloorEmergencyKey;

    [SerializeField]
    private AudioSource emergencyDoorAudioSource;

    [SerializeField]
    private AudioClip doorOpenSFX;


    // =====================================================
    // Next Scene / Cutscene
    // =====================================================

    [Header("Next Scene / Cutscene")]
    [SerializeField]
    private UnityEvent onEmergencyDoorOpened;


    private bool emergencyDoorOpened = false;


    // =====================================================
    // State
    // =====================================================

    protected override void ApplyWorldByState()
    {
    }


    protected override void DialogueRunning(
        string dialogueId
    )
    {
    }


    // =====================================================
    // Dialogue End
    // =====================================================

    protected override void HandleDialogueEnd(
        string dialogueId
    )
    {
        // =================================================
        // Room 201
        // =================================================

        if (dialogueId ==
            "room201_phone_before_shot")
        {
            StartCoroutine(
                PlayRoom201ShotAndContinue()
            );

            return;
        }


        // =================================================
        // 콜시트 퍼즐 성공
        // =================================================

        if (dialogueId ==
            "callsheet_puzzle_correct")
        {
            ChangeManagerDeskToSolved();

            return;
        }


        // =================================================
        // 문이 열렸다 대화 종료
        //
        // → 다음 씬 / 컷씬
        // =================================================

        if (dialogueId ==
            "emergencyDoor_useKey")
        {
            StartCoroutine(
                GoToNextSceneAfterDoorDialogue()
            );

            return;
        }
    }


    // =====================================================
    // Room 201
    // =====================================================

    private IEnumerator PlayRoom201ShotAndContinue()
    {
        if (room201PhoneEvent != null)
        {
            room201PhoneEvent
                .PlayGunshotAndApplause();
        }


        yield return new WaitForSecondsRealtime(
            shotToReactionDelay
        );


        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(
                "room201_phone_after_shot"
            );
        }
    }


    // =====================================================
    // Manager Desk
    // =====================================================

    private void ChangeManagerDeskToSolved()
    {
        if (managerDeskDrawer == null)
            return;


        managerDeskDrawer.StartDialogue =
            deskAfterSolvedDialogueId;


        Debug.Log(
            "[Manager Desk] StartDialogue → "
            + deskAfterSolvedDialogueId
        );
    }


    // =====================================================
    // Option
    // =====================================================

    protected override void HandleOption(
        string text,
        string nextId
    )
    {
        // =================================================
        // 서랍
        // =================================================

        if (text == "들여다본다")
        {
            StartCoroutine(
                OpenCallSheetPuzzleAfterDialogue()
            );

            return;
        }


        // =================================================
        // 비상문
        //
        // 선택 순간:
        // 열쇠 사용 + 문 열림 SFX
        //
        // 이후 DialogueManager가 자동으로
        // emergencyDoor_useKey로 넘어감
        // =================================================

        if (text == "열쇠를 사용한다")
        {
            UseEmergencyDoorKey();

            return;
        }
    }


    // =====================================================
    // Call Sheet Puzzle
    // =====================================================

    private IEnumerator
        OpenCallSheetPuzzleAfterDialogue()
    {
        if (dialogueManager == null)
            yield break;


        yield return new WaitUntil(
            () => !dialogueManager.isRunning
        );


        if (callSheetPuzzleController == null)
            yield break;


        if (callSheetPuzzleController.PuzzleSolved)
            yield break;


        callSheetPuzzleController.OpenPuzzle();
    }


    // =====================================================
    // Emergency Door
    // =====================================================

    private void UseEmergencyDoorKey()
    {
        if (emergencyDoorOpened)
            return;


        if (InventoryManager.Instance == null)
            return;


        if (thirdFloorEmergencyKey == null)
            return;


        if (!InventoryManager.Instance.HasItem(
                thirdFloorEmergencyKey
            ))
        {
            Debug.LogWarning(
                "[Emergency Door] 열쇠가 없습니다."
            );

            return;
        }


        emergencyDoorOpened = true;


        // 문 오브젝트 상태 변경
        if (emergencyDoorEvent != null)
        {
            emergencyDoorEvent.MarkOpened();
        }


        // =============================================
        // 열쇠 제거
        // =============================================

        InventoryManager.Instance.RemoveItem(
            thirdFloorEmergencyKey.ItemName
        );


        // =============================================
        // 문 열림 SFX
        //
        // "열쇠를 사용한다" 선택 직후 재생
        // =============================================

        if (emergencyDoorAudioSource != null &&
            doorOpenSFX != null)
        {
            emergencyDoorAudioSource.PlayOneShot(
                doorOpenSFX
            );
        }


        Debug.Log(
            "[Emergency Door] 열쇠 사용 → 문 열림 SFX"
        );


        // 여기서는 StartDialogue 하지 않음.
        //
        // JSON의
        // nextId = emergencyDoor_useKey
        //
        // 를 기존 DialogueManager가 자동 실행한다.
    }


    // =====================================================
    // 문 열림 대화 종료 → 다음 씬
    // =====================================================

    private IEnumerator
        GoToNextSceneAfterDoorDialogue()
    {
        if (dialogueManager != null)
        {
            yield return new WaitUntil(
                () => !dialogueManager.isRunning
            );
        }


        Debug.Log(
            "[Emergency Door] '문이 열렸다.' 종료 → 다음 씬"
        );


        // Inspector에서 이후 컷씬/씬 전환 함수 연결
        onEmergencyDoorOpened?.Invoke();
    }


    // =====================================================
    // Required Override
    // =====================================================

    protected override void OnPuzzleComplete()
    {
    }


    protected override void TryProgress()
    {
    }


    public void StartDialogue()
    {
    }
}