// InventoryManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using static UnityEditor.Progress;

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
    [SerializeField] private Transform _slotSectionTransform;
    public List<ItemSlot> _slots = new List<ItemSlot>();
    public int CurrentSelectIndex = 0;
    public TextMeshProUGUI DescriptionText;
    public GameObject SelectBorder;


    private const string inventoryFileNameFormat = "inventorySlot{0}.json";

    protected override void Init()
    {
        foreach (var slot in GetComponentsInChildren<ItemSlot>())
            _slots.Add(slot);

    }

    public void Start()
    {
        LoadInventory(GameManager.Instance.CurrentSaveData.SlotIndex);
        MoveSelectBorder(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelectBorder(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelectBorder(-1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (_slots[CurrentSelectIndex].IsEmpty) return;

        }

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
                    slot.Count,
                    item.IsConsumable
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
        var existing = _slots.Find(s => !s.IsEmpty && s.GetItemData().ItemName == data.ItemName);
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

    public void MoveSelectBorder(int offset)
    {

        CurrentSelectIndex = (CurrentSelectIndex + offset + _slots.Capacity) % _slots.Capacity;
        if (_slots[CurrentSelectIndex].IsEmpty) return;
        DescriptionText.text = _slots[CurrentSelectIndex].GetItemData().Description;
        SelectBorder.transform.SetParent(_slots[CurrentSelectIndex].transform);
        SelectBorder.gameObject.SetActive(true);
        SelectBorder.gameObject.transform.localPosition = Vector3.zero;
    }

    public void UseSelectedItem()
    {
        var slot = _slots[CurrentSelectIndex];
        if (slot.IsEmpty) return;

        var data = slot.GetItemData();
        data.Use();  // UnityEvent 로 연결된 로직 실행
    }

    public bool HasItem(string itemName)
    {
        return _slots.Exists(s => !s.IsEmpty && s.GetItemData().ItemName == itemName);
    }

    public ItemSlot GetItemSlot(string itemName)
    {
        return _slots.Find(s => s.GetItemData().ItemName == itemName);
    }

    public void CompactInventory()
    {
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
    }
}