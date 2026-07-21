using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum S7S2State
{
    None,           // 씬 시작, 전력 차단 상태
    PowerRestored,  // 차단기 레버로 전력 복구, 마네킹 사라짐
    CutterObtained, // 석상에서 '비상용 절단기' 획득
    RopeCut,        // 로프 절단, 2층으로 이동 가능
}

/// <summary>
/// [7-2] 호텔 1층 상호작용 컨트롤러.
/// - 석상: 전력 복구 전/후/절단기 획득 이후 대사 분기, 절단기 지급
/// - 로프: 절단기 보유 여부에 따른 대사 분기, 절단 시 로프/아이템 삭제
/// - 잠긴 문(비품창고): DialoguePoint(StartDialogue = "LockedDoor")로 처리
/// - 안내판(로비/연회장): DialogueObject(StartDialogue = "Sign_Lobby" / "Sign_Banquet")로 처리
/// - 기믹 1(마네킹 왈츠): 씬 시작 대사 종료 즉시 활성화 + 왈츠 BGM 재생
/// - 기믹 2(마네킹 미로): 차단기(화분) 대화 → 암전 + 점등 SFX → 전력 복구, 모든 마네킹 삭제
/// </summary>
public class Sequence7Scene2DialogueController : DialogueController<S7S2State>
{
    [Header("Interactables")]
    [SerializeField] private DialogueObject _statue;
    [SerializeField] private DialogueObject _rope;
    [SerializeField] private DialogueObject _breaker;

    [Header("Item")]
    [SerializeField] private ItemData _cutterData;

    [Header("Lights")]
    [Tooltip("전력 차단 상태에만 켜지는 오브젝트들(플레이어 반경 시야 라이트 등)")]
    [SerializeField] private GameObject[] _darkLightObjects;

    [Tooltip("전력 복구 후에 켜지는 오브젝트들(전역광, 램프 Light 자식 등)")]
    [SerializeField] private GameObject[] _litLightObjects;

    [Header("Mannequins")]
    [Tooltip("전력 복구 시 사라지는 마네킹 전체(메인 로비 + 연회장)")]
    [SerializeField] private GameObject[] _mannequins;

    [Header("기믹 1) 마네킹 왈츠")]
    [Tooltip("씬 시작 대사 종료 즉시 활성화되는 왈츠 마네킹 세트")]
    [SerializeField] private WaltzMannequinSet[] _waltzSets;

    [Tooltip("기믹 시작과 동시에 재생, 로비를 나갈 때까지 루프 (7-2_BGM_waltz_music)")]
    [SerializeField] private AudioClip _waltzBgm;

    [Header("전력 복구 연출")]
    [Tooltip("암전용 페이드 이미지 (씬 컨트롤러의 페이드 이미지와 공용 가능)")]
    [SerializeField] private Image _blackoutImage;
    [SerializeField] private float _blackoutFadeDuration = 0.4f;
    [SerializeField] private float _blackoutHoldDuration = 1f;

    [Header("SFX")]
    [SerializeField] private AudioClip _buttonSfx;      // [3-3 퍼즐] 스위치 효과음과 동일
    [SerializeField] private AudioClip _statueOpenSfx;  // [2-3-9] 난쟁이 석상 효과음과 동일
    [SerializeField] private AudioClip _ropeCutSfx;     // 7-2_SFX_rope_cutting
    [SerializeField] private AudioClip _leverSfx;       // 7-2_SFX_raise_lever
    [SerializeField] private AudioClip _lightOnSfx;     // 7-2_SFX_light_on

    [Header("Dialogue IDs")]
    [SerializeField] private string _openingStartId = "dialogue_1";
    [SerializeField] private string _openingEndId = "dialogue_3";

    private bool _openingSeen = false;
    private string _lastSfxDialogueId = "";

    private const string KEY_OPENING_SEEN = "OpeningSeen";

    protected override void ApplyWorldByState()
    {
        ApplyLightsAndMannequins();
        ApplyInteractables();

        // 재방문/로드 시: 씬 시작 대사를 이미 봤고 전력 복구 전이면 왈츠 즉시 재개
        if (_openingSeen && state < S7S2State.PowerRestored)
            StartWaltz();
    }

    // ---------- 씬 시작 독백 ----------
    /// <summary>씬 컨트롤러에서 호출. 최초 방문에만 독백을 재생한다.</summary>
    public void PlayOpeningMonologueIfNeeded()
    {
        if (_openingSeen) return;
        dialogueManager.StartDialogue(_openingStartId);
    }

    // ---------- 기믹 1) 마네킹 왈츠 ----------
    private void StartWaltz()
    {
        if (_waltzSets != null)
        {
            foreach (var set in _waltzSets)
            {
                if (set != null)
                    set.Activate();
            }
        }

        if (_waltzBgm != null && SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(_waltzBgm);
    }

    /// <summary>연회장 진입 트리거(PlayerZoneTrigger)에서 호출. 왈츠 BGM 정지.</summary>
    public void OnPlayerEnterBanquet()
    {
        if (state < S7S2State.PowerRestored && SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    /// <summary>로비 진입 트리거(PlayerZoneTrigger)에서 호출. 왈츠 BGM 재개.</summary>
    public void OnPlayerEnterLobby()
    {
        if (_openingSeen && state < S7S2State.PowerRestored
            && _waltzBgm != null && SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(_waltzBgm);
    }

    // ---------- 전력 복구 (기믹 2 차단기 레버) ----------
    public void OnPowerRestored()
    {
        if (state >= S7S2State.PowerRestored) return;

        state = S7S2State.PowerRestored;
        PersistPuzzleState();

        StartCoroutine(PowerRestoreSequence());
    }

    /// <summary>암전과 함께 점등 SFX 재생 후 안개 시스템 비활성화, 모든 마네킹 삭제.</summary>
    private IEnumerator PowerRestoreSequence()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();

        yield return FadeBlackout(0f, 1f, _blackoutFadeDuration);

        if (_lightOnSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(_lightOnSfx);

        ApplyLightsAndMannequins();
        ApplyInteractables();

        yield return new WaitForSeconds(_blackoutHoldDuration);

        yield return FadeBlackout(1f, 0f, _blackoutFadeDuration);
    }

    private IEnumerator FadeBlackout(float from, float to, float duration)
    {
        if (_blackoutImage == null) yield break;

        Color c = _blackoutImage.color;
        _blackoutImage.color = new Color(c.r, c.g, c.b, from);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : elapsed / duration;
            _blackoutImage.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, t));
            yield return null;
        }

        _blackoutImage.color = new Color(c.r, c.g, c.b, to);
    }

    private void ApplyLightsAndMannequins()
    {
        bool powerOn = state >= S7S2State.PowerRestored;

        if (_darkLightObjects != null)
        {
            foreach (var obj in _darkLightObjects)
            {
                if (obj != null)
                    obj.SetActive(!powerOn);
            }
        }

        if (_litLightObjects != null)
        {
            foreach (var obj in _litLightObjects)
            {
                if (obj != null)
                    obj.SetActive(powerOn);
            }
        }

        if (powerOn && _mannequins != null)
        {
            foreach (var m in _mannequins)
            {
                if (m != null)
                    m.SetActive(false);
            }
        }
    }

    private void ApplyInteractables()
    {
        if (_statue != null)
        {
            switch (state)
            {
                case S7S2State.None:
                    _statue.StartDialogue = "Statue_BeforePower";
                    break;
                case S7S2State.PowerRestored:
                    _statue.StartDialogue = "Statue_AfterPower";
                    break;
                default:
                    _statue.StartDialogue = "Statue_AfterCutter";
                    break;
            }
        }

        if (_rope != null)
        {
            if (state >= S7S2State.RopeCut)
            {
                _rope.gameObject.SetActive(false);
            }
            else
            {
                _rope.StartDialogue =
                    state >= S7S2State.CutterObtained ? "Rope_HasCutter" : "Rope_NoCutter";
            }
        }

        if (_breaker != null)
        {
            _breaker.StartDialogue =
                state >= S7S2State.PowerRestored ? "Breaker_Already" : "Breaker";
        }
    }

    // ---------- 대화 이벤트 ----------
    protected override void DialogueRunning(string dialogueId)
    {
        // 같은 대화의 여러 라인에서 중복 재생 방지
        if (dialogueId == _lastSfxDialogueId) return;
        _lastSfxDialogueId = dialogueId;

        switch (dialogueId)
        {
            case "Statue_BeforePower_Pressed":
            case "Statue_AfterPower_Pressed":
                PlaySfx(_buttonSfx);
                break;
            case "Statue_Opened":
                PlaySfx(_statueOpenSfx);
                break;
            case "Rope_Cut":
                PlaySfx(_ropeCutSfx);
                break;
            case "Breaker_Raised":
                PlaySfx(_leverSfx);
                break;
        }
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == _openingEndId && !_openingSeen)
        {
            _openingSeen = true;
            PersistPuzzleState();

            // 씬 시작 대사 이벤트가 종료된 즉시 마네킹 왈츠 시스템 활성화
            StartWaltz();
            return;
        }

        switch (dialogueId)
        {
            // 석상에서 절단기 획득
            case "Statue_Opened":
                if (state < S7S2State.CutterObtained)
                {
                    if (_cutterData != null)
                        InventoryManager.Instance.AddItem(_cutterData);

                    state = S7S2State.CutterObtained;
                    PersistPuzzleState();
                    ApplyInteractables();
                }
                break;

            // 차단기 레버: 전력 복구
            case "Breaker_Raised":
                OnPowerRestored();
                break;

            // 로프 절단: 로프 오브젝트 삭제 & 절단기 아이템 삭제
            case "Rope_Cut":
                if (state < S7S2State.RopeCut)
                {
                    if (_cutterData != null)
                        _cutterData.Use();

                    state = S7S2State.RopeCut;
                    PersistPuzzleState();
                    ApplyInteractables();
                }
                break;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(clip);
    }

    // ---------- 추가 저장/복원 ----------
    protected override void PersistExtra()
    {
        GameManager.Instance.SetInt(Key(KEY_OPENING_SEEN), _openingSeen ? 1 : 0);
    }

    protected override void RestoreExtra()
    {
        _openingSeen = GameManager.Instance.GetInt(Key(KEY_OPENING_SEEN), 0) == 1;
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }
}
