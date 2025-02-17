using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public Vector3 PlayerPosition;
    public float CurrentHealth;
    // public List<string> Items;
}



public class SaveLoadManager : MonoBehaviour
{
    [SerializeField]
    private const int saveSlotCount_ = 3; // 세이브 슬롯 개수


        public void SaveGame(int slotIndex, PlayerSaveData data)
        {
            if (slotIndex < 0 || slotIndex >= saveSlotCount_)
            {
                Debug.LogError("Invalid save slot index: " + slotIndex);
                return;
            }

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
