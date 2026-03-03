// ItemSlot.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    private ItemData Data;
    public bool IsEmpty => Data == null;
    public int Count { get; private set; }

    [SerializeField] private Image IconImage;
    [SerializeField] private TextMeshProUGUI NameText;
    [SerializeField] private TextMeshProUGUI ItemCountText;

    public void SetItem(ItemData data, int count = 1)
    {
        Data = data;
        Count = count;
        NameText.text = data.ItemName;
        IconImage.sprite = ResourceManager.Instance.Load<Sprite>("ItemIcon/" + data.ItemName);
        UpdateCountText();
        gameObject.SetActive(true);
        Debug.Log(gameObject.name);
    }

    public void IncrementCount(int amount = 1)
    {
        Count += amount;
        UpdateCountText();
    }

    public void DecrementCount(int amount = 1)
    {
        if (IsEmpty) return;

        Count -= amount;
        if (Count <= 0)
        {
            ClearSlot();
            InventoryManager.Instance.CompactInventory();
        }
        else
        {
            UpdateCountText();
        }
    }

    public void UpdateCountText()
    {
        ItemCountText.text = Count.ToString();
    }

    public ItemData GetItemData() => Data;

    public void ClearSlot()
    {
        Data = null;
        Count = 0;
        NameText.text = string.Empty;
        ItemCountText.text = string.Empty;
        IconImage.sprite = null;
        gameObject.SetActive(false);
    }
}
