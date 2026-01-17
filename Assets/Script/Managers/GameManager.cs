// GameManager.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameManager : Singleton<GameManager>
{
    public PlayerSaveData CurrentSaveData => saveData_;
    public GameObject Player;
    private PlayerSaveData saveData_;
    
    private float totalPlaySeconds_;

    private void Awake()
    {
        Init();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        totalPlaySeconds_ += Time.deltaTime;
    }

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
            playTime: "00:00:00",
            currentSceneName: currentScene.name,
            characterName: "Haru",
            companionName: ""
        );
    }

    /// <summary>
    /// SaveLoadManager 호출 시 사용.
    /// slotIndex를 받아서 SaveData에 반영하고 반환.
    /// </summary>
    public PlayerSaveData SaveGameData(int slotIndex, Vector3 currentPlayerPosition)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        saveData_.SlotIndex = slotIndex;
        saveData_.PlayerPosition = currentPlayerPosition;
        saveData_.PlayTime = FormatHHMMSS(totalPlaySeconds_);
        saveData_.SequenceNum = currentScene.name.Length > 8
                                    ? int.Parse(currentScene.name[8].ToString())
                                    : 0;
        // 테스트
        //saveData_.CompanionName = "Runa";
        return saveData_;
    }

    /// <summary>
    /// LoadGame에서 데이터를 받아와 플레이어 위치를 복원.
    /// </summary>
    public void LoadGameData(int slotIndex)
    {
        PlayerSaveData loaded = SaveLoadManager.Instance.LoadGame(slotIndex);
        if (loaded == null)
        {
            Debug.LogWarning($"[GameManager] No data in slot {slotIndex} to load.");
            return;
        }

        saveData_ = loaded;

        // 플레이어 위치 복원
        GameObject playerObj = GameObject.Find("PlayerHaru");
        if (playerObj != null)
            playerObj.transform.position = saveData_.PlayerPosition;

        totalPlaySeconds_ = ParseHHMMSS(saveData_.PlayTime);

        Debug.Log($"[GameManager] Loaded slot {slotIndex}: pos restored, playTime={saveData_.PlayTime}");
    }

    private static float ParseHHMMSS(string hhmmss)
    {
        if (string.IsNullOrEmpty(hhmmss))
            return 0f;

        // TimeSpan.Parse는 "hh:mm:ss" 형태를 잘 처리함
        if (TimeSpan.TryParse(hhmmss, out var ts))
            return (float)ts.TotalSeconds;

        return 0f;
    }

    // 초 -> "HH:mm:ss"
    private static string FormatHHMMSS(float totalSeconds)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int h = sec / 3600;
        int m = (sec % 3600) / 60;
        int s = sec % 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    public String GetSequenceNameByNum(int num)
    {
        string returnValue = "";
        switch (num)
        {
            case 1:
                returnValue = "오프닝 씬";
                break;
            case 2:
                returnValue = "사양";
                break;
            case 3:
                returnValue = "피크닉";
                break;
            case 4:
                returnValue = "반영식";
                break;
            case 5:
                returnValue = "꿈의 방";
                break;
            case 6:
                returnValue = "여름 바람";
                break;
            case 7:
                returnValue = "RETAKE";
                break;
            case 8:
                returnValue = "일식";
                break;
            case 9:
                returnValue = "거울";
                break;
            case 10:
                returnValue = "만년";
                break;
            case 11:
                returnValue = "라스트 씬";
                break;
        }
        return returnValue;
    }

    public void AddCompanion(string companionName)
    {
        if (saveData_.CompanionName == "")
            saveData_.CompanionName += companionName;
        else
            saveData_.CompanionName += ("," + companionName);
    }

    public void InitCompanion()
    {
        saveData_.CompanionName = "";
    }
}
