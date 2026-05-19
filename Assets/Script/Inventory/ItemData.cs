using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    public int ItemId;
    public string ItemName;
    public string Description;
    public bool IsConsumable;

    /// <summary>
    /// 이 메서드를 아이템마다 오버라이드해서 동작을 구현하세요.
    /// </summary>
    public virtual void Use()
    {
        InventoryManager.Instance.GetItemSlot(ItemName).DecrementCount();
    }
}
