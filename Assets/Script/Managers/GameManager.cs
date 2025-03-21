using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public PlayerSaveData CurrentSaveData => saveData_;

    private PlayerSaveData saveData_;

    protected override void Init()
    {
        // 초기 데이터를 기본 값으로 생성
        saveData_ = new PlayerSaveData(1, Vector3.zero, 100, DateTime.Now);
        DontDestroyOnLoad(this.gameObject);
    }

    public PlayerSaveData SaveGameData(Vector3 currentPlayerPosition)
    {
        saveData_.PlayerPosition = currentPlayerPosition;
        saveData_.CurrentHealth = StatusManager.Instance.CurrentHealth;
        saveData_.LastPlayTime = DateTime.Now.ToString();

        return saveData_;
    }

    public void LoadGameData(int slotIndex)
    {
        PlayerSaveData loadedData = SaveLoadManager.Instance.LoadGame(slotIndex);
        if (loadedData != null)
        {
            saveData_ = loadedData;
            StatusManager.Instance.playerStatus.CurrentHealth = saveData_.CurrentHealth;
            GameObject.Find("PlayerHaru").transform.position = saveData_.PlayerPosition;
            Debug.Log("Game loaded: GameManager의 SaveData가 업데이트되었습니다.");
        }
        else
        {
            Debug.LogWarning("Slot " + slotIndex + "에 저장된 데이터가 없습니다.");
        }

        
    }
}
