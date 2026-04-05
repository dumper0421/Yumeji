using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
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

    [Header("Hanger Masks")]
    public ItemData BlackFeatherBlackMask;
    public ItemData WhiteFeatherBlackMask;
    public ItemData WhiteFeatherWhiteMask;

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
