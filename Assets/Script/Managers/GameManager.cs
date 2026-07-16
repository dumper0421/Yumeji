using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public PlayerSaveData CurrentSaveData => saveData_;
    public GameObject Player;

    private PlayerSaveData saveData_;
    private float totalPlaySeconds_;
    private List<ItemDataSerializable> inventoryItems_ = new List<ItemDataSerializable>();

    private Dictionary<string, bool> boolStates_ = new Dictionary<string, bool>();
    private Dictionary<string, int> intStates_ = new Dictionary<string, int>();

    protected override void Awake()
    {
        base.Awake();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        totalPlaySeconds_ += Time.deltaTime;
    }

    protected override void Init()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        int seqNum = ExtractSequenceNum(currentScene.name);

        Vector3 startPos = Player != null ? Player.transform.position : Vector3.zero;

        saveData_ = new PlayerSaveData(
            slotIndex: 0,
            sequenceNum: seqNum,
            playerPosition: startPos,
            playTime: "00:00:00",
            currentSceneName: currentScene.name,
            characterName: "Haru",
            companionName: "",
            stateName: ""
        );

        boolStates_.Clear();
        intStates_.Clear();
        inventoryItems_ = new List<ItemDataSerializable>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var found = GameObject.Find("Haru_Player");
        if (found != null)
            Player = found;
    }

    public bool GetFlag(string key, bool defaultValue = false) =>
        boolStates_.TryGetValue(key, out var v) ? v : defaultValue;

    public void SetFlag(string key, bool value = true) => boolStates_[key] = value;

    public int GetInt(string key, int defaultValue = 0) =>
        intStates_.TryGetValue(key, out var v) ? v : defaultValue;

    public void SetInt(string key, int value) => intStates_[key] = value;

    public PlayerSaveData SaveGameData(int slotIndex, Vector3 currentPlayerPosition, Vector3 currentCompanionPosition)
    {
        Scene currentScene = SceneManager.GetActiveScene();

        saveData_.SlotIndex = slotIndex;
        saveData_.PlayerPosition = currentPlayerPosition;
        saveData_.CompanionPosition = currentCompanionPosition;
        saveData_.PlayTime = FormatHHMMSS(totalPlaySeconds_);
        saveData_.CurrentSceneName = currentScene.name;
        saveData_.SequenceNum = ExtractSequenceNum(currentScene.name);

        saveData_.BoolStates = ToBoolList(boolStates_);
        saveData_.IntStates = ToIntList(intStates_);

        return saveData_;
    }

    // 씬 로드 전에 호출 — intStates/boolStates/인벤토리를 메모리에 올린다.
    // RestorePuzzleState() 보다 반드시 먼저 실행돼야 한다.
    public void PreloadGameData(int slotIndex)
    {
        PlayerSaveData loaded = SaveLoadManager.Instance.LoadGame(slotIndex);
        if (loaded == null)
        {
            Debug.LogWarning($"[GameManager] No data in slot {slotIndex} to load.");
            return;
        }

        ApplyLoadedSaveData(loaded);

        var invItems = SaveLoadManager.Instance.LoadInventoryItems(slotIndex);
        inventoryItems_ = invItems ?? new List<ItemDataSerializable>();

        Debug.Log($"[GameManager] PreloadGameData slot {slotIndex}: states loaded");
    }

    // 씬 로드 후에 호출 — 플레이어 위치와 카메라를 복원한다.
    public void RestorePlayerPosition()
    {
        if (saveData_ == null)
            return;

        GameObject playerObj = Player != null ? Player : GameObject.Find("Haru_Player");
        if (playerObj == null)
            return;

        Vector3 before = playerObj.transform.position;
        Vector3 target = saveData_.PlayerPosition;

        var mover = playerObj.GetComponent<PlayerMove_Test_Lerp>();
        if (mover != null)
            mover.Teleport(target);
        else
            playerObj.transform.position = target;

        // Cinemachine에 워프를 알려 카메라가 즉시 이동하게 한다.
        // 알리지 않으면 댐핑 때문에 카메라가 천천히 따라온다.
        Vector3 delta = target - before;
        var brain = GameObject.FindFirstObjectByType<CinemachineBrain>();
        if (brain != null)
        {
            var vcam = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
            if (vcam != null)
                vcam.OnTargetObjectWarped(playerObj.transform, delta);
        }

        Debug.Log($"[GameManager] RestorePlayerPosition: {before} → {target}");
    }

    public void LoadGameData(int slotIndex)
    {
        PreloadGameData(slotIndex);

        GameObject playerObj = Player != null ? Player : GameObject.Find("Haru_Player");
        if (playerObj != null)
            playerObj.transform.position = saveData_.PlayerPosition;

        Debug.Log(
            $"[GameManager] Loaded slot {slotIndex}: pos restored, playTime={saveData_?.PlayTime}"
        );
    }

    private void ApplyLoadedSaveData(PlayerSaveData loaded)
    {
        saveData_ = loaded;
        totalPlaySeconds_ = ParseHHMMSS(saveData_.PlayTime);

        boolStates_ = ToBoolDict(saveData_.BoolStates);
        intStates_ = ToIntDict(saveData_.IntStates);
    }

    private static List<BoolEntry> ToBoolList(Dictionary<string, bool> dict)
    {
        var list = new List<BoolEntry>(dict.Count);
        foreach (var kv in dict)
            list.Add(new BoolEntry { Key = kv.Key, Value = kv.Value });
        return list;
    }

    private static Dictionary<string, bool> ToBoolDict(List<BoolEntry> list)
    {
        var dict = new Dictionary<string, bool>();
        if (list == null)
            return dict;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || string.IsNullOrEmpty(e.Key))
                continue;
            dict[e.Key] = e.Value;
        }
        return dict;
    }

    private static List<IntEntry> ToIntList(Dictionary<string, int> dict)
    {
        var list = new List<IntEntry>(dict.Count);
        foreach (var kv in dict)
            list.Add(new IntEntry { Key = kv.Key, Value = kv.Value });
        return list;
    }

    private static Dictionary<string, int> ToIntDict(List<IntEntry> list)
    {
        var dict = new Dictionary<string, int>();
        if (list == null)
            return dict;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || string.IsNullOrEmpty(e.Key))
                continue;
            dict[e.Key] = e.Value;
        }
        return dict;
    }

    private static float ParseHHMMSS(string hhmmss)
    {
        if (string.IsNullOrEmpty(hhmmss))
            return 0f;

        if (TimeSpan.TryParse(hhmmss, out var ts))
            return (float)ts.TotalSeconds;

        return 0f;
    }

    private static string FormatHHMMSS(float totalSeconds)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int h = sec / 3600;
        int m = (sec % 3600) / 60;
        int s = sec % 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    private static int ExtractSequenceNum(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return 0;

        const string token = "Sequence";
        int idx = sceneName.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            int start = idx + token.Length;
            int end = start;
            while (end < sceneName.Length && char.IsDigit(sceneName[end]))
                end++;

            if (end > start)
            {
                string digits = sceneName.Substring(start, end - start);
                if (int.TryParse(digits, out int v))
                    return v;
            }
        }

        int i = 0;
        while (i < sceneName.Length && !char.IsDigit(sceneName[i]))
            i++;
        if (i == sceneName.Length)
            return 0;

        int j = i;
        while (j < sceneName.Length && char.IsDigit(sceneName[j]))
            j++;

        if (int.TryParse(sceneName.Substring(i, j - i), out int fallback))
            return fallback;

        return 0;
    }

    public void AddCompanion(string companionName)
    {
        if (saveData_ == null)
            return;

        if (string.IsNullOrEmpty(saveData_.CompanionName))
        {
            saveData_.CompanionName = companionName;
            return;
        }

        string[] existingNames = saveData_.CompanionName.Split(',');
        if (System.Array.IndexOf(existingNames, companionName) >= 0)
            return;

        saveData_.CompanionName += ("," + companionName);
    }

    public void InitCompanion()
    {
        if (saveData_ == null)
            return;
        saveData_.CompanionName = "";
    }

    public string GetSequenceNameByNum(int num)
    {
        switch (num)
        {
            case 1:
                return "오프닝 씬";
            case 2:
                return "사양";
            case 3:
                return "피크닉";
            case 4:
                return "반영식";
            case 5:
                return "꿈의 방";
            case 6:
                return "여름 바람";
            case 7:
                return "RETAKE";
            case 8:
                return "일식";
            case 9:
                return "거울";
            case 10:
                return "만년";
            case 11:
                return "라스트 씬";
        }
        return "";
    }

    public bool HasCompanion()
    {
        return saveData_.CompanionName != "";
    }

    // ─── 인벤토리 데이터 (씬 전환 시에도 유지) ────────────────────────────
    public bool HasInventoryItem(string itemName) =>
        inventoryItems_.Exists(i => i.ItemName == itemName);

    public void AddInventoryItem(ItemDataSerializable item)
    {
        var existing = inventoryItems_.Find(i => i.ItemName == item.ItemName);
        if (existing != null)
        {
            existing.Count += item.Count;
            return;
        }
        inventoryItems_.Add(item);
    }

    public void RemoveInventoryItem(string itemName)
    {
        var item = inventoryItems_.Find(i => i.ItemName == itemName);
        if (item == null)
            return;
        if (item.Count > 1)
        {
            item.Count--;
            return;
        }
        inventoryItems_.Remove(item);
    }

    public ItemDataSerializable GetInventoryItem(string itemName) =>
        inventoryItems_.Find(i => i.ItemName == itemName);

    public List<ItemDataSerializable> GetInventoryItems() => inventoryItems_;

    public void SetInventoryItems(List<ItemDataSerializable> items) =>
        inventoryItems_ = items ?? new List<ItemDataSerializable>();
}
