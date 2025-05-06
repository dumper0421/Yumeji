// GameManager.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public PlayerSaveData CurrentSaveData => saveData_;
    public GameObject Player;
    private PlayerSaveData saveData_;

    protected override void Init()
    {

        // 기본 SaveData 초기화 (슬롯 0)
        Scene currentScene = SceneManager.GetActiveScene();
        int seqNum = currentScene.name.Length > 8
                                     ? (int)currentScene.name[8]
                                     : 0;
        Vector3 startPos = Player != null
                                     ? Player.transform.position
                                     : Vector3.zero;
        saveData_ = new PlayerSaveData(
            slotIndex: 0,
            sequenceNum: seqNum,
            playerPosition: startPos,
            lastPlayTime: DateTime.Now,
            currentSceneName: currentScene.name
        );
    }

    /// <summary>
    /// SaveLoadManager 호출 시 사용.
    /// slotIndex를 받아서 SaveData에 반영하고 반환.
    /// </summary>
    public PlayerSaveData SaveGameData(int slotIndex, Vector3 currentPlayerPosition)
    {
        saveData_.SlotIndex = slotIndex;
        saveData_.PlayerPosition = currentPlayerPosition;
        saveData_.LastPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return saveData_;
    }

    /// <summary>
    /// LoadGame에서 데이터를 받아와 플레이어 위치를 복원.
    /// </summary>
    public void LoadGameData(int slotIndex)
    {
        PlayerSaveData loaded = SaveLoadManager.Instance.LoadGame(slotIndex);
        if (loaded != null)
        {
            saveData_ = loaded;
            GameObject playerObj = GameObject.Find("PlayerHaru");
            if (playerObj != null)
                playerObj.transform.position = saveData_.PlayerPosition;

            Debug.Log($"[GameManager] Loaded slot {slotIndex}: position restored.");
        }
        else
        {
            Debug.LogWarning($"[GameManager] No data in slot {slotIndex} to load.");
        }
    }
}
