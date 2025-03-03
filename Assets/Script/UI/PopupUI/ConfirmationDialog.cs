using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationDialog : PopupUI
{
    private Action confirmAction_;
    private Action denyAction_;

    [SerializeField]
    private Button confirmButton_;
    [SerializeField]
    private Button denyButton_;
    [SerializeField]
    private TextMeshProUGUI contentTMP_;



    protected override void Awake()
    {
        base.Awake();
    }

    public void SetAction(Action confirmAction, Action denyAction)
    {
        confirmAction_ = confirmAction;
        denyAction_ = denyAction;

        confirmButton_.onClick.RemoveAllListeners();
        denyButton_.onClick.RemoveAllListeners();

        confirmButton_.onClick.AddListener(() =>
        {
            confirmAction_?.Invoke();
        });

        denyButton_.onClick.AddListener(() =>
        {
            denyAction_?.Invoke();
        });
    }

    public void SetButtonText(string denyText , string confirmText)
    {
        denyButton_.GetComponentInChildren<TextMeshProUGUI>().text = denyText;
        confirmButton_.GetComponentInChildren<TextMeshProUGUI>().text = confirmText;
    }


    public void SetContentText(string contentText)
    {
        contentTMP_.text = contentText; 
    }
}
