using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public GameObject GameOverUI;
    public GameObject MainCanvas;

    [SerializeField]
    private Button continueButton_;
    [SerializeField]
    private Button TitleButton_;
    [SerializeField]
    private Image _selectBorder;

    private Button _selectbutton;

    protected override void Init()
    {
        _selectbutton = continueButton_;
    }


    public void OpenGameOverUI()
    {
        GameOverUI.SetActive(true);
    }

    public void Update()
    {
        if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow)) && GameOverUI.activeSelf)
        {
            if (_selectbutton == continueButton_)
                _selectbutton = TitleButton_;
            else
                _selectbutton = continueButton_;

            _selectBorder.transform.SetParent(_selectbutton.transform);
            _selectBorder.GetComponent<RectTransform>().localPosition = Vector3.zero;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) && GameOverUI.activeSelf)
        {
            _selectbutton.onClick.Invoke();
        }
    }
}
