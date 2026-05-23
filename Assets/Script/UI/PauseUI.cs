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

    private GameObject _dialogueCanvas;
    private InteractionSystem _interactionSystem;

    private bool _wasPlayerMoveEnabled;
    private bool _wasInteractionEnabled;

    private void Awake()
    {
        if (_playerMove_Test_Lerp == null)
        {
            _playerMove_Test_Lerp = FindAnyObjectByType<PlayerMove_Test_Lerp>();
        }

        if (_dialogueCanvas == null)
        {
            var dialogueRoot = GameObject.Find("Dialogue Canvas");
            if (dialogueRoot != null && dialogueRoot.transform.childCount > 2)
                _dialogueCanvas = dialogueRoot.transform.GetChild(2).gameObject;
        }

        if (_interactionSystem == null && _playerMove_Test_Lerp != null)
            _interactionSystem = _playerMove_Test_Lerp.GetComponent<InteractionSystem>();
    }

    void Start()
    {
        Options = transform
            .GetChild(0)
            .transform.GetChild(0)
            .GetChild(0)
            .GetComponentsInChildren<TextMeshProUGUI>();

        Action denyAction = () =>
        {
            ConfirmationDialog.gameObject.SetActive(false);
        };

        Action confirmAction = () =>
        {
            Time.timeScale = 1f;
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
        if (SaveLoadPopup == null)
            return;

        // PauseUI가 열려있으면 다이얼로그 상태와 무관하게 먼저 처리
        if (BackGround.gameObject.activeSelf)
        {
            if (SaveLoadPopup.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    SaveLoadPopup.SetActive(false);
                return;
            }

            if (Inventory.activeSelf || Setting.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    InventoryManager.Instance.SelectBorder.SetActive(false);
                    Inventory.SetActive(false);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
                MoveSelectBorder(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                MoveSelectBorder(1);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                MovePage(CurrentSelectIndex);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePauseUI();
                Time.timeScale = 1f;
                _playerMove_Test_Lerp.enabled = _wasPlayerMoveEnabled;
                _interactionSystem.enabled = _wasInteractionEnabled;
                SoundManager.Instance.ResumeAllAudio();
            }
            return;
        }

        // PauseUI가 닫혀있을 때만 다이얼로그 체크
        if (_dialogueCanvas != null && _dialogueCanvas.activeSelf)
            return;

        if (SaveLoadPopup.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                SaveLoadPopup.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _wasPlayerMoveEnabled = _playerMove_Test_Lerp.enabled;
            _wasInteractionEnabled = _interactionSystem.enabled;
            BackGround.SetActive(true);
            SoundManager.Instance.PauseAllAudio();
            _playerMove_Test_Lerp.enabled = false;
            Time.timeScale = 0f;
            _interactionSystem.enabled = false;
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
            InventoryManager.Instance.RefreshSelectBorder();
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

    private void OnEnable() { }
}
