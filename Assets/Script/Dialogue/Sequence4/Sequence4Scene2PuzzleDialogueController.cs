using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using UnityEngine;
public enum S4S2PuzzleState
{

}
public class Sequence4Scene2PuzzleDialogueController : DialogueController<S4S2PuzzleState>
{

    public ItemData ScriptPage;
    public GameObject InteractionLight;
    public DialogueObject shelfObject;

    protected override void ApplyWorldByState() 
    {
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "shelf":
                InventoryManager.Instance.AddItem(ScriptPage);
                InteractionLight.gameObject.SetActive(false);
                shelfObject.StartDialogue = "shelf_After";
                break;
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
    }

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
