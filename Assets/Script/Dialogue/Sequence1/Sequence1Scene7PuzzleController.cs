using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public enum S1S7State
{
    None,
    Clear
}

public class Sequence1Scene7PuzzleController : DialogueController<S1S7State>
{
    public ItemData MatchData;
    public ItemData CharredPhotographData;

    public Image CharredPhotograph;
    public DialogueObject FirePlace;
    public void Update()
    {
        if (CharredPhotograph.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                CharredPhotograph.gameObject.SetActive(false);
        }
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch(dialogueId)
        {
            case "Save":
                PopupUIManager.Instance.SetSaveLoadPopup(true);
                break;

            case "Drawer_Match":
                InventoryManager.Instance.AddItem(MatchData);
                break;
            case "Bookshelf1_Slot1":
                InventoryManager.Instance.AddItem(CharredPhotographData);
                break;
            case "Fireplace_Lit":
                FirePlace.StartDialogue = "Fireplace2";
                FirePlace.IsDisposable = true;
                FirePlace.hasBeenInspected = false;
                break;

        }
        TryProgress();
    }

    protected override void DialogueRunning(string dialogueId)
    {
        switch(dialogueId)
        {
            case "Bookshelf1_Illust":
                CharredPhotograph.gameObject.SetActive(true);
                break;
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
        switch(nextId)
        {
            case "Fireplace_Lit":
                MatchData.Use();
                break;
            case "Fireplace_Phtograph":
                CharredPhotographData.Use();
                OnPuzzleComplete();
                break;
        }
    }

    protected override void OnPuzzleComplete()
    {
    }

    protected override void TryProgress()
    {
    }
}
