using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum S10S2State
{
    None,         // 씬 시작 직후
    Abstract,     // 5-1 추상공간 대화
    Beach,        // 5-2 해변 대화 (디졸브 이후)
    Illustration, // 일러스트 1 → 2 → 3
    Ending,       // 빈 해변 → 엔딩 크레딧
}

/// <summary>
/// 시퀀스10 씬2 "마지막 상영 (엔딩 1)" 컨트롤러.
/// 추상공간(1-1 텔레비전 방)에서 나란히 앉아 시작한 대화가 &lt;여름 바람&gt;의 해변으로 디졸브되고,
/// 루나가 바다로 걸어 들어간 뒤 일러스트 3장 → 빈 해변 → 엔딩 크레딧으로 끝난다.
///
/// 디졸브는 해변 맵을 추상공간과 같은 화면 구도로 겹쳐 두고 알파만 올린다.
/// 카메라와 인물은 움직이지 않으므로 두 사람의 화면상 위치·자세·구도가 그대로 유지된다. (기획서 4-2, 8)
/// 대사 넘김 외의 조작은 없다.
/// </summary>
public class Sequence10Scene2DialogueController : DialogueController<S10S2State>
{
    [Header("하루")]
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [Tooltip("쪼그려 앉은 뒷모습.")]
    [SerializeField]
    private Sprite _haruSitSprite;

    [Header("루나")]
    [SerializeField]
    private Transform _luna;

    [SerializeField]
    private Animator _lunaAnimator;

    [SerializeField]
    private ActorAnimParams _lunaAnimParams = new ActorAnimParams
    {
        dirX = "DirX",
        dirY = "Diry",
        walk = "IsWalking",
        animSpeed = "",
    };

    [Tooltip("쪼그려 앉은 뒷모습.")]
    [SerializeField]
    private Sprite _lunaSitSprite;

    [Tooltip("5-2: 루나가 일어나 걸어가는 바다 쪽 지점. 카메라는 따라가지 않는다.")]
    [SerializeField]
    private Transform _lunaSeaPoint;

    [Tooltip("루나 걷는 속도. 1 = 플레이어 걷기와 동일")]
    [SerializeField]
    private float _lunaWalkSpeed = 0.45f;

    [Tooltip("루나가 걷기 시작한 뒤 다음 대사까지")]
    [SerializeField]
    private float _lunaWalkLeadTime = 1.2f;

    [Header("공간")]
    [Tooltip("추상공간(텔레비전 방)과 텔레비전. 디졸브가 끝나면 끈다.")]
    [SerializeField]
    private GameObject[] _abstractSpace;

    [Tooltip("해변 맵. 추상공간과 같은 화면 구도로 겹쳐 두고 알파 0에서 시작한다.")]
    [SerializeField]
    private GameObject _beach;

    [Tooltip("추상공간 → 해변 디졸브 (기획: 2~3초)")]
    [SerializeField]
    private float _dissolveDuration = 2.5f;

    [Header("카메라")]
    [Tooltip("고정 구도. 추상공간·해변·빈 해변 모두 같은 구도다.")]
    [SerializeField]
    private CinemachineVirtualCamera _vcam;

    [Header("일러스트 (하루의 같은 시점)")]
    [SerializeField]
    private Image _illustrationImage;

    [SerializeField]
    private TMP_Text _illustrationPlaceholder;

    [Tooltip("일러스트 1·2·3. 비어 있으면 아래 설명으로 대신 띄운다.")]
    [SerializeField]
    private Sprite[] _illustrations = new Sprite[3];

    [SerializeField]
    private string[] _illustrationLabels =
    {
        "1. 하루의 시점. 루나의 뒷모습과 발을 덮는 파도",
        "2. 같은 시점에서 루나가 더 멀리, 더 깊은 바다로 들어간 모습",
        "3. 같은 시점에서 루나가 가장 멀리 있다. 파도가 몸 일부를 가리고 초점이 흐려져 있다",
    };

    [Tooltip("슬라이드 넘기듯 바뀌는 시간. 0이면 하드컷")]
    [SerializeField]
    private float _illustrationSwapDuration = 0.3f;

    [Tooltip("일러스트 3의 마지막 대화 UI가 사라진 뒤 일러스트를 보여주는 시간")]
    [SerializeField]
    private float _illustrationHoldAfterUI = 1.5f;

    [Header("화면 전환 타이밍")]
    [SerializeField]
    private float _fadeInDuration = 2f;

    [Tooltip("텔레비전 BGM이 꺼진 뒤 대화를 멈추는 시간 (기획: 2~3초)")]
    [SerializeField]
    private float _tvSilence = 2.5f;

    [Tooltip("해변 대사 뒤 루나가 일어나기 전 정적")]
    [SerializeField]
    private float _beatPause = 1f;

    [SerializeField]
    private float _illustrationFadeOut = 3f;

    [Tooltip("일러스트 페이드 아웃 뒤 짧은 암전")]
    [SerializeField]
    private float _blackHold = 1f;

    [SerializeField]
    private float _emptyBeachFadeIn = 3f;

    [Tooltip("빈 해변을 보여주는 시간 (기획: 약 8초)")]
    [SerializeField]
    private float _emptyBeachHold = 8f;

    [SerializeField]
    private float _finalFadeOut = 2f;

    [Header("사운드")]
    [Tooltip("추상공간 진입부터 루나의 '…'까지")]
    [SerializeField]
    private AudioClip _tvBgm;

    [Tooltip("추상공간 대화 중간부터 페이드 인, 엔딩 크레딧 직전까지 유지")]
    [SerializeField]
    private AudioClip _wavesBgm;

    [SerializeField]
    private float _wavesFadeIn = 3f;

    [Tooltip("일러스트 1부터 일러스트 3의 마지막 대화 UI가 사라질 때까지")]
    [SerializeField]
    private AudioClip _projectorBgm;

    [Range(0f, 1f)]
    [SerializeField]
    private float _projectorVolume = 0.6f;

    [SerializeField]
    private float _projectorFadeOut = 2f;

    [Tooltip("빈 해변이 암전된 뒤 파도 소리를 유지하는 시간")]
    [SerializeField]
    private float _wavesHoldAfterBlack = 1.5f;

    [SerializeField]
    private float _wavesFadeOut = 2f;

    [Header("씬 전환")]
    [Tooltip("엔딩 크레딧")]
    [SerializeField]
    private string _nextSceneName = "EndingScene";

    private bool _sequenceRunning;
    private int _illustrationIndex = -1;

    /// <summary>대사 사이 연출(디졸브/이동/일러스트 전환/엔딩)이 진행 중인지</summary>
    public bool IsSequenceRunning => _sequenceRunning;

    // ================================================================ 상태 적용

    protected override void ApplyWorldByState()
    {
        LockPlayer(true);
        HideIllustration();

        if (state == S10S2State.Ending)
        {
            StartCoroutine(Co_LoadNextFrame());
            return;
        }

        // 엔딩 이전에 저장됐으면 처음부터 다시 본다 (대사만 있는 씬이라 짧다)
        state = S10S2State.None;

        SetAbstractSpace(true);
        SetBeachAlpha(0f);
        if (_beach != null)
            _beach.SetActive(true);

        if (_luna != null)
            _luna.gameObject.SetActive(true);

        SitDown();

        StartCoroutine(Co_Intro());
    }

    private IEnumerator Co_LoadNextFrame()
    {
        yield return null;
        LoadNext();
    }

    // ================================================================ 5-1 추상공간

    private IEnumerator Co_Intro()
    {
        _sequenceRunning = true;

        // CutsceneManager.Start()가 화면을 검게 덮으므로 한 프레임 넘긴다
        yield return null;

        // 텔레비전 BGM과 함께 페이드 인
        PlayBgm(_tvBgm, 1f);
        yield return Co_FadeFromBlack(_fadeInDuration);

        state = S10S2State.Abstract;
        PersistPuzzleState();

        _sequenceRunning = false;
        dialogueManager.StartDialogue("a_1");
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        LockPlayer(true);

        switch (dialogueId)
        {
            // 루나의 "…" 뒤 텔레비전 BGM이 꺼지고 2~3초 멈췄다가 이어간다
            case "a_8":
                StartCoroutine(Co_TvOffSilence());
                break;

            // 다시 시작한 대화 중간부터 파도 소리가 페이드 인
            case "b_1":
                StartWaves();
                break;

            // "그런데 네 영화를 찍을 때는……" 뒤 자세·구도를 유지한 채 해변으로 디졸브
            case "b_2":
                StartCoroutine(Co_DissolveToBeach());
                break;

            // 대화가 잠시 끊기고 루나가 일어나 바다 쪽으로 걷기 시작
            case "c_1":
                StartCoroutine(Co_LunaWalksToSea());
                break;

            // 일러스트 1 + 영사기 BGM
            case "d_1":
                StartCoroutine(Co_ShowIllustration(0, "il_1", startProjector: true));
                break;

            case "il_1":
                StartCoroutine(Co_ShowIllustration(1, "il_2", startProjector: false));
                break;

            case "il_2":
                StartCoroutine(Co_ShowIllustration(2, "il_3", startProjector: false));
                break;

            // 마지막 키 입력으로 대화 UI가 사라지면 잠시 일러스트를 보여준 뒤 엔딩
            case "il_3":
                StartCoroutine(Co_Ending());
                break;
        }
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    private IEnumerator Co_TvOffSilence()
    {
        _sequenceRunning = true;

        StopBgm();

        if (_tvSilence > 0f)
            yield return new WaitForSeconds(_tvSilence);

        _sequenceRunning = false;
        dialogueManager.StartDialogue("b_1");
    }

    private IEnumerator Co_DissolveToBeach()
    {
        _sequenceRunning = true;

        // 추상공간 위로 모래사장과 바다가 겹쳐 나타나다가 해변 풍경만 남는다
        float elapsed = 0f;
        while (elapsed < _dissolveDuration)
        {
            elapsed += Time.deltaTime;
            SetBeachAlpha(Mathf.Clamp01(elapsed / _dissolveDuration));
            yield return null;
        }

        SetBeachAlpha(1f);
        SetAbstractSpace(false);

        state = S10S2State.Beach;
        PersistPuzzleState();

        _sequenceRunning = false;
        dialogueManager.StartDialogue("c_1");
    }

    // ================================================================ 5-2 해변

    private IEnumerator Co_LunaWalksToSea()
    {
        _sequenceRunning = true;

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause);

        // 루나만 일어나 바다 쪽으로 걷는다. 카메라는 따라가지 않는다.
        StandUpLuna();

        if (_luna != null && _lunaSeaPoint != null)
            StartCoroutine(TileActorMover.MoveTo(_luna, _lunaAnimator, _lunaAnimParams, _lunaSeaPoint.position, _lunaWalkSpeed));

        if (_lunaWalkLeadTime > 0f)
            yield return new WaitForSeconds(_lunaWalkLeadTime);

        _sequenceRunning = false;
        dialogueManager.StartDialogue("d_1");
    }

    private IEnumerator Co_ShowIllustration(int index, string nextDialogue, bool startProjector)
    {
        _sequenceRunning = true;

        if (index == 0)
        {
            state = S10S2State.Illustration;
            PersistPuzzleState();
        }

        if (startProjector)
            PlayProjector();

        yield return Co_SwapIllustration(index);

        _sequenceRunning = false;
        dialogueManager.StartDialogue(nextDialogue);
    }

    private IEnumerator Co_Ending()
    {
        _sequenceRunning = true;
        state = S10S2State.Ending;
        PersistPuzzleState();

        // 대화 UI가 사라지면 영사기 BGM을 줄여 파도 소리만 남긴다
        StopProjector();

        if (_illustrationHoldAfterUI > 0f)
            yield return new WaitForSeconds(_illustrationHoldAfterUI);

        yield return Co_FadeToBlack(_illustrationFadeOut);

        // 암전 아래에서 두 사람을 지우고 일러스트 이전의 해변 구도로 돌아간다
        HideIllustration();
        SetCharactersVisible(false);
        SetBeachAlpha(1f);

        if (_blackHold > 0f)
            yield return new WaitForSeconds(_blackHold);

        yield return Co_FadeFromBlack(_emptyBeachFadeIn);

        // 빈 해변 약 8초
        if (_emptyBeachHold > 0f)
            yield return new WaitForSeconds(_emptyBeachHold);

        yield return Co_FadeToBlack(_finalFadeOut);

        // 암전 뒤에도 파도 소리를 잠시 유지하다가 줄인다
        if (_wavesHoldAfterBlack > 0f)
            yield return new WaitForSeconds(_wavesHoldAfterBlack);

        FadeBgmOut(_wavesFadeOut);
        if (_wavesFadeOut > 0f)
            yield return new WaitForSeconds(_wavesFadeOut);

        StopBgm();
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMSourceVolume(1f);

        LoadNext();
    }

    private void LoadNext()
    {
        _sequenceRunning = false;

        if (string.IsNullOrEmpty(_nextSceneName))
            return;

        if (Application.CanStreamedLevelBeLoaded(_nextSceneName))
            SceneManager.LoadScene(_nextSceneName);
        else
            Debug.LogWarning($"[S10S2] 다음 씬 '{_nextSceneName}'이 빌드 설정에 없어 암전 상태로 멈춘다.", this);
    }

    // ================================================================ 인물

    /// <summary>두 사람을 나란히 쪼그려 앉힌다. 같은 방향(텔레비전/바다)을 바라보는 뒷모습.</summary>
    private void SitDown()
    {
        if (_playerMove != null)
        {
            _playerMove.SetFacing(Vector2.up);
            SetPose(_playerMove.animator, _playerMove.GetComponent<SpriteRenderer>(), _haruSitSprite);
        }

        if (_luna != null)
        {
            TileActorMover.SetFacing(_lunaAnimator, _lunaAnimParams, Vector2.up);
            SetPose(_lunaAnimator, _luna.GetComponentInChildren<SpriteRenderer>(), _lunaSitSprite);
        }
    }

    private static void SetPose(Animator animator, SpriteRenderer sr, Sprite pose)
    {
        if (pose == null || sr == null)
            return;

        // 고정 자세라 애니메이터가 스프라이트를 덮어쓰지 않게 끈다
        if (animator != null)
            animator.enabled = false;

        sr.sprite = pose;
    }

    private void StandUpLuna()
    {
        if (_lunaAnimator != null)
        {
            _lunaAnimator.enabled = true;
            TileActorMover.SetFacing(_lunaAnimator, _lunaAnimParams, Vector2.up);
        }
    }

    private void SetCharactersVisible(bool visible)
    {
        if (_playerMove != null)
        {
            var sr = _playerMove.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = visible;
        }

        if (_luna != null)
            _luna.gameObject.SetActive(visible);
    }

    // ================================================================ 공간

    private void SetAbstractSpace(bool on)
    {
        if (_abstractSpace == null)
            return;

        foreach (GameObject go in _abstractSpace)
        {
            if (go != null)
                go.SetActive(on);
        }
    }

    private void SetBeachAlpha(float a)
    {
        if (_beach == null)
            return;

        foreach (SpriteRenderer sr in _beach.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, a);
        }
    }

    // ================================================================ 일러스트

    private IEnumerator Co_SwapIllustration(int index)
    {
        if (_illustrationImage == null)
            yield break;

        Sprite sprite = _illustrations != null && index < _illustrations.Length ? _illustrations[index] : null;
        string label = _illustrationLabels != null && index < _illustrationLabels.Length ? _illustrationLabels[index] : "";

        bool wasShown = _illustrationImage.gameObject.activeSelf;
        _illustrationIndex = index;

        // 처음 나올 때는 서서히, 이후에는 슬라이드를 넘기듯 짧게 어두워졌다가 바뀐다
        if (wasShown && _illustrationSwapDuration > 0f)
            yield return Co_IllustrationAlpha(1f, 0f, _illustrationSwapDuration * 0.5f);

        _illustrationImage.gameObject.SetActive(true);

        if (sprite != null)
        {
            _illustrationImage.sprite = sprite;
            _illustrationImage.color = new Color(1f, 1f, 1f, _illustrationImage.color.a);
        }
        else
        {
            _illustrationImage.sprite = null;
            _illustrationImage.color = new Color(0.12f, 0.14f, 0.16f, _illustrationImage.color.a);
        }

        if (_illustrationPlaceholder != null)
        {
            _illustrationPlaceholder.gameObject.SetActive(sprite == null);
            _illustrationPlaceholder.text = "[임시] 일러스트 " + label;
        }

        float duration = wasShown ? _illustrationSwapDuration * 0.5f : Mathf.Max(_illustrationSwapDuration, 0.5f);
        if (duration > 0f)
            yield return Co_IllustrationAlpha(0f, 1f, duration);
        else
            SetIllustrationAlpha(1f);
    }

    private IEnumerator Co_IllustrationAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetIllustrationAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetIllustrationAlpha(to);
    }

    private void SetIllustrationAlpha(float a)
    {
        if (_illustrationImage != null)
        {
            Color c = _illustrationImage.color;
            _illustrationImage.color = new Color(c.r, c.g, c.b, a);
        }

        if (_illustrationPlaceholder != null)
        {
            Color c = _illustrationPlaceholder.color;
            _illustrationPlaceholder.color = new Color(c.r, c.g, c.b, a);
        }
    }

    private void HideIllustration()
    {
        _illustrationIndex = -1;

        if (_illustrationImage != null)
            _illustrationImage.gameObject.SetActive(false);

        if (_illustrationPlaceholder != null)
            _illustrationPlaceholder.gameObject.SetActive(false);
    }

    /// <summary>지금 보이는 일러스트 번호 (0~2, 없으면 -1)</summary>
    public int IllustrationIndex => _illustrationIndex;

    // ================================================================ 사운드

    private void PlayBgm(AudioClip clip, float volume)
    {
        if (SoundManager.Instance == null)
            return;

        if (clip == null)
        {
            Debug.LogWarning("[S10S2] BGM 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        SoundManager.Instance.SetBGMSourceVolume(volume);
        SoundManager.Instance.PlayBGM(clip);
    }

    private static void StopBgm()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    private static void FadeBgmOut(float duration)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMSourceVolume(0f, duration);
    }

    /// <summary>텔레비전 BGM이 꺼진 채널에 파도 소리를 0에서부터 올린다</summary>
    private void StartWaves()
    {
        if (_wavesBgm == null || SoundManager.Instance == null)
        {
            Debug.LogWarning("[S10S2] 파도 소리 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        PlayBgm(_wavesBgm, 0f);
        SoundManager.Instance.SetBGMSourceVolume(1f, _wavesFadeIn);
    }

    /// <summary>파도 소리(BGM 채널)와 겹쳐야 해서 영사기는 루프 SFX 채널로 튼다</summary>
    private void PlayProjector()
    {
        if (_projectorBgm == null)
        {
            Debug.LogWarning("[S10S2] 영사기 BGM 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayLoopSFX(_projectorBgm, _projectorVolume, 1f);
    }

    private void StopProjector()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopLoopSFX(_projectorFadeOut);
    }

    // ================================================================ 기타

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

    private void LockPlayer(bool locked)
    {
        if (_playerMove == null)
            return;

        _playerMove.enabled = !locked;
    }
}
