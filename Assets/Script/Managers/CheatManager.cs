using System.IO;
using UnityEngine;

public class CheatManager : Singleton<CheatManager>
{
    protected override void Init() { }

    private void Update()
    {
        // Ctrl + Shift + Delete : 전체 세이브 초기화
        if (Input.GetKey(KeyCode.LeftControl) &&
            Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Delete))
        {
            ResetAllSaves();
        }
    }

    private void ResetAllSaves()
    {
        const int slotCount = 5;

        for (int i = 0; i < slotCount; i++)
        {
            SaveLoadManager.Instance.DeleteSave(i);

            string inventoryPath = Path.Combine(Application.persistentDataPath, $"inventorySlot{i}.json");
            if (File.Exists(inventoryPath))
            {
                File.Delete(inventoryPath);
                Debug.Log($"[CheatManager] inventorySlot{i}.json 삭제");
            }
        }

        Debug.Log("[CheatManager] 모든 세이브 파일 초기화 완료");
    }
}
