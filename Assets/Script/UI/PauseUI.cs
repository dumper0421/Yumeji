using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    public GameObject SelectBorder;
    public TextMeshProUGUI[] Options;
    public GameObject Inventory;
    public GameObject BackGround;

    public ConfirmationDialog ConfirmationDialog;
    public GameObject SaveLoadPopup;

    private int CurrentSelectIndex = 0;
    void Start()
    {
        Options = transform.GetChild(0).transform.GetChild(0).GetChild(0).GetComponentsInChildren<TextMeshProUGUI>();

        Action denyAction = () => {
           ConfirmationDialog.gameObject.SetActive(false); 
        };

        Action confirmAction = () => {
            SceneManager.LoadScene("TitleScene");
        };

        ConfirmationDialog.SetAction(confirmAction, denyAction);
    }

    // Update is called once per frame
    void Update()
    {
        if (SaveLoadPopup.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                SaveLoadPopup.gameObject.SetActive(false);

            return;
        }

        if (!BackGround.gameObject.activeSelf && !SaveLoadPopup.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                BackGround.gameObject.SetActive(true);
            return;
        }

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
            MovePage(CurrentSelectIndex);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePauseUI();
        }
    }

    public void MoveSelectBorder(int offset)
    {
        CurrentSelectIndex = (CurrentSelectIndex + offset + Options.Length) % Options.Length;
        SelectBorder.transform.SetParent(Options[CurrentSelectIndex].transform);
        SelectBorder.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
    }

    public void MovePage(int CurrentSelectIndex)
    { 
        if (CurrentSelectIndex == 0)
        {
            Inventory.SetActive(true);
        }
        if (CurrentSelectIndex == 1)
        {

        }
        if (CurrentSelectIndex == 2)
        {
            ConfirmationDialog.gameObject.SetActive(true);
        }
    }

    public void ClosePauseUI()
    {
        BackGround.SetActive(!BackGround.gameObject.activeSelf);

        if (!BackGround.gameObject.activeSelf)
        {
            ConfirmationDialog.gameObject.SetActive(false);
            Inventory.SetActive(false);
        }
    }

}
