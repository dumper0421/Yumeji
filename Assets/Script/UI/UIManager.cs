using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public AudioClip BGM;
    public GameObject GameOverUI;
    public GameObject MainCanvas;

    [SerializeField]
    private Button continueButton_;
    [SerializeField]
    private Button TitleButton_;
    [SerializeField]
    private Image _selectBorder;

    private Button _selectbutton;

    // DontDestroyOnLoad 전에 false로 설정해야 Awake에서 적용됩니다.
    protected override void Awake()
    {
        IsDontDestroyOnLoad = false;
        base.Awake();
    }

    protected override void Init()
    {
        _selectbutton = continueButton_;
    }

    private void Start()
    {
        // MainCanvas 프리팹의 게임오버 버튼은 씬마다 대상을 연결해야 하는데, 연결이 빠진 씬(7-2 등)에서는
        // Enter를 눌러도 아무 일도 일어나지 않았다. 연결이 없으면 여기서 기본 동작을 붙인다.
        if (!HasValidPersistentCall(continueButton_))
            continueButton_?.onClick.AddListener(() => PopupUIManager.Instance?.SetSaveLoadPopup(false));

        if (!HasValidPersistentCall(TitleButton_))
            TitleButton_?.onClick.AddListener(GoToTitle);
    }

    private static bool HasValidPersistentCall(Button button)
    {
        if (button == null)
            return true;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) != null
                && !string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(i)))
                return true;
        }
        return false;
    }

    private static void GoToTitle()
    {
        Time.timeScale = 1f;
        SoundManager.Instance?.StopAllSFX();
        SoundManager.Instance?.StopBGM();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/2026PlayX4/TitleScene");
    }


    public void OpenGameOverUI()
    {
        GameOverUI.SetActive(true);
        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM(BGM);
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

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && GameOverUI.activeSelf)
        {
            _selectbutton.onClick.Invoke();
        }
    }
}
