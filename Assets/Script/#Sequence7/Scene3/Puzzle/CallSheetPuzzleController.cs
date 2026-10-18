using UnityEngine;

public class CallSheetPuzzleController : MonoBehaviour
{
    [Header("Puzzle UI")]
    [SerializeField] private GameObject puzzleCanvas;
    [SerializeField] private GameObject choicePanel;


    [Header("Slot Arrow")]
    [SerializeField] private RectTransform slotArrow;
    [SerializeField] private RectTransform[] slotArrowPoints;


    [Header("Player")]
    [SerializeField] private PlayerMove_Test_Lerp playerMove;
    [SerializeField] private InteractionSystem interactionSystem;


    [Header("Pause")]
    [SerializeField] private PauseUI pauseUI;


    private int selectedSlot = 0;
    private bool isPuzzleOpen = false;


    // 퍼즐을 열기 전 각 컴포넌트 상태 저장
    private bool wasPlayerMoveEnabled;
    private bool wasInteractionEnabled;
    private bool wasPauseUIEnabled;


    public bool IsPuzzleOpen => isPuzzleOpen;


    private void Update()
    {
        if (!isPuzzleOpen)
            return;


        // 선택창이 열려 있으면
        // 일단 메인 슬롯 입력은 받지 않음
        if (choicePanel != null &&
            choicePanel.activeSelf)
        {
            return;
        }


        // ==============================================
        // W / S : 슬롯 이동
        // ==============================================

        if (Input.GetKeyDown(KeyCode.W))
        {
            MoveSlot(-1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            MoveSlot(1);
        }


        // ==============================================
        // Space : 현재 슬롯 선택
        // 지금은 테스트 로그만 출력
        // ==============================================

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log(
                "[CallSheet Puzzle] Space 입력 / 현재 슬롯: "
                + (selectedSlot + 1)
            );
        }


        // ==============================================
        // Z : 퍼즐 종료
        // ==============================================

        if (Input.GetKeyDown(KeyCode.Z))
        {
            ClosePuzzle();
        }
    }


    // =====================================================
    // 퍼즐 시작
    // =====================================================

    public void OpenPuzzle()
    {
        if (isPuzzleOpen)
            return;


        isPuzzleOpen = true;
        selectedSlot = 0;


        // 퍼즐 UI 활성화
        if (puzzleCanvas != null)
        {
            puzzleCanvas.SetActive(true);
        }


        // 선택창은 처음에는 숨김
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }


        // 첫 번째 슬롯으로 화살표 이동
        UpdateSlotArrow();


        // ==============================================
        // 기존 상태 저장 후 플레이어 조작 잠금
        // ==============================================

        if (playerMove != null)
        {
            wasPlayerMoveEnabled = playerMove.enabled;

            if (playerMove.animator != null)
            {
                playerMove.animator.SetBool(
                    "Walking",
                    false
                );
            }

            playerMove.enabled = false;
        }


        if (interactionSystem != null)
        {
            wasInteractionEnabled =
                interactionSystem.enabled;

            interactionSystem.enabled = false;
        }


        // 퍼즐 중 ESC PauseUI 차단
        // 기존 PauseUI 코드는 수정하지 않음
        if (pauseUI != null)
        {
            wasPauseUIEnabled = pauseUI.enabled;

            pauseUI.enabled = false;
        }


        Debug.Log(
            "[CallSheet Puzzle] ★ 퍼즐 시작 ★"
        );
    }


    // =====================================================
    // W / S 슬롯 이동
    // =====================================================

    private void MoveSlot(int direction)
    {
        if (slotArrowPoints == null ||
            slotArrowPoints.Length == 0)
        {
            return;
        }


        selectedSlot += direction;


        // 위에서 W를 누르면 맨 아래로
        if (selectedSlot < 0)
        {
            selectedSlot =
                slotArrowPoints.Length - 1;
        }

        // 아래에서 S를 누르면 맨 위로
        else if (selectedSlot >= slotArrowPoints.Length)
        {
            selectedSlot = 0;
        }


        UpdateSlotArrow();


        Debug.Log(
            "[CallSheet Puzzle] 선택 슬롯: "
            + (selectedSlot + 1)
        );
    }


    // =====================================================
    // 화살표 위치 적용
    // =====================================================

    private void UpdateSlotArrow()
    {
        if (slotArrow == null)
            return;


        if (slotArrowPoints == null ||
            slotArrowPoints.Length == 0)
        {
            return;
        }


        if (selectedSlot < 0 ||
            selectedSlot >= slotArrowPoints.Length)
        {
            return;
        }


        RectTransform point =
            slotArrowPoints[selectedSlot];


        if (point == null)
            return;


        // SlotArrow와 Point가 같은 Canvas 계층에 있으므로
        // 월드 위치를 맞추는 방식
        slotArrow.position = point.position;
    }


    // =====================================================
    // 퍼즐 종료
    // =====================================================

    public void ClosePuzzle()
    {
        if (!isPuzzleOpen)
            return;


        isPuzzleOpen = false;


        // 퍼즐 UI 종료
        if (puzzleCanvas != null)
        {
            puzzleCanvas.SetActive(false);
        }


        // 기존 플레이어 상태 복구
        if (playerMove != null)
        {
            playerMove.enabled =
                wasPlayerMoveEnabled;
        }


        if (interactionSystem != null)
        {
            interactionSystem.enabled =
                wasInteractionEnabled;
        }


        // PauseUI 다시 활성화
        if (pauseUI != null)
        {
            pauseUI.enabled =
                wasPauseUIEnabled;
        }


        Debug.Log(
            "[CallSheet Puzzle] ★ 퍼즐 종료 ★"
        );
    }
    [ContextMenu("TEST - Open Puzzle")]
    private void TestOpenPuzzle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CallSheet Puzzle] Play 모드에서 테스트하세요.");
            return;
        }

        OpenPuzzle();
    }


    [ContextMenu("TEST - Close Puzzle")]
    private void TestClosePuzzle()
    {
        if (!Application.isPlaying)
            return;

        ClosePuzzle();
    }
}