using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupUI : MonoBehaviour
{
    public Button CloseButton;

   

    protected virtual void Awake()
    {
        CloseButton.onClick.AddListener(() =>
        {
            OnCloseButtonClicked();
            Debug.Log("dd");
        });
    }

    protected virtual void OnCloseButtonClicked()
    {
        PopupUIManager.Instance.ClosePopUp(this);
        ;
    }

}
