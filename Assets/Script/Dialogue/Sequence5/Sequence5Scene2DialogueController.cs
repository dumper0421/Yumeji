using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum S5S2State
{
    None
}
public class Sequence5Scene2DialogueController : DialogueController<S5S2State>
{
    [SerializeField] private ItemData _filmData;
    [SerializeField] private GameObject _filmObject;
    [SerializeField] private GameObject _ReiObject;
    [SerializeField] private GameObject _activeTrigger;

    protected override void ApplyWorldByState()
    {
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "SummerWindFilm_01":
                InventoryManager.Instance.AddItem(_filmData);
                _filmObject.SetActive(false);
                _ReiObject.SetActive(false);
                _activeTrigger.SetActive(true);
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
