using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum S8S2State
{
    None,     // 편지 낭독 전. 씬 시작 직후
    Letter,   // 편지 낭독 ~ 총성
    Kei,      // 암전 이후 케이와의 대화
    Free,     // 조작 활성화. T2로 시퀀스 9 전환 대기
    Finished, // T2 발동. 시퀀스 9로 넘어가기 직전
}

/// <summary>
/// 시퀀스8 씬2 "마지막 편지" 컨트롤러.
/// 기획서 플로우 1~14번을 대사 블록 종료 시점에 맞춰 실행한다.
///
/// 전체화면 일러스트는 DialogueManager의 imageName을 쓰지 않는다.
/// imageName은 이미지 전용 라인에서만 켜지고 다음 텍스트 라인에서 바로 꺼지는데,
/// 여기서는 일러스트를 띄운 채로 대사가 계속 나가야 하고 크로스페이드도 필요하다.
/// 그래서 일러스트 레이어를 이 컨트롤러가 직접 들고 간다.
/// </summary>
public class Sequence8Scene2DialogueController : DialogueController<S8S2State>
{
    /// <summary>
    /// 정원 색조 한 단계. 8-1과 같은 방식으로 Global Light 2D와 Volume 채도를 함께 바꾼다.
    /// </summary>
    [System.Serializable]
    private class LightStage
    {
        [Tooltip("Global Light 2D의 Intensity")]
        public float intensity = 1f;

        [Tooltip("Global Light 2D의 Color")]
        public Color color = Color.white;

        [Range(-100f, 100f)]
        [Tooltip("Color Adjustments의 Saturation. 0 = 원본, -100 = 완전 흑백")]
        public float saturation = 0f;
    }

    [Header("Haru")]
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [SerializeField]
    private ActorAnimParams _haruAnimParams = new ActorAnimParams();

    [Tooltip("9번: 암전이 끝나고 하루가 다시 보이는 위치 (H)")]
    [SerializeField]
    private Transform _haruHomePoint;

    [Tooltip("9번: 하루가 케이를 보는 방향. 케이가 북쪽 단 위에 있으므로 보통 위쪽.")]
    [SerializeField]
    private Vector2 _haruFacingKei = Vector2.up;

    [Header("Kei")]
    [SerializeField]
    private GameObject _kei;

    [SerializeField]
    private Animator _keiAnimator;

    [SerializeField]
    private ActorAnimParams _keiAnimParams = new ActorAnimParams();

    [Tooltip("1 = 플레이어 걷기와 동일(타일당 0.2초). 낮출수록 느려진다.")]
    [SerializeField]
    private float _keiMoveSpeed = 0.6f;

    [Tooltip("9번: 케이가 등장하는 상부 테라스 위치 (K)")]
    [SerializeField]
    private Transform _keiSpawnPoint;

    [Tooltip("12번: 케이가 붉은 커튼 앞까지 가는 경로. 한 칸씩 4방향으로만 이동한다.")]
    [SerializeField]
    private Transform[] _keiCurtainPath;

    [Tooltip("11번: 케이가 커튼(F) 쪽을 돌아보는 방향")]
    [SerializeField]
    private Vector2 _keiFacingCurtain = Vector2.up;

    [Header("석상")]
    [Tooltip("정상 석상. 총성과 함께 꺼진다.")]
    [SerializeField]
    private GameObject _statue;

    [Tooltip("파괴된 석상 잔해. 총성과 함께 켜진다. 씬에는 꺼둔 채로 배치할 것.")]
    [SerializeField]
    private GameObject _statueRubble;

    [Header("붉은 커튼")]
    [Tooltip("F 지점의 붉은 커튼. 씬에는 알파 0으로 배치해둔다.")]
    [SerializeField]
    private SpriteRenderer _curtain;

    [SerializeField]
    private float _curtainFadeInDuration = 2f;

    [Header("전체화면 일러스트")]
    [Tooltip("화면 전체를 덮는 일러스트 레이어. 크로스페이드를 위해 두 장을 번갈아 쓴다.")]
    [SerializeField]
    private Image _illustrationA;

    [SerializeField]
    private Image _illustrationB;

    [Tooltip("흔들림을 적용할 일러스트 부모. 비워두면 흔들지 않는다.")]
    [SerializeField]
    private RectTransform _illustrationRoot;

    [Tooltip("1번: 개봉 전 편지 봉투")]
    [SerializeField]
    private Sprite _letterClosedSprite;

    [Tooltip("2번: 개봉된 편지 봉투")]
    [SerializeField]
    private Sprite _letterOpenedSprite;

    [Tooltip("4번: 촬영장 전체화면 일러스트")]
    [SerializeField]
    private Sprite _filmSetSprite;

    [Tooltip("5번: 루나가 권총 총구를 턱밑에 댄 일러스트")]
    [SerializeField]
    private Sprite _pistolSprite;

    [Tooltip("6번: 같은 구도의 중앙 석상 일러스트")]
    [SerializeField]
    private Sprite _statueSprite;

    [SerializeField]
    private float _illustrationFadeDuration = 0.6f;

    [Header("총성 연출")]
    [Tooltip("화면 전체를 덮는 흰색 Image. 씬에는 알파 0으로 꺼둔 채 배치할 것.")]
    [SerializeField]
    private Image _flashImage;

    [SerializeField]
    private float _flashHoldDuration = 0.08f;

    [SerializeField]
    private float _flashFadeOutDuration = 0.35f;

    [SerializeField]
    private float _shakeDuration = 0.4f;

    [SerializeField]
    private float _shakeMagnitude = 18f;

    [Header("정원 색조")]
    [SerializeField]
    private Light2D _gardenLight;

    [Tooltip("Color Adjustments 오버라이드가 들어 있는 Global Volume. "
        + "비워두면 채도는 건드리지 않고 조명만 바뀐다.")]
    [SerializeField]
    private Volume _gardenVolume;

    [Tooltip("씬 시작 색조. 8-1이 끝난 상태를 그대로 이어받는다.")]
    [SerializeField]
    private LightStage _lightStageStart = new LightStage
    {
        intensity = 0.5f,
        color = new Color(0.80f, 0.80f, 0.84f),
        saturation = -75f,
    };

    [Tooltip("3번: 편지 본문이 시작될 때 한 단계 더 낮춘다.")]
    [SerializeField]
    private LightStage _lightStageLetter = new LightStage
    {
        intensity = 0.35f,
        color = new Color(0.76f, 0.76f, 0.80f),
        saturation = -88f,
    };

    [Tooltip("9번: 암전 이후 마지막 단계. 거의 흑백.")]
    [SerializeField]
    private LightStage _lightStageFinal = new LightStage
    {
        intensity = 0.25f,
        color = new Color(0.72f, 0.72f, 0.76f),
        saturation = -100f,
    };

    [SerializeField]
    private float _lightFadeDuration = 1.5f;

    // Volume 프로파일에서 찾아둔 Color Adjustments 오버라이드
    private ColorAdjustments _colorAdjustments;

    [Header("사운드")]
    [Tooltip("8-1에서 이어지는 낮은 영사기 작동음. 총성과 함께 끊긴다.")]
    [SerializeField]
    private AudioClip _projectorLoopClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float _projectorVolume = 0.35f;

    [Tooltip("5번: 화면 밖 녹화 시작 SFX")]
    [SerializeField]
    private AudioClip _recordStartSfx;

    [Tooltip("6번: 총성 SFX. 흰색 플래시와 같은 프레임에 재생한다.")]
    [SerializeField]
    private AudioClip _gunshotSfx;

    [Tooltip("8번: 암전 상태에서 남는 이명 SFX")]
    [SerializeField]
    private AudioClip _tinnitusSfx;

    [Range(0f, 1f)]
    [SerializeField]
    private float _tinnitusVolume = 0.5f;

    [Tooltip("11번: 커튼이 완전히 보이는 순간 시작하는 BGM")]
    [SerializeField]
    private AudioClip _redCurtainBgm;

    [Header("카메라")]
    [Tooltip("9~13번: 케이를 비추는 카메라")]
    [SerializeField]
    private CinemachineVirtualCamera _keiCamera;

    [Tooltip("14번: 자유 조작에서 하루를 따라다니는 평상시 카메라")]
    [SerializeField]
    private CinemachineVirtualCamera _playerCamera;

    [Header("트리거")]
    [Tooltip("커튼 앞 T2. 14번에서 열린다.")]
    [SerializeField]
    private GameObject _t2Trigger;

    [Header("타이밍")]
    [SerializeField]
    private float _introFadeInDuration = 1.5f;

    [Tooltip("1번: 개봉 전 봉투를 보여주는 시간")]
    [SerializeField]
    private float _letterClosedHoldDuration = 1.5f;

    [Tooltip("7번: 총성 뒤 암전까지의 시간")]
    [SerializeField]
    private float _blackoutAfterShot = 0.6f;

    [Tooltip("8번: 암전 상태에서 이명이 이어지는 시간")]
    [SerializeField]
    private float _tinnitusDuration = 3f;

    [Tooltip("13번: 케이가 들어간 뒤 루나의 음성까지의 암전 시간")]
    [SerializeField]
    private float _blackoutBeforeLunaVoice = 1f;

    [Header("씬 전환")]
    [SerializeField]
    private string _nextSceneName = "Sequence9S#1";

    [SerializeField]
    private float _fadeOutDuration = 2f;

    // 현재 앞에 나와 있는 일러스트 레이어. 크로스페이드 때마다 뒤집힌다.
    private Image _frontIllustration;
    private Image _backIllustration;

    protected override void Awake()
    {
        base.Awake();

        // Volume.profile은 런타임 사본을 돌려주므로 여기서 값을 바꿔도
        // 프로파일 애셋 원본은 더럽혀지지 않는다. (sharedProfile은 원본이라 위험)
        if (_gardenVolume != null && _gardenVolume.profile != null)
        {
            if (_gardenVolume.profile.TryGet(out _colorAdjustments))
                _colorAdjustments.saturation.overrideState = true;
            else
                Debug.LogWarning(
                    "[S8S2] Volume 프로파일에 Color Adjustments 오버라이드가 없어 채도를 바꿀 수 없다.",
                    this
                );
        }

        _frontIllustration = _illustrationA;
        _backIllustration = _illustrationB;
    }

    protected override void ApplyWorldByState()
    {
        HideIllustration(_illustrationA);
        HideIllustration(_illustrationB);
        SetFlashAlpha(0f);

        if (_t2Trigger != null)
            _t2Trigger.SetActive(false);

        if (state == S8S2State.Finished || state == S8S2State.Free)
        {
            // 세이브 복원: 연출을 재생하지 않고 결과만 맞춰둔다
            ApplyLightStageImmediate(_lightStageFinal);
            SetStatueDestroyed(true);
            SetCurtainAlpha(1f);

            if (_kei != null)
                _kei.SetActive(false);

            HandOverToPlayerCamera();
            LockPlayer(false);

            if (_t2Trigger != null)
                _t2Trigger.SetActive(true);

            return;
        }

        // 정상 진행: 8-1이 끝난 색조에서 시작한다
        ApplyLightStageImmediate(_lightStageStart);
        SetStatueDestroyed(false);
        SetCurtainAlpha(0f);

        if (_kei != null)
            _kei.SetActive(false);

        if (_keiCamera != null)
            _keiCamera.gameObject.SetActive(false);

        LockPlayer(true);

        StartCoroutine(Co_Intro());
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        // DialogueManager.EndDialogue()가 대화가 끝날 때마다 조작을 되돌려놓기 때문에
        // (DialogueManager.cs의 _playerMove.enabled = true)
        // 연출이 이어지는 구간에서는 여기서 다시 잠가야 한다.
        if (dialogueId != "dialogue_13")
            LockPlayer(true);

        switch (dialogueId)
        {
            // 3번: 봉투를 내리고 색조를 한 단계 더 낮춘 뒤 편지 본문
            case "dialogue_1":
                StartCoroutine(Co_LowerTintThenLetter());
                break;

            // 4번: 촬영장 전체화면 일러스트
            case "dialogue_2":
                StartCoroutine(Co_ShowIllustration(_filmSetSprite, "dialogue_3"));
                break;

            // 5번: 권총 일러스트 + 녹화 시작 SFX
            case "dialogue_3":
                StartCoroutine(Co_ShowPistol());
                break;

            // 6번: 같은 구도의 중앙 석상 일러스트로 크로스페이드
            case "dialogue_4":
                StartCoroutine(Co_ShowIllustration(_statueSprite, "dialogue_5"));
                break;

            // 6~9번: 총성 → 석상 붕괴 → 암전 → 이명 → 케이 등장
            case "dialogue_5":
                state = S8S2State.Kei;
                PersistPuzzleState();
                StartCoroutine(Co_GunshotToKei());
                break;

            // 11~12번: 커튼 등장 + BGM + 케이가 커튼 앞으로 이동
            case "dialogue_9":
                StartCoroutine(Co_CurtainAppears());
                break;

            // 12~13번: 케이가 커튼으로 들어가고 암전 중 루나의 음성
            case "dialogue_11":
                StartCoroutine(Co_KeiExits());
                break;

            // 13번: 화면을 밝히고 하루의 마지막 대사
            case "dialogue_12":
                StartCoroutine(Co_LightsUpThenLastLine());
                break;

            // 14번: 조작 활성화, T2 개방
            case "dialogue_13":
                state = S8S2State.Free;
                PersistPuzzleState();
                HandOverToPlayerCamera();
                LockPlayer(false);
                if (_t2Trigger != null)
                    _t2Trigger.SetActive(true);
                break;
        }
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    // ---------- 1~2번 ----------
    private IEnumerator Co_Intro()
    {
        StartProjectorLoop();

        // CutsceneManager는 자기 Start()에서 화면을 검게 덮는다.
        // 컴포넌트 간 Start 순서가 보장되지 않으므로 한 프레임 넘긴 뒤에 걷어낸다.
        yield return null;

        // 1번: 암전 상태에서 개봉 전 봉투를 먼저 올려두고 밝힌다
        ShowIllustrationImmediate(_letterClosedSprite);
        yield return Co_FadeFromBlack(_introFadeInDuration);

        if (_letterClosedHoldDuration > 0f)
            yield return new WaitForSeconds(_letterClosedHoldDuration);

        // 2번: 개봉된 봉투로 교체한 뒤 하루의 짧은 대사
        yield return Co_CrossfadeIllustration(_letterOpenedSprite);

        state = S8S2State.Letter;
        PersistPuzzleState();

        dialogueManager.StartDialogue("dialogue_1");
    }

    // ---------- 3번 ----------
    private IEnumerator Co_LowerTintThenLetter()
    {
        yield return Co_FadeOutIllustration();

        // 색조 하강은 편지 본문과 겹쳐서 진행한다. 다 내려갈 때까지 기다리면 대사가 늦는다.
        StartCoroutine(Co_FadeLight(_lightStageLetter));

        dialogueManager.StartDialogue("dialogue_2");
    }

    // ---------- 4번 / 6번 ----------
    private IEnumerator Co_ShowIllustration(Sprite sprite, string nextDialogueId)
    {
        yield return Co_CrossfadeIllustration(sprite);

        dialogueManager.StartDialogue(nextDialogueId);
    }

    // ---------- 5번 ----------
    private IEnumerator Co_ShowPistol()
    {
        yield return Co_CrossfadeIllustration(_pistolSprite);

        if (_recordStartSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(_recordStartSfx);

        dialogueManager.StartDialogue("dialogue_4");
    }

    // ---------- 6~9번 ----------
    private IEnumerator Co_GunshotToKei()
    {
        // 6번: 총성과 흰색 플래시는 같은 프레임에
        if (_gunshotSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(_gunshotSfx);

        StartCoroutine(Co_Flash());
        yield return Co_Shake();

        // 7번: 석상을 잔해로 교체하고 일러스트를 지운 뒤 즉시 암전
        SetStatueDestroyed(true);

        if (_blackoutAfterShot > 0f)
            yield return new WaitForSeconds(_blackoutAfterShot);

        yield return Co_FadeToBlack(0.05f);

        HideIllustration(_illustrationA);
        HideIllustration(_illustrationB);
        SetFlashAlpha(0f);

        // 8번: 총성 직후 다른 소리를 끊고 암전 상태에서 이명만 남긴다
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoopSFX();
            SoundManager.Instance.StopBGM();
        }

        PlayTinnitus();
        yield return new WaitForSeconds(_tinnitusDuration);
        StopTinnitus();

        // 9번: 하루를 H, 케이를 K에 배치하고 색조를 마지막 단계로
        PlaceHaruAtHome();
        PlaceKeiAtSpawn();
        ApplyLightStageImmediate(_lightStageFinal);
        HandOverToKeiCamera();

        yield return Co_FadeFromBlack(_introFadeInDuration);

        // 10번
        dialogueManager.StartDialogue("dialogue_6");
    }

    // ---------- 11~12번 ----------
    private IEnumerator Co_CurtainAppears()
    {
        // 케이가 커튼 쪽을 돌아본다
        TileActorMover.SetFacing(_keiAnimator, _keiAnimParams, _keiFacingCurtain);

        yield return Co_FadeCurtainIn();

        // 커튼이 완전히 보이는 순간 미지의 BGM
        if (_redCurtainBgm != null && SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(_redCurtainBgm);

        // 케이를 커튼 앞까지 자동 이동
        if (_kei != null)
            yield return TileActorMover.MovePath(
                _kei.transform,
                _keiAnimator,
                _keiAnimParams,
                _keiCurtainPath,
                _keiMoveSpeed
            );

        TileActorMover.SetFacing(_keiAnimator, _keiAnimParams, _keiFacingCurtain);

        dialogueManager.StartDialogue("dialogue_10");
    }

    // ---------- 12~13번 ----------
    private IEnumerator Co_KeiExits()
    {
        // 커튼으로 들어간다
        if (_kei != null)
            _kei.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // 13번: 잠시 암전시킨 뒤 루나의 음성
        yield return Co_FadeToBlack(0.8f);

        if (_blackoutBeforeLunaVoice > 0f)
            yield return new WaitForSeconds(_blackoutBeforeLunaVoice);

        dialogueManager.StartDialogue("dialogue_12");
    }

    // ---------- 13번 ----------
    private IEnumerator Co_LightsUpThenLastLine()
    {
        yield return Co_FadeFromBlack(_introFadeInDuration);

        dialogueManager.StartDialogue("dialogue_13");
    }

    // ---------- 14번 ----------
    /// <summary>T2 접촉 트리거에서 호출</summary>
    public void OnPlayerEnteredT2()
    {
        if (state == S8S2State.Finished)
            return;

        state = S8S2State.Finished;
        PersistPuzzleState();

        if (_t2Trigger != null)
            _t2Trigger.SetActive(false);

        LockPlayer(true);

        CutsceneManager.Instance.FadeToBlack(
            () =>
            {
                if (!string.IsNullOrEmpty(_nextSceneName))
                    SceneManager.LoadScene(_nextSceneName);
            },
            _fadeOutDuration
        );
    }

    // ---------- 배치 ----------
    private void PlaceHaruAtHome()
    {
        if (_playerMove == null)
            return;

        if (_haruHomePoint != null)
            _playerMove.transform.position = _haruHomePoint.position;

        _playerMove.SetFacing(_haruFacingKei);
    }

    private void PlaceKeiAtSpawn()
    {
        if (_kei == null)
            return;

        if (_keiSpawnPoint != null)
            _kei.transform.position = _keiSpawnPoint.position;

        _kei.SetActive(true);
        TileActorMover.SetWalking(_keiAnimator, _keiAnimParams, false, _keiMoveSpeed);
        TileActorMover.SetFacing(_keiAnimator, _keiAnimParams, Vector2.down);
    }

    private void SetStatueDestroyed(bool destroyed)
    {
        if (_statue != null)
            _statue.SetActive(!destroyed);

        if (_statueRubble != null)
            _statueRubble.SetActive(destroyed);
    }

    // ---------- 카메라 ----------
    private void HandOverToKeiCamera()
    {
        // 먼저 켠 뒤에 끄지 않으면 카메라가 하나도 없는 프레임이 생긴다
        if (_keiCamera != null)
            _keiCamera.gameObject.SetActive(true);

        if (_playerCamera != null)
            _playerCamera.gameObject.SetActive(false);
    }

    private void HandOverToPlayerCamera()
    {
        if (_playerCamera != null)
            _playerCamera.gameObject.SetActive(true);

        if (_keiCamera != null)
            _keiCamera.gameObject.SetActive(false);
    }

    // ---------- 일러스트 ----------
    private void ShowIllustrationImmediate(Sprite sprite)
    {
        if (_frontIllustration == null || sprite == null)
            return;

        _frontIllustration.sprite = sprite;
        SetImageAlpha(_frontIllustration, 1f);
        _frontIllustration.gameObject.SetActive(true);
    }

    /// <summary>
    /// 뒤 레이어에 새 일러스트를 올리고 알파를 교차시킨다.
    /// 앞 레이어가 비어 있으면 그냥 페이드 인이 된다.
    /// </summary>
    private IEnumerator Co_CrossfadeIllustration(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[S8S2] 일러스트 스프라이트가 비어 있어 교체를 건너뛴다.", this);
            yield break;
        }

        if (_backIllustration == null)
        {
            ShowIllustrationImmediate(sprite);
            yield break;
        }

        _backIllustration.sprite = sprite;
        SetImageAlpha(_backIllustration, 0f);
        _backIllustration.gameObject.SetActive(true);

        float fromFront = _frontIllustration != null ? _frontIllustration.color.a : 0f;
        float elapsed = 0f;

        while (elapsed < _illustrationFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _illustrationFadeDuration);

            SetImageAlpha(_backIllustration, t);
            if (_frontIllustration != null)
                SetImageAlpha(_frontIllustration, Mathf.Lerp(fromFront, 0f, t));

            yield return null;
        }

        SetImageAlpha(_backIllustration, 1f);
        HideIllustration(_frontIllustration);

        // 앞뒤를 뒤집어서 다음 크로스페이드에 대비한다
        Image swap = _frontIllustration;
        _frontIllustration = _backIllustration;
        _backIllustration = swap;
    }

    private IEnumerator Co_FadeOutIllustration()
    {
        if (_frontIllustration == null || !_frontIllustration.gameObject.activeSelf)
            yield break;

        float from = _frontIllustration.color.a;
        float elapsed = 0f;

        while (elapsed < _illustrationFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _illustrationFadeDuration);
            SetImageAlpha(_frontIllustration, Mathf.Lerp(from, 0f, t));
            yield return null;
        }

        HideIllustration(_frontIllustration);
    }

    private void HideIllustration(Image image)
    {
        if (image == null)
            return;

        SetImageAlpha(image, 0f);
        image.gameObject.SetActive(false);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        image.color = new Color(c.r, c.g, c.b, alpha);
    }

    // ---------- 플래시 / 흔들림 ----------
    private IEnumerator Co_Flash()
    {
        if (_flashImage == null)
            yield break;

        _flashImage.gameObject.SetActive(true);
        SetImageAlpha(_flashImage, 1f);

        if (_flashHoldDuration > 0f)
            yield return new WaitForSeconds(_flashHoldDuration);

        float elapsed = 0f;
        while (elapsed < _flashFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            SetImageAlpha(_flashImage, 1f - Mathf.Clamp01(elapsed / _flashFadeOutDuration));
            yield return null;
        }

        SetFlashAlpha(0f);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (_flashImage == null)
            return;

        SetImageAlpha(_flashImage, alpha);
        _flashImage.gameObject.SetActive(alpha > 0f);
    }

    /// <summary>
    /// 총성 순간에는 전체화면 일러스트가 맵을 덮고 있으므로 일러스트 레이어를 흔든다.
    /// (Cinemachine 카메라를 흔들면 vcam이 매 프레임 위치를 되돌려서 티가 나지 않는다)
    /// </summary>
    private IEnumerator Co_Shake()
    {
        if (_illustrationRoot == null || _shakeDuration <= 0f)
        {
            yield return new WaitForSeconds(_shakeDuration);
            yield break;
        }

        Vector2 origin = _illustrationRoot.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;

            // 뒤로 갈수록 잦아든다
            float falloff = 1f - Mathf.Clamp01(elapsed / _shakeDuration);
            Vector2 offset = Random.insideUnitCircle * (_shakeMagnitude * falloff);
            _illustrationRoot.anchoredPosition = origin + offset;

            yield return null;
        }

        _illustrationRoot.anchoredPosition = origin;
    }

    // ---------- 커튼 ----------
    private void SetCurtainAlpha(float alpha)
    {
        if (_curtain == null)
            return;

        Color c = _curtain.color;
        _curtain.color = new Color(c.r, c.g, c.b, alpha);
    }

    private IEnumerator Co_FadeCurtainIn()
    {
        if (_curtain == null)
            yield break;

        float from = _curtain.color.a;
        float elapsed = 0f;

        while (elapsed < _curtainFadeInDuration)
        {
            elapsed += Time.deltaTime;
            SetCurtainAlpha(Mathf.Lerp(from, 1f, Mathf.Clamp01(elapsed / _curtainFadeInDuration)));
            yield return null;
        }

        SetCurtainAlpha(1f);
    }

    // ---------- 페이드 ----------
    private IEnumerator Co_FadeToBlack(float duration)
    {
        bool done = false;
        CutsceneManager.Instance.FadeToBlack(() => done = true, duration);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator Co_FadeFromBlack(float duration)
    {
        bool done = false;
        CutsceneManager.Instance.FadeFromBlack(() => done = true, duration);
        yield return new WaitUntil(() => done);
    }

    // ---------- 사운드 ----------
    private void StartProjectorLoop()
    {
        if (_projectorLoopClip == null)
        {
            Debug.LogWarning("[S8S2] 영사기 작동음 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayLoopSFX(_projectorLoopClip, _projectorVolume);
    }

    private void PlayTinnitus()
    {
        if (_tinnitusSfx == null)
        {
            Debug.LogWarning("[S8S2] 이명 SFX 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayLoopSFX(_tinnitusSfx, _tinnitusVolume);
    }

    private void StopTinnitus()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopLoopSFX(1f);
    }

    // ---------- 색조 ----------
    private void ApplyLightStageImmediate(LightStage stage)
    {
        if (stage == null)
            return;

        if (_gardenLight != null)
        {
            _gardenLight.intensity = stage.intensity;
            _gardenLight.color = stage.color;
        }

        if (_colorAdjustments != null)
            _colorAdjustments.saturation.value = stage.saturation;
    }

    private IEnumerator Co_FadeLight(LightStage to)
    {
        if (to == null || (_gardenLight == null && _colorAdjustments == null))
            yield break;

        float fromIntensity = _gardenLight != null ? _gardenLight.intensity : 0f;
        Color fromColor = _gardenLight != null ? _gardenLight.color : Color.white;
        float fromSaturation = _colorAdjustments != null ? _colorAdjustments.saturation.value : 0f;

        float elapsed = 0f;

        while (elapsed < _lightFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _lightFadeDuration);

            if (_gardenLight != null)
            {
                _gardenLight.intensity = Mathf.Lerp(fromIntensity, to.intensity, t);
                _gardenLight.color = Color.Lerp(fromColor, to.color, t);
            }

            if (_colorAdjustments != null)
                _colorAdjustments.saturation.value = Mathf.Lerp(fromSaturation, to.saturation, t);

            yield return null;
        }

        ApplyLightStageImmediate(to);
    }

    // ---------- 조작 ----------
    private void LockPlayer(bool locked)
    {
        if (_playerMove == null)
            return;

        _playerMove.enabled = !locked;

        if (_playerMove.animator != null)
        {
            _playerMove.animator.SetBool("Walking", false);
            _playerMove.animator.SetBool("Pushing", false);
        }
    }
}
