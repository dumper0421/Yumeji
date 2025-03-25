using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public GameObject GameOverUI;
    public GameObject MainCanvas;

    protected override void Init()
    {
     
    }


    public void OpenGameOverUI()
    {
        GameOverUI.SetActive(true);
    }
}
