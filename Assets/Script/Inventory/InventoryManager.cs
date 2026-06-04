// InventoryManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.Progress;
#endif

[Serializable]
public class InventoryData
{
    public List<ItemDataSerializable> Items = new List<ItemDataSerializable>();
}

[Serializable]
public class ItemDataSerializable
{
    public int ItemId;
    public string ItemName;
    public string Description;
    public int Count;
    public bool IsConsumable;

    public ItemDataSerializable(int id, string name, string desc, int count, bool isConsumable)
    {
        ItemId = id;
        ItemName = name;
        Description = desc;
        Count = count;
        IsConsumable = isConsumable;
    }
}

public class InventoryManager : Singleton<InventoryManager>
{
    public GameObject ItemSlotPrefab;
    public GameObject BackGround;
    [SerializeField] private Transform _slotSectionTransform;
    public List<ItemSlot> _slots = new List<ItemSlot>();
    public int CurrentSelectIndex = 0;
    public TextMeshProUGUI DescriptionText;
    public GameObject SelectBorder;

    // DontDestroyOnLoad 전에 false로 설정해 씬 전환 시 함께 파괴되도록 합니다.
    protected override void Awake()
    {
        IsDontDestroyOnLoad = false;
        base.Awake();
    }

    protected override void Init()
    {
        foreach (var slot in GetComponentsInChildren<ItemSlot>())
            _slots.Add(slot);
    }

    public void Start()
    {
        IsDontDestroyOnLoad = false;
        SelectBorder.SetActive(false);
        RefreshAllSlots();
    }

    // GameManager의 인벤토리 데이터를 기반으로 UI 슬롯을 전부 갱신합니다.
    public void RefreshAllSlots()
    {
        if (_slots == null || _slots.Count == 0) return;

        foreach (var slot in _slots)
            slot.ClearSlot();

        var items = GameManager.Instance?.GetInventoryItems();
        if (items == null) return;

        foreach (var it in items)
        {
            var slot = _slots.Find(s => s.IsEmpty);
            if (slot == null)
            {
                if (ItemSlotPrefab == null || _slotSectionTransform == null) break;
                var obj = Instantiate(ItemSlotPrefab, _slotSectionTransform);
                slot = obj.GetComponent<ItemSlot>();
                _slots.Add(slot);
            }

            var newItem = ScriptableObject.CreateInstance<ItemData>();
            newItem.ItemId = it.ItemId;
            newItem.ItemName = it.ItemName;
            newItem.Description = it.Description;
            newItem.IsConsumable = it.IsConsumable;
            slot.SetItem(newItem, it.Count);
        }
    }

    public void Update()
    {
        if (BackGround == null || !BackGround.gameObject.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveSelectBorder(1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveSelectBorder(-1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (_slots.Count > 0 && !_slots[CurrentSelectIndex].IsEmpty) { }
        }
    }

    // 아이템 추가: GameManager 데이터 갱신 후 UI 반영
    public void AddItem(ItemData data)
    {
        GameManager.Instance?.AddInventoryItem(
            new ItemDataSerializable(data.ItemId, data.ItemName, data.Description, 1, data.IsConsumable)
        );

        if (_slots == null || _slots.Count == 0 || BackGround == null) return;

        var existing = _slots.Find(s => !s.IsEmpty && s.GetItemData().ItemName == data.ItemName);
        if (existing != null)
        {
            existing.IncrementCount();
            return;
        }

        var slot = _slots.Find(s => s.IsEmpty);
        if (slot == null)
        {
            var obj = Instantiate(ItemSlotPrefab, _slotSectionTransform);
            slot = obj.GetComponent<ItemSlot>();
            _slots.Add(slot);
        }

        slot.SetItem(data, 1);
        int slotIndex = _slots.IndexOf(slot);
        CurrentSelectIndex = slotIndex;

        if (BackGround.activeSelf)
            MoveSelectBorderTo(slotIndex);

        Debug.Log($"[AddItem] SO.name='{data.name}', ItemName='{data.ItemName}'");
    }

    // 아이템 제거: GameManager 데이터 갱신 후 UI 반영
    public void RemoveItem(string itemName)
    {
        GameManager.Instance?.RemoveInventoryItem(itemName);
        RefreshAllSlots();
    }

    // HasItem은 GameManager의 인메모리 데이터를 기준으로 확인 (씬 전환 직후에도 정확)
    public bool HasItem(string itemName)
    {
        return GameManager.Instance?.HasInventoryItem(itemName) ?? false;
    }

    public void RefreshSelectBorder()
    {
        if (_slots == null || _slots.Count == 0 || _slots[CurrentSelectIndex].IsEmpty)
        {
            if (SelectBorder != null) SelectBorder.SetActive(false);
            return;
        }
        MoveSelectBorderTo(CurrentSelectIndex);
    }

    public void MoveSelectBorder(int offset)
    {
        if (_slots == null || _slots.Count == 0) return;
        int count = _slots.Count;
        CurrentSelectIndex = (CurrentSelectIndex + offset + count) % count;
        if (_slots[CurrentSelectIndex].IsEmpty) return;
        if (DescriptionText != null)
            DescriptionText.text = _slots[CurrentSelectIndex].GetItemData().Description;
        if (SelectBorder != null)
        {
            SelectBorder.transform.SetParent(_slots[CurrentSelectIndex].transform);
            SelectBorder.gameObject.SetActive(true);
            SelectBorder.gameObject.transform.localPosition = Vector3.zero;
        }
    }

    public void UseSelectedItem()
    {
        if (_slots == null || _slots.Count == 0) return;
        var slot = _slots[CurrentSelectIndex];
        if (slot.IsEmpty) return;
        var data = slot.GetItemData();
        data.Use();
    }

    public bool HasItem(ItemData itemData) => HasItem(itemData.ItemName);

    public ItemSlot GetItemSlot(string itemName)
    {
        if (_slots == null) return null;
        return _slots.Find(s => !s.IsEmpty && s.GetItemData().ItemName == itemName);
    }

    public void CompactInventory()
    {
        if (_slots == null) return;

        int targetIndex = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_slots[i].IsEmpty)
            {
                if (i != targetIndex)
                {
                    var item = _slots[i].GetItemData();
                    var count = _slots[i].Count;
                    _slots[targetIndex].SetItem(item, count);
                    _slots[i].ClearSlot();
                }
                targetIndex++;
            }
        }

        CurrentSelectIndex = Mathf.Clamp(CurrentSelectIndex, 0, Mathf.Max(0, targetIndex - 1));

        if (targetIndex == 0)
        {
            if (DescriptionText != null) DescriptionText.text = string.Empty;
            if (SelectBorder != null) SelectBorder.gameObject.SetActive(false);
        }
        else
        {
            MoveSelectBorderTo(CurrentSelectIndex);
        }
    }

    public void MoveSelectBorderTo(int newIndex)
    {
        if (_slots == null || newIndex < 0 || newIndex >= _slots.Count) return;
        if (_slots[newIndex].IsEmpty) return;

        CurrentSelectIndex = newIndex;

        if (DescriptionText != null)
            DescriptionText.text = _slots[newIndex].GetItemData().Description;

        if (SelectBorder != null)
        {
            SelectBorder.transform.SetParent(_slots[newIndex].transform, false);
            SelectBorder.transform.localPosition = Vector3.zero;
            SelectBorder.SetActive(true);
        }
    }
}
