using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class DialogueOption
{
    public string text;
    public string nextId;
    public bool hasIntermediateAction;
    public string requiresItem;
}

[System.Serializable]
public class DialogueLine
{
    public string text;
    public bool showPortrait;
    public string nextId;
}

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
[System.Serializable]
public class SpeakerInfo
{
    public string speakerName;
    public Sprite portrait;
}
public class DialogueManager : MonoBehaviour
{
    public event Action<string, string> OnOptionSelected;
    public event Action<string> OnDialogueComplete;
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

    private Dictionary<string, Dialogue> _dialogues;
    private Dictionary<string, Sprite> _portraitMap;
    private Dialogue _current;
    private Queue<DialogueLine> _lines;
    private bool _waitingForInput = false;
    private DialogueLine currentLine;

    private List<Button> optionButtons = new List<Button>();
    private int selectedOption = 0;

    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 128); 
    [SerializeField] private Color32 selectedColor = new Color32(128, 128, 0, 128); 


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
            if (Input.GetKeyDown(KeyCode.DownArrow))
                ChangeSelection(1);
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                ChangeSelection(-1);
            else if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(enterKey))
                optionButtons[selectedOption].onClick.Invoke();
            return;
        }

        if (_waitingForInput)
        {
            if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(enterKey))
            {
                _waitingForInput = false;
                DisplayNext();
            }
        }
    }

    public void StartDialogue(string dialogueId)
    {
        if (_dialogues == null || !_dialogues.TryGetValue(dialogueId, out _current))
            return;

        dialoguePanel.SetActive(true);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);
        optionPanel.gameObject.SetActive(false);

        _lines = new Queue<DialogueLine>(_current.lines ?? Array.Empty<DialogueLine>());
        _waitingForInput = false;
        selectedOption = 0;
        optionButtons.Clear();
        _playerMove.animator.SetBool("Walking", false);
        _playerMove.animator.SetBool("Pushing", false);
        _playerMove.enabled = false; 
        DisplayNext();
    }

    private void DisplayNext()
    {
        // 옵션·입력 초기화
        ClearOptions();
        _waitingForInput = false;

        // 1) 남은 대사 출력
        if (_lines.Count > 0)
        {
            currentLine = _lines.Dequeue();
            if (currentLine.text == null)
                dialoguePanel.GetComponent<Image>().enabled = false;
            else
                dialoguePanel.GetComponent<Image>().enabled = true;

            dialogueText.text = currentLine.text;
            OnDialogueAction?.Invoke(_current.id);
            // 초상화 처리
            if (currentLine.showPortrait)
            {
                bool isHero = _current.speaker == "하루";
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

            // 자동 진행 vs. 입력 대기
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

        // 2) 현재 line 에 nextId 가 지정돼 있으면 자동으로 넘어가기
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

        // 3) 선택지 표시
        if (_current.options != null && _current.options.Length > 0)
        {
            optionPanel.gameObject.SetActive(true);
            ShowOptions(_current.options);
            UpdateOptionVisuals();
            return;
        }

        // 4) 대화 종료
        EndDialogue();
    }


    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(_current.autoAdvanceDelay);
        DisplayNext();
    }

    private void ShowOptions(DialogueOption[] opts)
    {
        // 기존에 만들어 둔 optionButtons, selectedOption 초기화
        optionButtons.Clear();
        selectedOption = 0;

        foreach (var opt in opts)
        {

            if (!string.IsNullOrEmpty(opt.requiresItem) && !InventoryManager.Instance.HasItem(opt.requiresItem))
                continue;  // 인벤 안에 없으면 버튼 생성 안 함

            var btn = Instantiate(buttonPrefab, optionPanel);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = opt.text;

            btn.onClick.AddListener(() =>
            {
                // 이벤트 전달 (퍼즐 컨트롤러가 수신)
                OnOptionSelected?.Invoke(opt.text, opt.nextId);

                ClearOptions();
                if (opt.nextId == "End")
                {
                    EndDialogue();
                }
                // 중간 액션이 없는 옵션만 자동으로 다음 대화 실행
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

    private void ClearOptions()
    {
        foreach (Transform t in optionPanel)
            Destroy(t.gameObject);
        optionButtons.Clear();
        optionPanel.gameObject.SetActive(false);
    }

    private void ChangeSelection(int delta)
    {
        if (optionButtons.Count == 0) return;
        selectedOption = (selectedOption + delta + optionButtons.Count) % optionButtons.Count;
        UpdateOptionVisuals();
    }

    private void UpdateOptionVisuals()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].image.color = (i == selectedOption)
                ? selectedColor
                : normalColor;
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);
        OnDialogueComplete?.Invoke(_current.id);
        _playerMove.enabled = true ;
    }
}