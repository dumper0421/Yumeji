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
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image leftPortraitImage;
    [SerializeField] private Image rightPortraitImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform optionPanel;
    [SerializeField] private Button buttonPrefab;

    [Header("Speakers")]
    [SerializeField] private SpeakerInfo[] speakerInfos;

    [Header("Input Keys")]
    [SerializeField] private KeyCode advanceKey = KeyCode.Space;
    [SerializeField] private KeyCode enterKey = KeyCode.Return;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;
    public bool IsStop = false;

    private Dictionary<string, Dialogue> _dialogues;
    private Dictionary<string, Sprite> _portraitMap;
    private Dialogue _current;
    private Queue<DialogueLine> _lines;

    private DialogueLine currentLine;

    private List<Button> optionButtons = new List<Button>();
    private int selectedOption = 0;

    [SerializeField] private PlayerMove_Test_Lerp _playerMove;


    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 128);
    [SerializeField] private Color32 selectedColor = new Color32(128, 128, 0, 128);

    public bool _waitingForInput = false;
    public bool isRunning = false;

    [Header("Next Prompt")]
    [SerializeField] private RectTransform nextPrompt;
    [SerializeField] private float promptAmplitude = 6f;   // 위아래 이동 폭
    [SerializeField] private float promptSpeed = 3f;       // 이동 속도
    private Vector2 nextPromptBasePos;
    private bool nextPromptActive = false;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charsPerSecond = 35f;   // 1초에 몇 글자
    [SerializeField] private float commaPause = 0.05f;     // , 일 때 추가 멈춤
    [SerializeField] private float periodPause = 0.12f;    // .!? 일 때 추가 멈춤

    private Coroutine typingCo;
    private Coroutine autoAdvanceCo;
    private bool isTyping = false;
    private string typingFullText = "";

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

        if (nextPrompt != null)
        {
            nextPromptBasePos = nextPrompt.anchoredPosition;
            nextPrompt.gameObject.SetActive(false);
        }
    }

    public void Initialize(Dictionary<string, Dialogue> dialogues)
    {
        _dialogues = dialogues;
    }

    void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        // 옵션 선택 중이면 옵션 입력만 처리
        if (optionPanel.gameObject.activeSelf && optionButtons.Count > 0)
        {
            if (_playerMove != null)
                _playerMove.enabled = false;

            if (Input.GetKeyDown(KeyCode.DownArrow))
                ChangeSelection(1);
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                ChangeSelection(-1);
            else if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(enterKey))
                optionButtons[selectedOption].onClick.Invoke();

            return;
        }

        // 대화 전체 스킵
        if (!IsStop && Input.GetKeyDown(skipKey))
        {
            SkipToEnd();
            return;
        }

        // 타이핑 중/대기 중 입력 처리 (스킵 우선)
        if ((isTyping || _waitingForInput) && !IsStop)
        {
            if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(enterKey))
            {
                if (isTyping)
                {
                    CompleteTyping();
                }
                else if (_waitingForInput)
                {
                    _waitingForInput = false;
                    DisplayNext();
                }
            }
        }

        // NextPrompt 표시
        if (nextPromptActive && nextPrompt != null)
        {
            float y = Mathf.Sin(Time.unscaledTime * promptSpeed) * promptAmplitude;
            nextPrompt.anchoredPosition = nextPromptBasePos + Vector2.up * y;
        }
    }

    /// <summary>
    /// 지정된 대화 ID로부터 대화를 시작한다.
    /// </summary>
    public void StartDialogue(string dialogueId)
    {
        StopTypingAndAutoAdvance();
        SetNextPrompt(false);

        if (_dialogues == null || !_dialogues.TryGetValue(dialogueId, out _current))
            return;

        if (_playerMove != null)
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
    /// </summary>
    private void DisplayNext()
    {
        ClearOptions();
        _waitingForInput = false;

        if (IsStop) return;

        // 다음 라인 존재
        if (_lines.Count > 0)
        {
            currentLine = _lines.Dequeue();

            // 패널 이미지 표시/숨김
            var panelImg = dialoguePanel.GetComponent<Image>();
            if (panelImg != null)
                panelImg.enabled = (currentLine.text != null);

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

            // 텍스트 타자기 시작
            OnDialogueAction?.Invoke(_current.id);
            StartTypingLine(currentLine.text ?? "");

            return;
        }

        // 라인 다 끝났고 nextId가 있으면 다음 대화로
        if (currentLine != null && !string.IsNullOrEmpty(currentLine.nextId))
        {
            OnDialogueComplete?.Invoke(_current.id);
            StartDialogue(currentLine.nextId);
            return;
        }

        // 라인 자체가 없는 대화였다면 완료 이벤트
        if (currentLine == null)
            OnDialogueComplete?.Invoke(_current.id);

        // 선택지 있으면 선택지 표시
        if (_current.options != null && _current.options.Length > 0)
        {
            optionPanel.gameObject.SetActive(true);
            ShowOptions(_current.options);
            UpdateOptionVisuals();
            return;
        }

        EndDialogue();
    }

    private void StartTypingLine(string text)
    {
        StopTypingAndAutoAdvance();
        SetNextPrompt(false);

        typingFullText = text ?? "";
        dialogueText.text = "";

        // 타자기 비활성/빈문장 -> 즉시 완료 처리
        if (!useTypewriter || string.IsNullOrEmpty(typingFullText))
        {
            dialogueText.text = typingFullText;
            OnLineFinished();
            return;
        }

        isTyping = true;
        typingCo = StartCoroutine(TypeRoutine(typingFullText));
    }

    private IEnumerator TypeRoutine(string full)
    {
        float secPerChar = (charsPerSecond <= 0f) ? 0f : (1f / charsPerSecond);

        for (int i = 0; i < full.Length; i++)
        {
            dialogueText.text += full[i];

            float extra = 0f;
            char c = full[i];

            if (c == ',' || c == '，')
                extra = commaPause;
            else if (c == '.' || c == '!' || c == '?' || c == '。' || c == '！' || c == '？')
                extra = periodPause;

            // 시간정지에도 진행되게 realtime 사용
            float wait = Mathf.Max(0f, secPerChar + extra);
            if (wait > 0f)
                yield return new WaitForSecondsRealtime(wait);
            else
                yield return null;
        }

        isTyping = false;
        typingCo = null;

        OnLineFinished();
    }

    private void CompleteTyping()
    {
        if (!isTyping) return;

        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
        }

        isTyping = false;
        dialogueText.text = typingFullText;

        OnLineFinished();
    }

    private void OnLineFinished()
    {
        // 타이핑이 끝난 시점에서 다음 안내(NextPrompt) 조건 결정
        if (_current != null && _current.autoAdvance)
        {
            SetNextPrompt(false);

            if (autoAdvanceCo != null)
                StopCoroutine(autoAdvanceCo);

            autoAdvanceCo = StartCoroutine(AutoAdvanceAfterLine());
        }
        else
        {
            _waitingForInput = true;
            SetNextPrompt(_lines != null);
        }
    }

    private IEnumerator AutoAdvanceAfterLine()
    {
        float d = (_current != null) ? _current.autoAdvanceDelay : 0f;
        if (d > 0f)
            yield return new WaitForSecondsRealtime(d);

        autoAdvanceCo = null;
        DisplayNext();
    }

    private void StopTypingAndAutoAdvance()
    {
        _waitingForInput = false;
        SetNextPrompt(false);

        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
        }
        if (autoAdvanceCo != null)
        {
            StopCoroutine(autoAdvanceCo);
            autoAdvanceCo = null;
        }

        isTyping = false;
        typingFullText = "";
    }

    private void ShowOptions(DialogueOption[] opts)
    {
        StopTypingAndAutoAdvance();
        SetNextPrompt(false);

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
                    return;
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

    private void ClearOptions()
    {
        foreach (Transform t in optionPanel)
            Destroy(t.gameObject);

        optionButtons.Clear();
        optionPanel.gameObject.SetActive(false);
        SetNextPrompt(false);
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

    public void SkipToEnd()
    {
        if (!isRunning || IsStop) return;

        StopTypingAndAutoAdvance();
        ClearOptions();
        SetNextPrompt(false);

        // 현재 대화의 남은 라인 이벤트 발생
        while (_lines != null && _lines.Count > 0)
        {
            currentLine = _lines.Dequeue();
            OnDialogueAction?.Invoke(_current.id);
        }

        // nextId 체인 끝까지 따라가며 이벤트 발생
        while (true)
        {
            string nextId = currentLine?.nextId ?? "";

            if (string.IsNullOrEmpty(nextId) || !_dialogues.TryGetValue(nextId, out var next))
                break;

            OnDialogueComplete?.Invoke(_current.id);
            _current = next;
            currentLine = null;

            // 선택지는 자동 스킵 불가
            if (_current.options != null && _current.options.Length > 0)
                break;

            foreach (var line in _current.lines ?? Array.Empty<DialogueLine>())
            {
                currentLine = line;
                OnDialogueAction?.Invoke(_current.id);
            }
        }

        EndDialogue();
    }

    private void EndDialogue()
    {
        StopTypingAndAutoAdvance();
        SetNextPrompt(false);

        isRunning = false;
        dialoguePanel.SetActive(false);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);

        if (_playerMove != null)
            _playerMove.enabled = true;

        OnDialogueComplete?.Invoke(_current != null ? _current.id : "");
    }

    private void SetNextPrompt(bool show)
    {
        if (nextPrompt == null) return;

        nextPromptActive = show;
        nextPrompt.gameObject.SetActive(show);

        if (show)
            nextPromptBasePos = nextPrompt.anchoredPosition;
        else
            nextPrompt.anchoredPosition = nextPromptBasePos;
    }
}
