using UnityEngine;

public enum S4S2PuzzleState
{
    None
}

public class Sequence4Scene2PuzzleDialogueController : DialogueController<S4S2PuzzleState>
{
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

    protected override void ApplyWorldByState()
    {
        ApplyTableWorld();
        ApplyMannequin2World();
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
        ApplyTableWorld();
    }

    private bool HasItem(ItemData itemData)
    {
        if (itemData == null) return false;
        return InventoryManager.Instance.HasItem(itemData.ItemName);
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

    protected override void OnPuzzleComplete()
    {
    }

    protected override void TryProgress()
    {
        ApplyTableWorld();
        ApplyMannequin2World();
    }

    public void StartDialogue()
    {
    }
}