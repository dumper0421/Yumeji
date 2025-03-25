using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupUIManager : Singleton<PopupUIManager>
{
    public Image BlockImage;

    [SerializeField]
    private PopupUI confirmationDialog_;
    [SerializeField]
    private PopupUI saveLoadPopup_;

    private LinkedList<PopupUI> activePopupList_ = new LinkedList<PopupUI>();
    private List<PopupUI> allPopupList_ = new List<PopupUI>();

    private void Update()
    {

        #region UI Open Test ESC 끄기 TAB 열기
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            CloseTopPopUp();
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            SetSaveLoadPopup(true);
        }

        if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            SetSaveLoadPopup(false);
        }    
        #endregion
    }

    protected override void Init()
    {
        ;
    }

    public void SetSaveConfirmationDialog(int slotNum, bool hasData)
    {
        if (confirmationDialog_.gameObject.activeSelf) return;
        
        ConfirmationDialog saveDialog = confirmationDialog_.GetComponent<ConfirmationDialog>();
        
        BlockImage.gameObject.SetActive(true);
        confirmationDialog_.gameObject.SetActive(true);
        activePopupList_.AddFirst(confirmationDialog_);

        Action denyAction = () => {
            saveDialog.gameObject.SetActive(false);
            BlockImage.gameObject.SetActive(false);
        };

        Action confirmAction = () => {
            SaveLoadManager.Instance.SaveGame(slotNum);
            saveLoadPopup_.GetComponent<SaveLoadPopup>().AllUpdateSaveSlot();
            saveDialog.gameObject.SetActive(false);
        };

        saveDialog.SetAction(confirmAction, denyAction);
        if(hasData)
            saveDialog.SetContentText("현재 진행 상황을 슬롯에 덮어쓰기합니다.");
        else
            saveDialog.SetContentText("현재 진행 상황을 저장합니다.");
        saveDialog.SetButtonText("취소", "저장");

        Debug.Log(saveDialog);
    }

    //isSave가 true일 떄 save로 아닐 때 덮어쓰기로 UI 열기
    public void SetSaveLoadPopup(bool isSave)
    {
        saveLoadPopup_.gameObject.SetActive(true);
        activePopupList_.AddFirst(saveLoadPopup_);
        saveLoadPopup_.GetComponent<SaveLoadPopup>().SwitchSaveLoadUI(isSave);
    }

    public void CloseTopPopUp()
    {
        if (activePopupList_.Count == 0) return;

        activePopupList_.First.Value.gameObject.SetActive(false);
        activePopupList_.Remove(activePopupList_.First);

        if (BlockImage.gameObject.activeSelf)
        {
            BlockImage.gameObject.SetActive(false);
        }
    }

    public void ClosePopUp(PopupUI popupUI)
    {
        if (!activePopupList_.Contains(popupUI)) return;

        popupUI.gameObject.SetActive(false);
        activePopupList_.Remove(popupUI);

        if (BlockImage.gameObject.activeSelf)
        {
            BlockImage.gameObject.SetActive(false);
        }
    }
}
