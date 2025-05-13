using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class SaveLoadPopup : PopupUI
{
    public bool IsSave = true;

    private SaveSlot[] saveSlots;

    public GameObject SelectBorder;
    public int CurrentSelectIndex = 0;

    protected override void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
        PopupUIManager.Instance.BlockImage.gameObject.SetActive(false);
    }
    protected override void Awake()
    {
        base.Awake();
        saveSlots = transform.GetChild(0).GetComponentsInChildren<SaveSlot>();
    }

    public void SwitchSaveLoadUI(bool isSave)
    {

        foreach (var slot in saveSlots)
        {
            slot.isSave = isSave;
            Button slotButton = slot.GetComponent<Button>();

            slotButton.onClick.RemoveAllListeners();

            if (isSave)
            {
                bool hasData = SaveLoadManager.Instance.LoadGame(slot.SlotIndex) != null;

                slotButton.onClick.AddListener(() =>
                {
                    PopupUIManager.Instance.SetSaveConfirmationDialog(slot.SlotIndex,hasData);
                });
            }
            else
            {
                PlayerSaveData hasData = SaveLoadManager.Instance.LoadGame(slot.SlotIndex);

                if (hasData != null)
                {
                    slotButton.onClick.AddListener(() =>
                    {
                        UnityAction<Scene, LoadSceneMode> loadAction = null;
                        loadAction = (scene, mode) =>
                        {
                            GameManager.Instance.LoadGameData(slot.SlotIndex);
                            UIManager.Instance.GameOverUI.gameObject.SetActive(false);
                            // 이벤트 중복 호출 방지를 위해 등록 해제
                            SceneManager.sceneLoaded -= loadAction;
                        };

                        SceneManager.sceneLoaded += loadAction;
                        SceneManager.LoadScene(hasData.CurrentSceneName);
                    });
                }
            }

            UpdateSaveSlot(slot.SlotIndex);
        }

    }

    public void UpdateSaveSlot(int slotIndex)
    {
        SaveSlot slot = saveSlots[slotIndex];
        PlayerSaveData slotData = SaveLoadManager.Instance.LoadGame(slotIndex);
        slot.SetSaveSlot(slotData);
    }

    public void AllUpdateSaveSlot()
    {
        for (int slotIndex = 0; slotIndex < saveSlots.Length; slotIndex++)
            UpdateSaveSlot(slotIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            gameObject.SetActive(false);

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelectBorder(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelectBorder(1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            saveSlots[CurrentSelectIndex].GetComponent<Button>().onClick?.Invoke();
        }
    }

    public void MoveSelectBorder(int offset)
    {
        CurrentSelectIndex = (CurrentSelectIndex + offset + saveSlots.Length) % saveSlots.Length;
        SelectBorder.transform.SetParent(saveSlots[CurrentSelectIndex].transform);
        SelectBorder.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
    }

}
