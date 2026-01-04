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
    public GameObject Setting;

    public GameObject BackGround;

    public ConfirmationDialog ConfirmationDialog;
    public GameObject SaveLoadPopup;

    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove_Test_Lerp;

    private int CurrentSelectIndex = 0;

    private void Awake()
    {
        if (_playerMove_Test_Lerp == null)
        {
            _playerMove_Test_Lerp = GameObject.Find("PlayerHaru").GetComponent<PlayerMove_Test_Lerp>();
        }
    }

    void Start()
    {
        Options = transform.GetChild(0).transform.GetChild(0).GetChild(0).GetComponentsInChildren<TextMeshProUGUI>();

        Action denyAction = () => {
           ConfirmationDialog.gameObject.SetActive(false); 
        };

        Action confirmAction = () => {
            SoundManager.Instance.StopAllSFX();
            SoundManager.Instance.StopBGM();
            SceneManager.LoadScene("TitleScene");
        };

        ConfirmationDialog.SetAction(confirmAction, denyAction);

        Setting = SettingManager.Instance.gameObject.transform.GetChild(0).gameObject;
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
            {
                BackGround.gameObject.SetActive(true);
                _playerMove_Test_Lerp.enabled = false;
                Time.timeScale = 0f;
            }

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
            Time.timeScale = 1f;
            _playerMove_Test_Lerp.enabled = true;
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
            Setting.SetActive(true);
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
            Setting.SetActive(false);
        }
    }

    private void OnEnable()
    {
        
    }
    

}
