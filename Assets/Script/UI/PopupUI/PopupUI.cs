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
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(() =>
            {
                OnCloseButtonClicked();
            });
        }
    }

    protected virtual void OnCloseButtonClicked()
    {
        PopupUIManager.Instance.ClosePopUp(this);
        ;
    }

    public virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCloseButtonClicked();
        }
    }

}
