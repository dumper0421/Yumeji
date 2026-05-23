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

    [SerializeField] private CompanionSystem _reiCompanionSystem;
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] private BoxCollider2D _reiBoxCollider;
    [SerializeField] private GameObject _stageDoor;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(AddStartItems());
    }

    private IEnumerator AddStartItems()
    {
        yield return null;
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
            case "HaruRei_13":
                _playerMove.SetCompanion(_reiCompanionSystem, "레이");
                _reiBoxCollider.enabled = false;
                _stageDoor.SetActive(false);
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
