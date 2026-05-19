using UnityEngine;

public enum S4S2PuzzleState
{
    None
}

public class Sequence4Scene2PuzzleDialogueController : DialogueController<S4S2PuzzleState>
{
    [Header("Scene Controller")]
    public Sequence4Scene2SceneController sceneController;

    [Header("Shelf")]
    public ItemData ScriptPage;
    public GameObject InteractionLight;
    public DialogueObject shelfObject;

    [Header("Table Puzzle")]
    public ItemData EmptyChampagneGlass;
    public ItemData FullChampagneGlass;

    public DialogueObject tableObject;
    public DialogueObject waterLeakObject;

    public GameObject emptyGlassOnTableVisual;
    public GameObject fullGlassOnTableVisual;

    private bool isTableSetComplete = false;

    [Header("Lights")]
    public PuzzleLightController Light1Controller;
    public PuzzleLightController Light2Controller;

    [Header("Mannequin 2")]
    public MannequinPoseController mannequin2Controller;
    public DialogueObject mannequin2Object;

    [Header("Hanger / Mannequin1")]
    public ItemData BlackFeatherBlackMask;
    public ItemData WhiteFeatherBlackMask;
    public ItemData WhiteFeatherWhiteMask;

    public DialogueObject hangerObject;
    public DialogueObject mannequin1Object;

    public GameObject mannequin1DefaultVisual;
    public GameObject mannequin1MaskedVisual;

    protected override void ApplyWorldByState()
    {
        ApplyTableWorld();
        ApplyMannequin2World();
        ApplyHangerWorld();
        ApplyMannequin1World();
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "shelf":
                InventoryManager.Instance.AddItem(ScriptPage);

                if (InteractionLight != null)
                    InteractionLight.SetActive(false);

                if (shelfObject != null)
                    shelfObject.StartDialogue = "shelf_After";
                break;

            case "light1_on":
                if (Light1Controller != null)
                    Light1Controller.ActivateLight();
                break;

            case "light2_on":
                if (Light2Controller != null)
                    Light2Controller.ActivateLight();
                break;
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
        switch (text)
        {
            // 1) 테이블 셋팅
            case "줍는다":
                TryTakeEmptyGlass();
                break;

            case "채운다":
                TryFillGlass();
                break;

            case "놓는다":
                TryPlaceFullGlass();
                break;

            // 2) 마네킹2 버튼
            case "누른다":
                if (mannequin2Controller != null)
                    mannequin2Controller.NextPose();

                ApplyMannequin2World();
                break;

            // 3) 행거
            case "검은색 깃털이 달린 검은색 가면":
                TryTakeMask(BlackFeatherBlackMask);
                break;

            case "흰색 깃털이 달린 검은색 가면":
                TryTakeMask(WhiteFeatherBlackMask);
                break;

            case "흰색 깃털이 달린 흰색 가면":
                TryTakeMask(WhiteFeatherWhiteMask);
                break;

            case "걸어둔다":
                TryPutBackMask();
                break;

            // 4) 마네킹1
            case "씌운다":
                TryApplyMaskToMannequin1();
                break;
        }
    }

    private void TryTakeEmptyGlass()
    {
        if (isTableSetComplete) return;
        if (HasItem(EmptyChampagneGlass)) return;
        if (HasItem(FullChampagneGlass)) return;

        InventoryManager.Instance.AddItem(EmptyChampagneGlass);
        ApplyTableWorld();
    }

    private void TryFillGlass()
    {
        if (!HasItem(EmptyChampagneGlass)) return;

        EmptyChampagneGlass.Use();
        InventoryManager.Instance.AddItem(FullChampagneGlass);
        ApplyTableWorld();
    }

    private void TryPlaceFullGlass()
    {
        if (!HasItem(FullChampagneGlass)) return;

        FullChampagneGlass.Use();
        isTableSetComplete = true;

        if (sceneController != null)
            sceneController.IsTablePuzzleSolved = true;

        ApplyTableWorld();
    }

    private void TryTakeMask(ItemData itemData)
    {
        if (sceneController != null && sceneController.IsMannequin1Solved) return;
        if (itemData == null) return;
        if (HasAnyMask()) return;

        InventoryManager.Instance.AddItem(itemData);
        ApplyHangerWorld();
        ApplyMannequin1World();
    }

    private void TryPutBackMask()
    {
        if (sceneController != null && sceneController.IsMannequin1Solved) return;

        if (HasItem(BlackFeatherBlackMask))
            BlackFeatherBlackMask.Use();
        else if (HasItem(WhiteFeatherBlackMask))
            WhiteFeatherBlackMask.Use();
        else if (HasItem(WhiteFeatherWhiteMask))
            WhiteFeatherWhiteMask.Use();

        ApplyHangerWorld();
        ApplyMannequin1World();
    }

    private void TryApplyMaskToMannequin1()
    {
        if (sceneController != null && sceneController.IsMannequin1Solved) return;

        if (HasCorrectMask())
        {
            WhiteFeatherWhiteMask.Use();

            if (sceneController != null)
                sceneController.IsMannequin1Solved = true;

            ApplyMannequin1World();
            ApplyHangerWorld();
            return;
        }

        if (HasWrongMask())
        {
            dialogueManager.StartDialogue("mannequin1_wrongMask");
            return;
        }
    }

    private bool HasItem(ItemData itemData)
    {
        if (itemData == null) return false;
        return InventoryManager.Instance.HasItem(itemData.ItemName);
    }

    private bool HasAnyMask()
    {
        return HasItem(BlackFeatherBlackMask)
            || HasItem(WhiteFeatherBlackMask)
            || HasItem(WhiteFeatherWhiteMask);
    }

    private bool HasCorrectMask()
    {
        return HasItem(WhiteFeatherWhiteMask);
    }

    private bool HasWrongMask()
    {
        return HasItem(BlackFeatherBlackMask) || HasItem(WhiteFeatherBlackMask);
    }

    private void ApplyTableWorld()
    {
        if (tableObject != null)
        {
            if (isTableSetComplete)
                tableObject.StartDialogue = "table_afterSet";
            else if (HasItem(FullChampagneGlass))
                tableObject.StartDialogue = "table_hasFullGlass";
            else if (HasItem(EmptyChampagneGlass))
                tableObject.StartDialogue = "table_hasEmptyGlass";
            else
                tableObject.StartDialogue = "table_noGlass";
        }

        if (waterLeakObject != null)
        {
            if (isTableSetComplete)
                waterLeakObject.StartDialogue = "waterLeak_default";
            else if (HasItem(EmptyChampagneGlass))
                waterLeakObject.StartDialogue = "waterLeak_fillAsk";
            else
                waterLeakObject.StartDialogue = "waterLeak_default";
        }

        if (emptyGlassOnTableVisual != null)
        {
            bool showEmptyGlass = !isTableSetComplete
                                  && !HasItem(EmptyChampagneGlass)
                                  && !HasItem(FullChampagneGlass);

            emptyGlassOnTableVisual.SetActive(showEmptyGlass);
        }

        if (fullGlassOnTableVisual != null)
        {
            fullGlassOnTableVisual.SetActive(isTableSetComplete);
        }
    }

    private void ApplyMannequin2World()
    {
        if (mannequin2Object == null || mannequin2Controller == null) return;

        if (mannequin2Controller.IsSolved)
            mannequin2Object.StartDialogue = "mannequin2_poseCorrect";
        else
            mannequin2Object.StartDialogue = "mannequin2_default";
    }

    private void ApplyHangerWorld()
    {
        if (hangerObject == null) return;

        if (sceneController != null && sceneController.IsMannequin1Solved)
        {
            hangerObject.StartDialogue = "hanger_afterSolved";
            return;
        }

        if (HasAnyMask())
            hangerObject.StartDialogue = "hanger_hasMask";
        else
            hangerObject.StartDialogue = "hanger_noMask";
    }

    private void ApplyMannequin1World()
    {
        if (mannequin1Object == null) return;

        bool solved = sceneController != null && sceneController.IsMannequin1Solved;

        if (solved)
        {
            mannequin1Object.StartDialogue = "mannequin1_afterSolved";

            if (mannequin1DefaultVisual != null)
                mannequin1DefaultVisual.SetActive(false);

            if (mannequin1MaskedVisual != null)
                mannequin1MaskedVisual.SetActive(true);

            return;
        }

        if (HasCorrectMask())
            mannequin1Object.StartDialogue = "mannequin1_hasCorrectMask";
        else if (HasWrongMask())
            mannequin1Object.StartDialogue = "mannequin1_hasWrongMask";
        else
            mannequin1Object.StartDialogue = "mannequin1_default";

        if (mannequin1DefaultVisual != null)
            mannequin1DefaultVisual.SetActive(true);

        if (mannequin1MaskedVisual != null)
            mannequin1MaskedVisual.SetActive(false);
    }

    protected override void OnPuzzleComplete()
    {
    }

    protected override void TryProgress()
    {
        ApplyTableWorld();
        ApplyMannequin2World();
        ApplyHangerWorld();
        ApplyMannequin1World();
    }

    public void StartDialogue()
    {
    }
}