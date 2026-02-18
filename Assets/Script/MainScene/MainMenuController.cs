using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class MainMenuController : MonoBehaviour
{
    public GameObject SelectBorder;
    public GameObject SaveLoadPopUp;
    public GameObject SettingCanvasObject;

    public AudioClip BGM;

    private Button[] buttons_;
    private int CurrentSelectIndex = 0;
    private void Awake()
    {
        buttons_ = GetComponentsInChildren<Button>();
        Time.timeScale = 1f;
        Screen.SetResolution(1344, 960, true);
    }

    public void Start()
    {
        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM(BGM);
    }

    public void Update()
    {
        if (SaveLoadPopUp.gameObject.activeSelf || SettingCanvasObject.activeSelf) return;

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
            buttons_[CurrentSelectIndex].onClick?.Invoke();
        }
    }

    private void OnButtonPointerEnter(Button btn)
    {
        if (btn.GetComponentInChildren<TMP_Text>() is TMP_Text tmp)
        {
            tmp.color = Color.red;
        }
    }

    private void OnButtonPointerExit(Button btn)
    {
        if (btn.GetComponentInChildren<TMP_Text>() is TMP_Text tmp)
        {
            tmp.color = Color.white;
        }
    }

    public void MoveSelectBorder(int offset)
    {
        CurrentSelectIndex = (CurrentSelectIndex + offset + buttons_.Length) % buttons_.Length;
        SelectBorder.transform.SetParent(buttons_[CurrentSelectIndex].transform);
        SelectBorder.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
    }

    public void GameQuit()
    {
        Application.Quit();
    }
}
