using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


/// <summary>
///  대화 선택지
/// </summary>
[System.Serializable]
public class DialogueOption
{
    public string text;
    public string nextId;
    public bool hasIntermediateAction;
    public string requiresItem;
}

/// <summary>
///  대화 라인
/// </summary>
[System.Serializable]
public class DialogueLine
{
    public string text;
    public bool showPortrait;
    public string nextId;
}
/// <summary>
///  대화 데이터
/// </summary>
[System.Serializable]
public class Dialogue
{
    public string id;
    public string speaker;
    public DialogueLine[] lines;
    public DialogueOption[] options;
    public bool autoAdvance;
    public float autoAdvanceDelay;
}
/// <summary>
///  화자 정보
/// </summary>
[System.Serializable]
public class SpeakerInfo
{
    public string speakerName;
    public Sprite portrait;
}
/// <summary>
/// 대화를 관리하는 매니저 클래스.
/// UI 갱신, 입력 처리, 대사 표시, 선택지 표시 및 대화 종료를 담당.
/// </summary>
public class DialogueManager : MonoBehaviour
{

    /// <summary>
    /// 선택지가 선택될 때 발생하는 이벤트. (텍스트, 다음 대화 ID 반환)
    /// </summary>
    public event Action<string, string> OnOptionSelected;
    /// <summary>
    /// 대화가 완료될 때 발생하는 이벤트. (대화 ID 반환)
    /// </summary>
    public event Action<string> OnDialogueComplete;
    /// <summary>
    /// 대화가 진행될 때 발생하는 이벤트. (대화 ID 반환)
    /// </summary>
    public event Action<string> OnDialogueAction;


    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image leftPortraitImage;
    public Image rightPortraitImage;
    public TextMeshProUGUI dialogueText;
    public Transform optionPanel;
    public Button buttonPrefab;

    [Header("Speakers")]
    public SpeakerInfo[] speakerInfos;

    [Header("Input Keys")]
    public KeyCode advanceKey = KeyCode.Space;
    public KeyCode enterKey = KeyCode.Return;
    public bool IsStop = false;

    private Dictionary<string, Dialogue> _dialogues;
    private Dictionary<string, Sprite> _portraitMap;
    private Dialogue _current;
    private Queue<DialogueLine> _lines;
    
    private DialogueLine currentLine;

    private List<Button> optionButtons = new List<Button>();
    private int selectedOption = 0;

    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 128); 
    [SerializeField] private Color32 selectedColor = new Color32(128, 128, 0, 128);

    public bool _waitingForInput = false;
    public bool isRunning = false;

    void Awake()
    {
        dialoguePanel.SetActive(false);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);

        _portraitMap = new Dictionary<string, Sprite>();
        foreach (var info in speakerInfos)
        {
            if (!string.IsNullOrEmpty(info.speakerName) && info.portrait != null)
                _portraitMap[info.speakerName] = info.portrait;
        }
    }

    public void Initialize(Dictionary<string, Dialogue> dialogues)
    {
        _dialogues = dialogues;
    }

    void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        if (optionPanel.gameObject.activeSelf && optionButtons.Count > 0)
        {
            if (_playerMove != null)
                _playerMove.enabled = false;
            if (Input.GetKeyDown(KeyCode.DownArrow))
                ChangeSelection(1);
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                ChangeSelection(-1);
            else if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(enterKey))
            {
                optionButtons[selectedOption].onClick.Invoke();
            }
            return;
        }

        if (_waitingForInput)
        {
            if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(enterKey) && !IsStop)
            {
                 _waitingForInput = false;
                DisplayNext();
            }
        }
    }
    /// <summary>
    /// 지정된 대화 ID로부터 대화를 시작한다.
    /// 대화창을 활성화하고, 플레이어 조작을 막으며 첫 라인을 출력한다.
    /// </summary>
    public void StartDialogue(string dialogueId)
    {
        if (_dialogues == null || !_dialogues.TryGetValue(dialogueId, out _current))
            return;

        if(_playerMove != null)
            _playerMove.enabled = false;

        isRunning = true;
        dialoguePanel.SetActive(true);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);
        optionPanel.gameObject.SetActive(false);

        _lines = new Queue<DialogueLine>(_current.lines ?? Array.Empty<DialogueLine>());
        _waitingForInput = false;
        selectedOption = 0;
        optionButtons.Clear();
        if (_playerMove != null)
        {
            _playerMove.animator.SetBool("Walking", false);
            _playerMove.animator.SetBool("Pushing", false);
            _playerMove.enabled = false;
        }
        DisplayNext();
    }
    /// <summary>
    /// 현재 대화 라인 또는 선택지를 진행한다.
    /// 라인이 남아 있으면 표시하고, 없으면 다음 ID 또는 선택지로 넘어간다.
    /// </summary>
    private void DisplayNext()
    {
        ClearOptions();
        _waitingForInput = false;

        if (IsStop) return;
        if (_lines.Count > 0)
        {
            currentLine = _lines.Dequeue();
            if (currentLine.text == null)
                dialoguePanel.GetComponent<Image>().enabled = false;
            else
                dialoguePanel.GetComponent<Image>().enabled = true;

            dialogueText.text = currentLine.text;
            OnDialogueAction?.Invoke(_current.id);
            if (currentLine.showPortrait)
            {
                bool isHero = _current.speaker == "�Ϸ�";
                var target = isHero ? leftPortraitImage : rightPortraitImage;
                var other = isHero ? rightPortraitImage : leftPortraitImage;
                other.gameObject.SetActive(false);

                if (_portraitMap.TryGetValue(_current.speaker, out var sprite))
                {
                    target.sprite = sprite;
                    target.gameObject.SetActive(true);
                }
            }
            else
            {
                leftPortraitImage.gameObject.SetActive(false);
                rightPortraitImage.gameObject.SetActive(false);
            }

            if (_current.autoAdvance)
            {
                StartCoroutine(AutoAdvance());
            }
            else
            {
                _waitingForInput = true;
            }
            return;
        }

        if (currentLine != null&&!string.IsNullOrEmpty(currentLine.nextId))
        {
            OnDialogueComplete?.Invoke(_current.id);
            StartDialogue(currentLine.nextId);
            return;
        }

        if(currentLine == null)
        {
            OnDialogueComplete?.Invoke(_current.id);
        }

        if (_current.options != null && _current.options.Length > 0)
        {
            optionPanel.gameObject.SetActive(true);
            ShowOptions(_current.options);
            UpdateOptionVisuals();
            return;
        }

        EndDialogue();
    }


    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(_current.autoAdvanceDelay);
        DisplayNext();
    }
    /// <summary>
    /// 선택지를 UI에 표시한다.
    /// 필요 아이템이 없는 경우만 활성화된다.
    /// </summary>
    private void ShowOptions(DialogueOption[] opts)
    {
        optionButtons.Clear();
        selectedOption = 0;

        foreach (var opt in opts)
        {

            if (!string.IsNullOrEmpty(opt.requiresItem) && !InventoryManager.Instance.HasItem(opt.requiresItem))
                continue;  

            var btn = Instantiate(buttonPrefab, optionPanel);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = opt.text;

            btn.onClick.AddListener(() =>
            {
                OnOptionSelected?.Invoke(opt.text, opt.nextId);

                ClearOptions();
                if (opt.nextId == "End")
                {
                    EndDialogue();
                }
              
                if (!opt.hasIntermediateAction)
                {
                    StartDialogue(opt.nextId);
                }
            });

            optionButtons.Add(btn);
        }
        
        if (optionButtons.Count == 0)
        {
            EndDialogue();
            return;
        }

        optionPanel.gameObject.SetActive(true);
        UpdateOptionVisuals();
    }
    /// <summary>
    /// 선택지 버튼들을 제거한다.
    /// </summary>
    private void ClearOptions()
    {
        foreach (Transform t in optionPanel)
            Destroy(t.gameObject);
        optionButtons.Clear();
        optionPanel.gameObject.SetActive(false);
    }
    /// <summary>
    /// 현재 선택된 옵션을 변경한다. (위/아래 입력 처리)
    /// </summary>
    private void ChangeSelection(int delta)
    {
        if (optionButtons.Count == 0) return;
        selectedOption = (selectedOption + delta + optionButtons.Count) % optionButtons.Count;
        UpdateOptionVisuals();
    }
 /// <summary>
    /// 옵션 버튼 색상을 갱신한다.
    /// </summary>
    private void UpdateOptionVisuals()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].image.color = (i == selectedOption)
                ? selectedColor
                : normalColor;
        }
    }

    /// <summary>
    /// 대화를 종료하고 UI를 비활성화한다.
    /// 플레이어 조작을 다시 활성화한다.
    /// </summary>
    private void EndDialogue()
    {
        isRunning = false;
        dialoguePanel.SetActive(false);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);
        OnDialogueComplete?.Invoke(_current.id);
        if (_playerMove != null)
            _playerMove.enabled = true ;
    }
}