using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public int SequenceNum;
    public Vector3 PlayerPosition;
    public float CurrentHealth;
    public string LastPlayTime;

    static public int slotIndex_ = 0;

    public PlayerSaveData(int sequenceNum, Vector3 playerPosition, float currentHealth, DateTime lastPlayTime)
    {
        SequenceNum = sequenceNum;
        PlayerPosition = playerPosition;
        CurrentHealth = currentHealth;
        LastPlayTime = lastPlayTime.ToString("yyyy-MM-dd HH:mm:ss");
        slotIndex_++;
    }
}



public class SaveLoadManager : Singleton<SaveLoadManager>
{
    protected override bool IsGlobal => true;

    [SerializeField]
    private const int saveSlotCount_ = 5; // 세이브 슬롯 개수

    protected override void OnSingletonInit()
    {
        ;
    }


    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount_)
        {
            Debug.LogError("Invalid save slot index: " + slotIndex);
            return;
        }
        GameManager.Instance.SaveGameData(GameObject.Find("PlayerHaru").transform.position);
        PlayerSaveData data = GameManager.Instance.CurrentSaveData;

        string json = JsonUtility.ToJson(data, true);
        string filePath = GetSaveFilePath(slotIndex);
        File.WriteAllText(filePath, json);
        Debug.Log("Game saved to slot " + slotIndex + " at " + filePath);
    }
    
    public PlayerSaveData LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount_)
        {
            Debug.LogError("Invalid save slot index: " + slotIndex);
            return null;
        }
    
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Save file not found in slot " + slotIndex);
            return null;
        }
    
        string json = File.ReadAllText(filePath);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
        Debug.Log("Game loaded from slot " + slotIndex);
        return data;
    }
    
    public void DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saveSlotCount_)
        {
            Debug.LogError("Invalid save slot index: " + slotIndex);
            return;
        }
    
        string filePath = GetSaveFilePath(slotIndex);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("Save file deleted from slot " + slotIndex);
        }
    }
    
    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, "saveSlot" + slotIndex + ".json");
    }
}
