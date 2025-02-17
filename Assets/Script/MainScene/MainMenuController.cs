using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private Button[] buttons_;

    private void Awake()
    {
        buttons_ = GetComponentsInChildren<Button>();

        foreach (Button btn in buttons_)
        {
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { OnButtonPointerEnter(btn); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { OnButtonPointerExit(btn); });
            trigger.triggers.Add(entryExit);
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
}
