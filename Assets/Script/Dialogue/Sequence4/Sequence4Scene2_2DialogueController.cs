using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum S4S2_2State
{
    None
}

public class Sequence4Scene2_2DialogueController : DialogueController<S4S2_2State>
{
    [SerializeField] private ItemData ClapperBoardData;
    [SerializeField] private ItemData SscriptPageData;


    protected override void Start()
    {
        base.Start();
        InventoryManager.Instance.AddItem(ClapperBoardData);
        InventoryManager.Instance.AddItem(SscriptPageData);
    }

    protected override void ApplyWorldByState()
    {
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "HaruRei_10":
                ClapperBoardData.Use();
                SscriptPageData.Use();
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




}
