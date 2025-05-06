// InventoryManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    public ItemDataSerializable(int id, string name, string desc, int count)
    {
        ItemId = id;
        ItemName = name;
        Description = desc;
        Count = count;
    }
}

public class InventoryManager : Singleton<InventoryManager>
{
    public GameObject ItemSlotPrefab;
    [SerializeField] private Transform _slotSectionTransform;
    public List<ItemSlot> _slots = new List<ItemSlot>();

    private const string inventoryFileNameFormat = "inventorySlot{0}.json";

    protected override void Init()
    {
        foreach (var slot in GetComponentsInChildren<ItemSlot>())
            _slots.Add(slot);

    }

    public void Start()
    {
        LoadInventory(GameManager.Instance.CurrentSaveData.SlotIndex);
    }

    public void SaveInventory(int slotIndex)
    {
        var data = new InventoryData();
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty)
            {
                var item = slot.GetItemData();
                data.Items.Add(new ItemDataSerializable(
                    item.ItemId,
                    item.ItemName,
                    item.Description,
                    slot.Count
                ));
            }
        }

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(
            Application.persistentDataPath,
            string.Format(inventoryFileNameFormat, slotIndex)
        );
        File.WriteAllText(path, json);
        Debug.Log($"[InventoryManager] Saved to {path}");
    }

    public void LoadInventory(int slotIndex)
    {
        // 슬롯이 하나도 없으면(아직 UI가 초기화되지 않았으면) 로드 무시
        if (_slots == null || _slots.Count == 0)
        {
            Debug.LogWarning("[InventoryManager] 슬롯이 초기화되지 않아 로드 생략");
            return;
        }

        string path = Path.Combine(
            Application.persistentDataPath,
            string.Format(inventoryFileNameFormat, slotIndex)
        );
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[InventoryManager] File not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<InventoryData>(json);

        foreach (var slot in _slots)
            slot.ClearSlot();

        foreach (var it in data.Items)
        {
            var slot = _slots.Find(s => s.IsEmpty);
            if (slot == null)
            {
                var obj = Instantiate(ItemSlotPrefab, _slotSectionTransform);
                slot = obj.GetComponent<ItemSlot>();
                _slots.Add(slot);
            }

            var newItem = new ItemData
            {
                ItemId = it.ItemId,
                ItemName = it.ItemName,
                Description = it.Description
            };
            slot.SetItem(newItem, it.Count);
        }

        Debug.Log($"[InventoryManager] Loaded from {path}");
    }

    public void AddItem(ItemData data)
    {
        // 1) 이미 있는 아이템이면 수량만 증가
        var existing = _slots.Find(s => !s.IsEmpty && s.GetItemData().ItemId == data.ItemId);
        if (existing != null)
        {
            existing.IncrementCount();
            return;
        }

        // 2) 빈 슬롯 찾기 (없으면 새로 생성)
        var slot = _slots.Find(s => s.IsEmpty);
        if (slot == null)
        {
            var obj = Instantiate(ItemSlotPrefab, _slotSectionTransform);
            slot = obj.GetComponent<ItemSlot>();
            _slots.Add(slot);
        }

        // 3) 항상 count=1로 세팅
        slot.SetItem(data, 1);
    }
}
