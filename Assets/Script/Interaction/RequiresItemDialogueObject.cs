using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 아이템이 있을 때만 대화 걸 수 있는 오브젝트
public class RequiresItemDialogueObject : DialogueObject
{
    // 필요한 아이템 데이터
    [SerializeField]
    private List<ItemData> _requiredItemDatas;

    // 아이템 소모해야 될 때
    [SerializeField]
    private bool _isUse;

    protected override void OnInspect()
    {
        foreach (ItemData item in _requiredItemDatas)
        {
            if (!InventoryManager.Instance.HasItem(item.ItemName))
            {
                return;
            }
        }

        foreach (ItemData item in _requiredItemDatas)
            item.Use();

        if (!DialogueManager.isRunning)
            DialogueManager.StartDialogue(StartDialogue);
    }
}
