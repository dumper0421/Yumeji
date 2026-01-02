// SaveLoadManager.cs
using System;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public int SlotIndex;        // 저장된 슬롯 인덱스
    public int SequenceNum;
    public Vector3 PlayerPosition;
    public string PlayTime;
    public string CurrentSceneName;
    public string CharacterName;

    public PlayerSaveData(int slotIndex, int sequenceNum, Vector3 playerPosition, string playTime, string currentSceneName, string characterName)
    {
        SlotIndex = slotIndex;
        SequenceNum = sequenceNum;
        PlayerPosition = playerPosition;
        PlayTime = string.IsNullOrEmpty(playTime) ? "00:00:00" : playTime;
        CurrentSceneName = currentSceneName;
        CharacterName = characterName;
    }
}

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    [SerializeField] private const int saveSlotCount_ = 5;

    private const string inventoryFileNameFormat = "saveSlot{0}.json";

    protected override void Init()
    {
        // 필요 시 초기화 로직
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount_)
        {
            Debug.LogError($"[SaveLoadManager] Invalid save slot: {slotIndex}");
            return;
        }

        // 플레이어 위치 가져오기
        var playerObj = GameObject.Find("PlayerHaru");
        if (playerObj == null)
        {
            Debug.LogError("[SaveLoadManager] PlayerHaru 오브젝트를 찾을 수 없습니다.");
            return;
        }
        Vector3 pos = playerObj.transform.position;

        // GameManager에 저장 요청 (slotIndex 포함)
        PlayerSaveData data = GameManager.Instance.SaveGameData(slotIndex, pos);

        // JSON 직렬화 및 파일 쓰기
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath,
                     string.Format(inventoryFileNameFormat, slotIndex));
        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] Saved slot {slotIndex} → {path}");

        // 인벤토리도 저장
        InventoryManager.Instance.SaveInventory(slotIndex);
    }

    public PlayerSaveData LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount_)
        {
            Debug.LogError($"[SaveLoadManager] Invalid load slot: {slotIndex}");
            return null;
        }

        string path = Path.Combine(Application.persistentDataPath,
                     string.Format(inventoryFileNameFormat, slotIndex));
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoadManager] No save file at slot {slotIndex}");
            return null;
        }

        // JSON 읽기 및 역직렬화
        string json = File.ReadAllText(path);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
        Debug.Log($"[SaveLoadManager] Loaded slot {slotIndex} ← {path}");

        return data;
    }

    public void DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount_)
        {
            Debug.LogError($"[SaveLoadManager] Invalid delete slot: {slotIndex}");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath,
                     string.Format(inventoryFileNameFormat, slotIndex));
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveLoadManager] Deleted save slot {slotIndex}");
        }
    }
}
