using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public GameObject GameOverUI;
    public GameObject MainCanvas;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnSingletonInit()
    {
    }

    public void Init()
    {
        GameOverUI.gameObject.SetActive(false);
    }


    public void OpenGameOverUI()
    {
        GameOverUI.SetActive(true);
    }
}
