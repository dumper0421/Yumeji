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


    private Button[] buttons_;
    private int CurrentSelectIndex = 0;
    private void Awake()
    {
        buttons_ = GetComponentsInChildren<Button>();

        //foreach (Button btn in buttons_)
        //{
        //    EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        //    if (trigger == null)
        //    {
        //        trigger = btn.gameObject.AddComponent<EventTrigger>();
        //    }

        //    EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        //    entryEnter.eventID = EventTriggerType.PointerEnter;
        //    entryEnter.callback.AddListener((data) => { OnButtonPointerEnter(btn); });
        //    trigger.triggers.Add(entryEnter);

        //    EventTrigger.Entry entryExit = new EventTrigger.Entry();
        //    entryExit.eventID = EventTriggerType.PointerExit;
        //    entryExit.callback.AddListener((data) => { OnButtonPointerExit(btn); });
        //    trigger.triggers.Add(entryExit);
        //}
    }

    public void Update()
    {
        if (SaveLoadPopUp.gameObject.activeSelf) return;

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
