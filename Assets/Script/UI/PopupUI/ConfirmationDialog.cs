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

    private Button _selectbutton;

    [SerializeField] Color32 selectedColor = new Color32(164,10,10,255);  // 선택 상태 색
    [SerializeField] Color32 normalColor = new Color32(47,47,47,255);  // 비선택 상태 색
    [SerializeField] private Image _blackImage;
    
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (_selectbutton == confirmButton_) 
                _selectbutton = denyButton_;
            else
                _selectbutton = confirmButton_;

            UpdateButtonColors();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            _selectbutton.onClick.Invoke();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _selectbutton = confirmButton_;
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

    void UpdateButtonColors()
    {
        confirmButton_.GetComponent<Image>().color =
            (_selectbutton == confirmButton_) ? selectedColor : normalColor;
        denyButton_.GetComponent<Image>().color =
            (_selectbutton == denyButton_) ? selectedColor : normalColor;
    }

    private void OnEnable()
    {
        if (_blackImage != null)
            _blackImage.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_blackImage != null)
            _blackImage.gameObject.SetActive(false);
    }

}
