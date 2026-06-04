using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class BoolEntry
{
    public string Key;
    public bool Value;
}

[Serializable]
public class IntEntry
{
    public string Key;
    public int Value;
}

[Serializable]
public class PlayerSaveData
{
    public int SlotIndex;
    public int SequenceNum;
    public Vector3 PlayerPosition;
    public string PlayTime;
    public string CurrentSceneName;
    public string CharacterName;
    public string CompanionName;

    public string StateName;

    public List<BoolEntry> BoolStates = new List<BoolEntry>();
    public List<IntEntry> IntStates = new List<IntEntry>();

    public PlayerSaveData(
        int slotIndex,
        int sequenceNum,
        Vector3 playerPosition,
        string playTime,
        string currentSceneName,
        string characterName,
        string companionName,
        string stateName
    )
    {
        SlotIndex = slotIndex;
        SequenceNum = sequenceNum;
        PlayerPosition = playerPosition;
        PlayTime = string.IsNullOrEmpty(playTime) ? "00:00:00" : playTime;
        CurrentSceneName = currentSceneName;
        CharacterName = characterName;
        CompanionName = companionName;
        StateName = string.IsNullOrEmpty(stateName) ? "" : stateName;
    }
}

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    private const int SaveSlotCount = 5;
    private const string SaveFileNameFormat = "saveSlot{0}.json";

    protected override void Init() { }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SaveSlotCount)
        {
            Debug.LogError($"[SaveLoadManager] Invalid save slot: {slotIndex}");
            return;
        }

        GameObject playerObj =
            (GameManager.Instance != null && GameManager.Instance.Player != null)
                ? GameManager.Instance.Player
                : GameObject.Find("Haru_Player");

        if (playerObj == null)
        {
            Debug.LogError("[SaveLoadManager] Player 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Vector3 pos = playerObj.transform.position;
        PlayerSaveData data = GameManager.Instance.SaveGameData(slotIndex, pos);

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, string.Format(SaveFileNameFormat, slotIndex));
        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] Saved slot {slotIndex} → {path}");

        // 인벤토리는 GameManager 메모리 데이터를 기준으로 저장
        SaveInventoryItems(slotIndex, GameManager.Instance.GetInventoryItems());
    }

    public PlayerSaveData LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SaveSlotCount)
        {
            Debug.LogError($"[SaveLoadManager] Invalid load slot: {slotIndex}");
            return null;
        }

        string path = Path.Combine(
            Application.persistentDataPath,
            string.Format(SaveFileNameFormat, slotIndex)
        );
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoadManager] No save file at slot {slotIndex}");
            return null;
        }

        string json = File.ReadAllText(path);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

        Debug.Log($"[SaveLoadManager] Loaded slot {slotIndex} ← {path}");
        return data;
    }

    public void DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SaveSlotCount)
        {
            Debug.LogError($"[SaveLoadManager] Invalid delete slot: {slotIndex}");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, string.Format(SaveFileNameFormat, slotIndex));
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveLoadManager] Deleted save slot {slotIndex}");
        }
    }

    private const string InventoryFileNameFormat = "inventorySlot{0}.json";

    public void SaveInventoryItems(int slotIndex, System.Collections.Generic.List<ItemDataSerializable> items)
    {
        var data = new InventoryData { Items = items ?? new System.Collections.Generic.List<ItemDataSerializable>() };
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, string.Format(InventoryFileNameFormat, slotIndex));
        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] Inventory saved slot {slotIndex} → {path}");
    }

    public System.Collections.Generic.List<ItemDataSerializable> LoadInventoryItems(int slotIndex)
    {
        string path = Path.Combine(Application.persistentDataPath, string.Format(InventoryFileNameFormat, slotIndex));
        if (!File.Exists(path)) return null;
        var data = JsonUtility.FromJson<InventoryData>(File.ReadAllText(path));
        return data?.Items;
    }
}
