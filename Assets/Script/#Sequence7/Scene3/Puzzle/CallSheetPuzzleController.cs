using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CallSheetPuzzleController : MonoBehaviour
{
    public enum CallSheetPiece
    {
        None,
        Casting,
        LastShooting,
        SingleScreening
    }

    private enum ChoiceMode
    {
        InsertPiece,
        RemovePiece
    }


    // =====================================================
    // UI
    // =====================================================

    [Header("Puzzle UI")]
    [SerializeField] private GameObject puzzleCanvas;
    [SerializeField] private GameObject choicePanel;


    [Header("Slot")]
    [SerializeField] private Image[] slotPieceImages;
    [SerializeField] private RectTransform slotArrow;
    [SerializeField] private RectTransform[] slotArrowPoints;


    [Header("Piece Sprites")]
    [SerializeField] private Sprite castingSprite;
    [SerializeField] private Sprite lastShootingSprite;
    [SerializeField] private Sprite singleScreeningSprite;


    [Header("Choice UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI[] choiceTexts;
    [SerializeField] private RectTransform choiceArrow;
    [SerializeField] private RectTransform[] choiceArrowPoints;


    // =====================================================
    // Inventory
    // =====================================================

    [Header("Inventory Items")]
    [SerializeField] private ItemData castingItem;
    [SerializeField] private ItemData lastShootingItem;
    [SerializeField] private ItemData singleScreeningItem;


    [Header("Reward")]
    [SerializeField] private ItemData thirdFloorEmergencyKey;


    // =====================================================
    // Result
    // =====================================================

    [Header("Result")]
    [SerializeField] private DialogueManager dialogueManager;

    [SerializeField]
    private string correctDialogueId =
        "callsheet_puzzle_correct";

    [SerializeField]
    private string wrongDialogueId =
        "callsheet_puzzle_wrong";


    // =====================================================
    // SFX
    // =====================================================

    [Header("Puzzle SFX")]
    [SerializeField] private AudioSource resultAudioSource;

    [SerializeField] private AudioClip insertSFX;
    [SerializeField] private AudioClip correctSFX;
    [SerializeField] private AudioClip wrongSFX;


    // =====================================================
    // Player
    // =====================================================

    [Header("Player")]
    [SerializeField] private PlayerMove_Test_Lerp playerMoveTestLerp;
    [SerializeField] private PlayerActionController playerAction;


    [Header("Pause")]
    [SerializeField] private PauseUI pauseUI;


    // =====================================================
    // Puzzle State
    // =====================================================

    private readonly CallSheetPiece[] slots =
    {
        CallSheetPiece.None,
        CallSheetPiece.None,
        CallSheetPiece.None
    };


    private readonly List<CallSheetPiece> availablePieces =
        new List<CallSheetPiece>();


    private int selectedSlot = 0;
    private int selectedChoice = 0;


    private ChoiceMode currentChoiceMode =
        ChoiceMode.InsertPiece;


    private bool isPuzzleOpen = false;
    private bool resultSequenceRunning = false;
    private bool puzzleSolved = false;


    // =====================================================
    // Previous Player State
    // =====================================================

    private bool previousCanMove;
    private bool previousPlayerActionLocked;
    private bool previousPauseUIEnabled;


    public bool IsPuzzleOpen => isPuzzleOpen;
    public bool PuzzleSolved => puzzleSolved;


    // =====================================================
    // Update
    // =====================================================

    private void Update()
    {
        if (!isPuzzleOpen)
            return;


        if (resultSequenceRunning)
            return;


        // 선택창 입력
        if (choicePanel != null &&
            choicePanel.activeSelf)
        {
            HandleChoiceInput();
            return;
        }


        // 위
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSlot(-1);
        }

        // 아래
        else if (Input.GetKeyDown(KeyCode.S) ||
                 Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSlot(1);
        }


        // 선택
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SelectCurrentSlot();
        }


        // 퍼즐 종료
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ClosePuzzle();
        }
    }


    // =====================================================
    // Puzzle Open
    // =====================================================

    public void OpenPuzzle()
    {
        if (isPuzzleOpen)
            return;


        if (puzzleSolved)
            return;


        isPuzzleOpen = true;
        resultSequenceRunning = false;

        selectedSlot = 0;


        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(true);


        if (choicePanel != null)
            choicePanel.SetActive(false);


        SavePlayerControlState();

        LockPlayerControls(true);


        // ESC Pause 차단
        if (pauseUI != null)
        {
            previousPauseUIEnabled =
                pauseUI.enabled;

            pauseUI.enabled = false;
        }


        UpdateSlotArrow();
    }


    // =====================================================
    // Player Lock
    // =====================================================

    private void SavePlayerControlState()
    {
        if (playerMoveTestLerp != null)
        {
            previousCanMove =
                playerMoveTestLerp.canMove;
        }


        if (playerAction != null)
        {
            previousPlayerActionLocked =
                playerAction.IsLockedByEvent;
        }
    }


    private void LockPlayerControls(bool locked)
    {
        if (playerMoveTestLerp != null)
        {
            playerMoveTestLerp.canMove =
                !locked;
        }


        if (playerAction != null)
        {
            playerAction.IsLockedByEvent =
                locked;
        }
    }


    private void RestorePlayerControl()
    {
        if (playerMoveTestLerp != null)
        {
            playerMoveTestLerp.canMove =
                previousCanMove;
        }


        if (playerAction != null)
        {
            playerAction.IsLockedByEvent =
                previousPlayerActionLocked;
        }


        if (pauseUI != null)
        {
            pauseUI.enabled =
                previousPauseUIEnabled;
        }
    }


    // =====================================================
    // Slot
    // =====================================================

    private void MoveSlot(int direction)
    {
        if (slotArrowPoints == null ||
            slotArrowPoints.Length == 0)
        {
            return;
        }


        selectedSlot += direction;


        if (selectedSlot < 0)
        {
            selectedSlot =
                slotArrowPoints.Length - 1;
        }
        else if (selectedSlot >=
                 slotArrowPoints.Length)
        {
            selectedSlot = 0;
        }


        UpdateSlotArrow();
    }


    private void UpdateSlotArrow()
    {
        if (slotArrow == null)
            return;


        if (slotArrowPoints == null ||
            selectedSlot < 0 ||
            selectedSlot >= slotArrowPoints.Length)
        {
            return;
        }


        RectTransform point =
            slotArrowPoints[selectedSlot];


        if (point != null)
        {
            slotArrow.position =
                point.position;
        }
    }


    private void SelectCurrentSlot()
    {
        if (slots[selectedSlot] ==
            CallSheetPiece.None)
        {
            OpenPieceChoice();
        }
        else
        {
            OpenRemoveChoice();
        }
    }


    // =====================================================
    // Insert Choice
    // =====================================================

    private void OpenPieceChoice()
    {
        currentChoiceMode =
            ChoiceMode.InsertPiece;


        BuildAvailablePieceList();


        if (availablePieces.Count == 0)
            return;


        selectedChoice = 0;


        if (titleText != null)
        {
            titleText.text =
                "어떤 콜시트 조각을 끼울까?";
        }


        RefreshInsertChoiceTexts();


        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }


        UpdateChoiceArrow();
    }


    // =====================================================
    // Remove Choice
    // =====================================================

    private void OpenRemoveChoice()
    {
        currentChoiceMode =
            ChoiceMode.RemovePiece;


        selectedChoice = 0;


        if (titleText != null)
        {
            titleText.text =
                "콜시트 조각을 꺼낼까?";
        }


        if (choiceTexts != null &&
            choiceTexts.Length > 0 &&
            choiceTexts[0] != null)
        {
            choiceTexts[0].gameObject.SetActive(true);
            choiceTexts[0].text = "꺼낸다";
        }


        if (choiceTexts != null &&
            choiceTexts.Length > 1 &&
            choiceTexts[1] != null)
        {
            choiceTexts[1].gameObject.SetActive(true);
            choiceTexts[1].text = "그만둔다";
        }


        if (choiceTexts != null &&
            choiceTexts.Length > 2 &&
            choiceTexts[2] != null)
        {
            choiceTexts[2].gameObject.SetActive(false);
        }


        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }


        UpdateChoiceArrow();
    }


    // =====================================================
    // Available Pieces
    //
    // 선택창 순서
    //
    // 1. 마지막 촬영의 기억
    // 2. 캐스팅
    // 3. 단 한 번의 상영
    // =====================================================

    private void BuildAvailablePieceList()
    {
        availablePieces.Clear();


        if (InventoryManager.Instance == null)
            return;


        // 마지막 촬영의 기억
        if (lastShootingItem != null &&
            InventoryManager.Instance.HasItem(
                lastShootingItem
            ) &&
            !IsPieceAlreadyInserted(
                CallSheetPiece.LastShooting
            ))
        {
            availablePieces.Add(
                CallSheetPiece.LastShooting
            );
        }


        // 캐스팅
        if (castingItem != null &&
            InventoryManager.Instance.HasItem(
                castingItem
            ) &&
            !IsPieceAlreadyInserted(
                CallSheetPiece.Casting
            ))
        {
            availablePieces.Add(
                CallSheetPiece.Casting
            );
        }


        // 단 한 번의 상영
        if (singleScreeningItem != null &&
            InventoryManager.Instance.HasItem(
                singleScreeningItem
            ) &&
            !IsPieceAlreadyInserted(
                CallSheetPiece.SingleScreening
            ))
        {
            availablePieces.Add(
                CallSheetPiece.SingleScreening
            );
        }
    }


    private bool IsPieceAlreadyInserted(
        CallSheetPiece piece
    )
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == piece)
                return true;
        }


        return false;
    }


    private void RefreshInsertChoiceTexts()
    {
        if (choiceTexts == null)
            return;


        for (int i = 0;
             i < choiceTexts.Length;
             i++)
        {
            if (choiceTexts[i] == null)
                continue;


            if (i < availablePieces.Count)
            {
                choiceTexts[i]
                    .gameObject
                    .SetActive(true);


                choiceTexts[i].text =
                    GetPieceName(
                        availablePieces[i]
                    );
            }
            else
            {
                choiceTexts[i]
                    .gameObject
                    .SetActive(false);
            }
        }
    }


    private string GetPieceName(
        CallSheetPiece piece
    )
    {
        switch (piece)
        {
            case CallSheetPiece.Casting:
                return "캐스팅";

            case CallSheetPiece.LastShooting:
                return "마지막 촬영의 기억";

            case CallSheetPiece.SingleScreening:
                return "단 한 번의 상영";
        }


        return "";
    }


    // =====================================================
    // Choice Input
    // =====================================================

    private void HandleChoiceInput()
    {
        int choiceCount =
            GetCurrentChoiceCount();


        if (choiceCount <= 0)
            return;


        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveChoice(-1);
        }
        else if (Input.GetKeyDown(KeyCode.S) ||
                 Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveChoice(1);
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentChoiceMode ==
                ChoiceMode.InsertPiece)
            {
                InsertSelectedPiece();
            }
            else
            {
                HandleRemoveChoice();
            }

            return;
        }


        if (Input.GetKeyDown(KeyCode.Z))
        {
            CloseChoicePanel();
        }
    }


    private int GetCurrentChoiceCount()
    {
        if (currentChoiceMode ==
            ChoiceMode.InsertPiece)
        {
            return availablePieces.Count;
        }


        return 2;
    }


    private void MoveChoice(int direction)
    {
        int choiceCount =
            GetCurrentChoiceCount();


        if (choiceCount <= 0)
            return;


        selectedChoice += direction;


        if (selectedChoice < 0)
        {
            selectedChoice =
                choiceCount - 1;
        }
        else if (selectedChoice >=
                 choiceCount)
        {
            selectedChoice = 0;
        }


        UpdateChoiceArrow();
    }


    private void UpdateChoiceArrow()
    {
        if (choiceArrow == null)
            return;


        if (choiceArrowPoints == null ||
            selectedChoice < 0 ||
            selectedChoice >= choiceArrowPoints.Length)
        {
            return;
        }


        RectTransform point =
            choiceArrowPoints[selectedChoice];


        if (point != null)
        {
            choiceArrow.position =
                point.position;
        }
    }


    // =====================================================
    // Insert
    // =====================================================

    private void InsertSelectedPiece()
    {
        if (selectedChoice < 0 ||
            selectedChoice >=
            availablePieces.Count)
        {
            return;
        }


        CallSheetPiece piece =
            availablePieces[selectedChoice];


        slots[selectedSlot] =
            piece;


        RefreshSlotVisual(
            selectedSlot
        );


        // =============================================
        // 콜시트 삽입 SFX
        // =============================================

        if (resultAudioSource != null &&
            insertSFX != null)
        {
            resultAudioSource.PlayOneShot(
                insertSFX
            );
        }


        CloseChoicePanel();


        if (AreAllSlotsFilled())
        {
            EvaluatePuzzle();
        }
    }


    // =====================================================
    // Remove
    // =====================================================

    private void HandleRemoveChoice()
    {
        if (selectedChoice == 0)
        {
            RemovePieceFromCurrentSlot();
        }
        else
        {
            CloseChoicePanel();
        }
    }


    private void RemovePieceFromCurrentSlot()
    {
        if (slots[selectedSlot] ==
            CallSheetPiece.None)
        {
            CloseChoicePanel();
            return;
        }


        slots[selectedSlot] =
            CallSheetPiece.None;


        RefreshSlotVisual(
            selectedSlot
        );


        CloseChoicePanel();
    }


    // =====================================================
    // Slot Visual
    // =====================================================

    private void RefreshSlotVisual(int index)
    {
        if (slotPieceImages == null ||
            index < 0 ||
            index >= slotPieceImages.Length)
        {
            return;
        }


        Image image =
            slotPieceImages[index];


        if (image == null)
            return;


        CallSheetPiece piece =
            slots[index];


        if (piece ==
            CallSheetPiece.None)
        {
            image.sprite = null;
            image.gameObject.SetActive(false);

            return;
        }


        switch (piece)
        {
            case CallSheetPiece.Casting:
                image.sprite =
                    castingSprite;
                break;


            case CallSheetPiece.LastShooting:
                image.sprite =
                    lastShootingSprite;
                break;


            case CallSheetPiece.SingleScreening:
                image.sprite =
                    singleScreeningSprite;
                break;
        }


        image.gameObject.SetActive(true);
    }


    // =====================================================
    // Puzzle Check
    // =====================================================

    private bool AreAllSlotsFilled()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] ==
                CallSheetPiece.None)
            {
                return false;
            }
        }


        return true;
    }


    private void EvaluatePuzzle()
    {
        if (resultSequenceRunning)
            return;


        // 정답 순서
        //
        // 캐스팅
        // 마지막 촬영의 기억
        // 단 한 번의 상영

        bool isCorrect =
            slots[0] == CallSheetPiece.Casting &&
            slots[1] == CallSheetPiece.LastShooting &&
            slots[2] == CallSheetPiece.SingleScreening;


        resultSequenceRunning = true;
        isPuzzleOpen = false;


        if (choicePanel != null)
            choicePanel.SetActive(false);


        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);


        if (isCorrect)
        {
            HandleCorrectResult();
        }
        else
        {
            HandleWrongResult();
        }
    }


    // =====================================================
    // Correct
    // =====================================================

    private void HandleCorrectResult()
    {
        puzzleSolved = true;


        if (resultAudioSource != null &&
            correctSFX != null)
        {
            resultAudioSource.PlayOneShot(
                correctSFX
            );
        }


        ConsumeCallSheetItems();


        // 3층 비상문 열쇠 지급
        if (thirdFloorEmergencyKey != null &&
            InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasItem(
                    thirdFloorEmergencyKey
                ))
            {
                InventoryManager.Instance.AddItem(
                    thirdFloorEmergencyKey
                );
            }
        }


        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(
                correctDialogueId
            );


            StartCoroutine(
                WaitForResultDialogueEnd(true)
            );
        }
        else
        {
            FinishCorrectResult();
        }
    }


    private void ConsumeCallSheetItems()
    {
        if (InventoryManager.Instance == null)
            return;


        if (castingItem != null &&
            InventoryManager.Instance.HasItem(
                castingItem
            ))
        {
            InventoryManager.Instance.RemoveItem(
                castingItem.ItemName
            );
        }


        if (lastShootingItem != null &&
            InventoryManager.Instance.HasItem(
                lastShootingItem
            ))
        {
            InventoryManager.Instance.RemoveItem(
                lastShootingItem.ItemName
            );
        }


        if (singleScreeningItem != null &&
            InventoryManager.Instance.HasItem(
                singleScreeningItem
            ))
        {
            InventoryManager.Instance.RemoveItem(
                singleScreeningItem.ItemName
            );
        }
    }


    // =====================================================
    // Wrong
    // =====================================================

    private void HandleWrongResult()
    {
        if (resultAudioSource != null &&
            wrongSFX != null)
        {
            resultAudioSource.PlayOneShot(
                wrongSFX
            );
        }


        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(
                wrongDialogueId
            );


            StartCoroutine(
                WaitForResultDialogueEnd(false)
            );
        }
        else
        {
            ReturnToPuzzleAfterWrongAnswer();
        }
    }


    // =====================================================
    // Result Dialogue
    // =====================================================

    private IEnumerator WaitForResultDialogueEnd(
        bool isCorrect
    )
    {
        yield return new WaitUntil(
            () => dialogueManager.isRunning
        );


        yield return new WaitUntil(
            () => !dialogueManager.isRunning
        );


        if (isCorrect)
        {
            FinishCorrectResult();
        }
        else
        {
            ReturnToPuzzleAfterWrongAnswer();
        }
    }


    private void FinishCorrectResult()
    {
        resultSequenceRunning = false;
        isPuzzleOpen = false;


        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);


        RestorePlayerControl();
    }


    private void ReturnToPuzzleAfterWrongAnswer()
    {
        resultSequenceRunning = false;
        isPuzzleOpen = true;


        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(true);


        if (choicePanel != null)
            choicePanel.SetActive(false);


        for (int i = 0; i < slots.Length; i++)
        {
            RefreshSlotVisual(i);
        }


        UpdateSlotArrow();
    }


    // =====================================================
    // Close
    // =====================================================

    private void CloseChoicePanel()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
    }


    public void ClosePuzzle()
    {
        if (!isPuzzleOpen)
            return;


        if (resultSequenceRunning)
            return;


        ResetSlots();


        isPuzzleOpen = false;


        if (choicePanel != null)
            choicePanel.SetActive(false);


        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);


        RestorePlayerControl();
    }


    private void ResetSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] =
                CallSheetPiece.None;


            if (slotPieceImages != null &&
                i < slotPieceImages.Length &&
                slotPieceImages[i] != null)
            {
                slotPieceImages[i]
                    .gameObject
                    .SetActive(false);
            }
        }
    }


    // =====================================================
    // TEST
    // =====================================================

    [ContextMenu("TEST - Open Puzzle")]
    private void TestOpenPuzzle()
    {
        if (!Application.isPlaying)
            return;


        OpenPuzzle();
    }
}