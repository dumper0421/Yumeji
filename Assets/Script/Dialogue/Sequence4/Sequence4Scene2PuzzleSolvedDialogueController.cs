using UnityEngine;

public enum S4S2PuzzleSolvedState
{
    None
}

public class Sequence4Scene2PuzzleSolvedDialogueController : DialogueController<S4S2PuzzleSolvedState>
{
    [Header("Item")]
    public ItemData BrokenSlatePiece;

    [Header("Objects")]
    public DialogueObject directorMannequinObject;
    public GameObject directorSparkleObject;
    public GameObject exitWallObject;

    protected override void ApplyWorldByState()
    {
        ApplySolvedWorld();
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "director_mannequin1":
                TryGetBrokenSlatePiece();
                break;
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
    }

    private void TryGetBrokenSlatePiece()
    {
        if (HasItem(BrokenSlatePiece)) return;

        InventoryManager.Instance.AddItem(BrokenSlatePiece);
        ApplySolvedWorld();
    }

    private bool HasItem(ItemData itemData)
    {
        if (itemData == null) return false;
        return InventoryManager.Instance.HasItem(itemData.ItemName);
    }

    private void ApplySolvedWorld()
    {
        bool hasSlatePiece = HasItem(BrokenSlatePiece);

        if (directorMannequinObject != null)
        {
            directorMannequinObject.StartDialogue = hasSlatePiece
                ? "director_mannequin2"
                : "director_mannequin1";
        }

        if (directorSparkleObject != null)
        {
            directorSparkleObject.SetActive(!hasSlatePiece);
        }

        if (exitWallObject != null)
        {
            exitWallObject.SetActive(!hasSlatePiece);
        }
    }

    protected override void OnPuzzleComplete()
    {
    }

    protected override void TryProgress()
    {
        ApplySolvedWorld();
    }

    public void StartDialogue()
    {
    }
}