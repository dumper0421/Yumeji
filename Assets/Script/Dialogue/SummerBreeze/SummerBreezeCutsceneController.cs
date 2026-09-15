using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SummerBreezeState
{
    None,
    S1,
    S2,
    S3,
    S4,
    Finished,
}

/// <summary>
/// 단편 영화 &lt;여름 바람&gt; 인게임 컷씬.
///
/// [6-1]의 흰 화면에서 이어받아 S#1~S#4를 하나의 맵에서 연속 재생하고,
/// 마지막 셔터와 함께 [6-2] 상영실로 넘긴다.
///
/// 플레이어는 대사 넘김만 할 수 있다. 이동/상호작용/촬영 입력은 전부 잠근다.
/// 같은 장소를 시간대와 날씨(M1 맑음 / M2 비 / M3 저녁)만 바꿔 반복하는데,
/// 맵을 3벌 만드는 대신 맵 한 벌 위에 색 오버레이와 비 파티클을 얹어서 표현한다.
///
/// 전체 흐름이 한 줄로 이어지는 연출이라 대사 블록마다 콜백으로 쪼개지 않고
/// 마스터 코루틴 하나가 순서대로 진행한다.
/// 대사 블록은 Co_Play(시작ID)로 시작하고 DialogueManager.isRunning이
/// 꺼질 때까지(= nextId 체인이 전부 끝날 때까지) 기다린다.
/// </summary>
public class SummerBreezeCutsceneController : DialogueController<SummerBreezeState>
{
    /// <summary>
    /// 맵 한 벌로 날씨·시간대를 표현하기 위한 화면 효과 한 단계.
    /// 맵을 3벌 만드는 대신 색 오버레이의 색·농도와 비 파티클만 바꾼다.
    /// </summary>
    [System.Serializable]
    public class MapMood
    {
        [Tooltip("화면 전체에 덮을 색")]
        public Color tint = Color.white;

        [Range(0f, 1f)]
        [Tooltip("덮는 정도. 0이면 아무 효과 없음")]
        public float strength = 0f;

        [Tooltip("비 파티클을 켤지")]
        public bool rain = false;
    }

    // ---------------------------------------------------------------- 인물

    [Header("인물 - 소년(하루)")]
    [SerializeField]
    private PlayerMove_Test_Lerp _haru;

    [Tooltip("1 = 플레이어 걷기와 동일(타일당 0.2초). 낮출수록 느려진다.")]
    [SerializeField]
    private float _haruMoveSpeed = 0.8f;

    [SerializeField]
    private ActorAnimParams _haruAnim = new ActorAnimParams();

    [Header("인물 - 소녀(루나)")]
    [SerializeField]
    private Transform _luna;

    [SerializeField]
    private Animator _lunaAnimator;

    [SerializeField]
    private float _lunaMoveSpeed = 0.8f;

    [Tooltip("루나 애니메이터는 DirX / Diry / IsWalking 이고 AnimSpeed가 없다.")]
    [SerializeField]
    private ActorAnimParams _lunaAnim = new ActorAnimParams
    {
        dirX = "DirX",
        dirY = "Diry",
        walk = "IsWalking",
        animSpeed = "",
    };

    // ---------------------------------------------------------------- 맵 상태

    [Header("맵 상태 - 맵 한 벌 + 화면 효과")]
    [Tooltip(
        "맵 위에 덮는 색 오버레이. 월드 위, 대사창 아래에 깔리는 캔버스의 Image를 넣는다. "
            + "맵을 3벌 만드는 대신 이 색과 비 효과만 바꿔서 날씨·시간대를 표현한다."
    )]
    [SerializeField]
    private Image _moodOverlay;

    [Tooltip("비 파티클. 처음에는 꺼져 있어야 한다.")]
    [SerializeField]
    private GameObject _rain;

    [Tooltip("M1 맑음. S#1, S#2")]
    [SerializeField]
    private MapMood _moodClear = new MapMood { tint = new Color(1f, 0.98f, 0.9f), strength = 0f };

    [Tooltip("M2 비. S#3")]
    [SerializeField]
    private MapMood _moodRain = new MapMood
    {
        tint = new Color(0.35f, 0.42f, 0.55f),
        strength = 0.45f,
        rain = true,
    };

    [Tooltip("M3 저녁. S#4 앞부분")]
    [SerializeField]
    private MapMood _moodEvening = new MapMood
    {
        tint = new Color(1f, 0.55f, 0.3f),
        strength = 0.35f,
    };

    [Tooltip("S#4 밤. 저녁 위에 불꽃과 함께 얹는다. 별도 맵을 만들지 않는다.")]
    [SerializeField]
    private MapMood _moodNight = new MapMood
    {
        tint = new Color(0.12f, 0.14f, 0.3f),
        strength = 0.55f,
    };

    [Tooltip("화면 밖에서 상태가 바뀔 때는 0으로 두면 즉시 바뀐다.")]
    [SerializeField]
    private float _moodFadeDuration = 0f;

    [Tooltip(
        "비(M2) 상태에서 켜는 Color Adjustments 프로파일. Summer_Breeze_Rainy를 넣는다. "
            + "곱하기·색상 닷지는 Renderer2D의 ScreenBlendLayers 머티리얼(SummerWind_RainyBlend)에서 조절한다."
    )]
    [SerializeField]
    private VolumeProfile _rainVolumeProfile;

    // 프로파일로 런타임에 만드는 글로벌 볼륨. 비 보정 세기(0~1)를 곱하기·닷지와 같이 움직인다.
    private Volume _rainVolume;

    [Tooltip("S#4의 불꽃 애니메이션. 처음에는 꺼져 있어야 한다.")]
    [SerializeField]
    private GameObject _fireworks;

    [Tooltip("S#4의 여행 가방. 루나가 버스를 타고 떠날 때 함께 꺼진다.")]
    [SerializeField]
    private GameObject _travelBag;

    // ---------------------------------------------------------------- 카메라

    [Header("카메라")]
    [Tooltip("컷씬 카메라. 비워두면 씬에서 찾는다. 시작할 때 Follow를 카메라 리그로 바꾼다.")]
    [SerializeField]
    private CinemachineVirtualCamera _vcam;

    [Tooltip("Virtual Camera가 따라다닐 리그. 이 오브젝트를 옮겨서 구도를 잡는다.")]
    [SerializeField]
    private Transform _cameraRig;

    [Tooltip("S#1 시작 구도. 화면 아래쪽 정류장.")]
    [SerializeField]
    private Transform _camS1Start;

    [Tooltip("S#1 첫 만남 구도. 중앙 해변.")]
    [SerializeField]
    private Transform _camS1Beach;

    [Tooltip("S#2 시작 구도. 방파제와 섬이 함께 보이는 위치.")]
    [SerializeField]
    private Transform _camS2Start;

    [Tooltip("S#2 두 사람이 방파제 끝으로 이동했을 때 따라가는 구도.")]
    [SerializeField]
    private Transform _camS2End;

    [Tooltip("S#3 구도. 정류장과 빗속 바다가 함께 보이는 위치.")]
    [SerializeField]
    private Transform _camS3;

    [Tooltip("S#4 이별 구도. 정류장.")]
    [SerializeField]
    private Transform _camS4BusStop;

    [Tooltip("S#4 마지막 구도. 방파제의 소년과 옆 빈자리가 함께 보이는 위치.")]
    [SerializeField]
    private Transform _camS4Pier;

    // ---------------------------------------------------------------- S#1

    [Header("S#1 - 첫 만남 (M1 맑음)")]
    [Tooltip(
        "켜면 씬에 배치해둔 하루의 자리에서 그대로 시작한다(순간이동 없음). "
            + "이때 아래 S1 Haru Stand는 무시된다."
    )]
    [SerializeField]
    private bool _s1UseHaruScenePosition = true;

    [Tooltip("S1 Use Haru Scene Position을 끄면 여기로 하루를 옮겨놓고 시작한다.")]
    [SerializeField]
    private Transform _s1HaruStand;

    [Tooltip("소녀가 화면 왼쪽 밖에서 걸어 들어오기 시작하는 위치")]
    [SerializeField]
    private Transform _s1LunaEnter;

    [Tooltip("셔터 소리와 함께 소녀가 멈추는 위치. 소년의 촬영 방향을 가로지르는 지점.")]
    [SerializeField]
    private Transform _s1LunaShutter;

    [Tooltip("소녀가 오른쪽으로 이어서 걸어 화면 밖으로 나가는 경로")]
    [SerializeField]
    private Transform[] _s1LunaExitPath;

    [Tooltip("소녀 퇴장 후 소년이 그 방향을 바라본 채 멈추는 시간")]
    [SerializeField]
    private float _s1HoldAfterExit = 2f;

    // ---------------------------------------------------------------- S#2

    [Header("S#2 - 두 번째 만남 (M1 맑음)")]
    [Tooltip("방파제 맨 끝에서 2~3칸 떨어진 곳. 소년이 먼 섬을 찍는 위치.")]
    [SerializeField]
    private Transform _s2HaruStand;

    [SerializeField]
    private Transform _s2LunaEnter;

    [Tooltip("소년에게서 1~2칸 떨어진 곳. 소녀가 걸어와 멈추는 위치.")]
    [SerializeField]
    private Transform _s2LunaStop;

    [Tooltip("방파제 끝. 소녀가 2~3칸 이동해 서는 위치.")]
    [SerializeField]
    private Transform _s2LunaEnd;

    [Tooltip("소년이 한 칸 정도 거리를 두고 따라가 서는 위치.")]
    [SerializeField]
    private Transform _s2HaruEnd;

    [Tooltip("소녀가 소년의 촬영 방향 안으로 한 칸 들어가 서는 위치.")]
    [SerializeField]
    private Transform _s2LunaFrameIn;

    // ---------------------------------------------------------------- S#3

    [Header("S#3 - 세 번째 만남 (M2 비)")]
    [Tooltip("소년이 정류장 쪽으로 걸어오기 시작하는 위치")]
    [SerializeField]
    private Transform _s3HaruEnter;

    [Tooltip("소녀에게서 한 칸 떨어진 곳. 소년이 멈추는 위치.")]
    [SerializeField]
    private Transform _s3HaruStop;

    [Tooltip("바다를 향해 서 있는 소녀의 위치")]
    [SerializeField]
    private Transform _s3LunaStand;

    [Tooltip("5번: 소년이 소녀 쪽을 봤다가 다시 바다를 향하기 전 머무는 시간")]
    [SerializeField]
    private float _s3LookAtLunaHold = 1f;

    // ---------------------------------------------------------------- S#4

    [Header("S#4 - 약속한 날 (M3 저녁)")]
    [Tooltip("소년이 정류장 쪽으로 걸어오기 시작하는 위치")]
    [SerializeField]
    private Transform _s4HaruEnter;

    [Tooltip("소녀에게서 한 칸 떨어진 곳")]
    [SerializeField]
    private Transform _s4HaruStop;

    [Tooltip("정류장 의자 앞, 여행 가방 옆에 선 소녀의 위치")]
    [SerializeField]
    private Transform _s4LunaStand;

    [Tooltip("혼자 남은 소년이 앉아 있을 방파제 끝 자리")]
    [SerializeField]
    private Transform _s4HaruSeat;

    [Tooltip("소년이 바라볼 옆 빈자리. 방향 계산에만 쓴다.")]
    [SerializeField]
    private Transform _s4EmptySeat;

    [Tooltip("3번: 소년이 도로를 향한 채 멈추는 시간")]
    [SerializeField]
    private float _s4HoldAfterBus = 3f;

    [Tooltip("5번: 빈자리를 바라보는 시간")]
    [SerializeField]
    private float _s4LookAtEmptySeatHold = 1.5f;

    // ---------------------------------------------------------------- 사운드

    [Header("사운드")]
    [Tooltip("BGM_해변 소리. S#1, S#2, S#4")]
    [SerializeField]
    private AudioClip _bgmBeach;

    [Tooltip("BGM_비 그리고 해변 소리. S#3")]
    [SerializeField]
    private AudioClip _bgmRainBeach;

    [Tooltip("사진 셔터음. 기존 촬영 시 사용하던 것과 같은 클립을 넣을 것.")]
    [SerializeField]
    private AudioClip _sfxShutter;

    [SerializeField]
    private AudioClip _sfxBusDeparture;

    [SerializeField]
    private AudioClip _sfxFireworks;

    [Range(0f, 1f)]
    [SerializeField]
    private float _bgmVolume = 0.8f;

    [Range(0f, 1f)]
    [SerializeField]
    private float _sfxVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("불꽃놀이는 마지막까지 깔리므로 대사를 덮지 않게 낮게 잡는다.")]
    [SerializeField]
    private float _fireworksVolume = 0.6f;

    [Tooltip("장면이 바뀔 때 BGM을 바꾸는 데 쓰는 페이드 시간. 화면이 검은 동안 처리한다.")]
    [SerializeField]
    private float _bgmFadeDuration = 1f;

    // ---------------------------------------------------------------- 연출 타이밍

    [Header("연출 - 타이밍")]
    [Tooltip("[6-1]의 흰 화면에서 맵으로 넘어오는 시간")]
    [SerializeField]
    private float _openFadeDuration = 2f;

    [Tooltip("장면과 장면 사이 페이드 아웃 시간")]
    [SerializeField]
    private float _fadeOutDuration = 1.5f;

    [Tooltip("장면과 장면 사이 페이드 인 시간")]
    [SerializeField]
    private float _fadeInDuration = 1.5f;

    [Tooltip("검은 화면에서 다음 장면을 세팅하고 머무는 시간")]
    [SerializeField]
    private float _blackHoldDuration = 0.6f;

    [Tooltip("S#1: 정류장에서 해변까지 카메라가 올라가는 시간")]
    [SerializeField]
    private float _introPanDuration = 4f;

    [Tooltip("S#2: 두 사람을 따라 카메라가 방파제 끝으로 옮겨가는 시간")]
    [SerializeField]
    private float _s2CameraFollowDuration = 1.5f;

    [Tooltip("촬영 모션을 재생하고 기다리는 시간. 하루의 Shoot 애니메이션 길이가 약 0.83초다.")]
    [SerializeField]
    private float _shootHoldDuration = 0.9f;

    [Tooltip("대사 블록이 끝난 뒤 다음 연출로 넘어가기 전 간격")]
    [SerializeField]
    private float _beatPause = 0.4f;

    // ---------------------------------------------------------------- 진입/종료

    [Header("진입 / 종료")]
    [Tooltip("[6-1]이 영사기의 백색 화면이라 흰색에서 열린다. 끄면 검은색에서 연다.")]
    [SerializeField]
    private bool _openFromWhite = true;

    [Tooltip("[6-2] 상영실 씬 이름. 비워두면 페이드 아웃까지만 하고 멈춘다.")]
    [SerializeField]
    private string _nextSceneName = "";

    [Tooltip("컷씬 동안 꺼둘 오브젝트. 메뉴 캔버스처럼 입력을 받는 것들을 넣는다.")]
    [SerializeField]
    private GameObject[] _disableDuringCutscene;

    // ----------------------------------------------------------------

    private MonoBehaviour[] _haruInputScripts;
    private bool _cutsceneRunning;

    /// <summary>지금 컷씬을 몰고 있는 컨트롤러. 씬에 둘 이상 있으면 안 된다.</summary>
    private static SummerBreezeCutsceneController _active;

    /// <summary>
    /// 컷씬 감독은 씬에 하나만 있어야 한다.
    /// 둘이면 각자 코루틴을 돌리면서 루나를 동시에 움직여 도착 시점이 어긋나고,
    /// 늦은 쪽이 같은 대사 블록을 다시 시작해서 이미 넘긴 줄이 처음부터 또 나온다.
    /// </summary>
    protected override void Awake()
    {
        if (_active != null && _active != this)
        {
            Debug.LogError(
                $"[SummerBreeze] 컷씬 컨트롤러가 씬에 둘 이상이다. 대사가 두 번 나오는 원인이므로 "
                    + $"'{Path(transform)}' 쪽은 꺼둔다. 사용 중: '{Path(_active.transform)}'. "
                    + "안 쓰는 쪽의 컴포넌트를 지울 것.",
                this
            );

            enabled = false; // enabled가 false면 Start가 호출되지 않아 컷씬도 시작되지 않는다
            return;
        }

        _active = this;
        base.Awake();
    }

    protected override void OnDestroy()
    {
        if (_active == this)
        {
            _active = null;
            // 전역 셰이더 값이라 씬을 떠나도 남는다. 다른 씬에 비 보정이 따라가지 않게 끈다.
            ScreenBlendLayersFeature.Weight = 0f;
        }

        // 꺼진 중복 인스턴스는 base.Awake를 타지 않아 구독한 적이 없다
        if (enabled)
            base.OnDestroy();
    }

    private static string Path(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    /// <summary>
    /// DialogueManager는 대사가 끝날 때마다 PlayerMove를 다시 켠다.
    /// 대사 블록이 끝난 뒤 다시 잠그는 것만으로는 한 프레임 틈이 남아서,
    /// 컷씬이 도는 동안에는 매 프레임 잠금을 다시 걸어둔다.
    /// </summary>
    private void LateUpdate()
    {
        if (!_cutsceneRunning || _haru == null)
            return;

        if (_haru.enabled)
            _haru.enabled = false;

        _haru.canMove = false;
    }

    protected override void ApplyWorldByState()
    {
        // 단편 영화라 중간부터 이어보는 의미가 없다. 이 씬에 들어오면 항상 S#1부터 튼다.
        // state는 어디까지 재생했는지 기록만 남긴다.
        StartCoroutine(Co_PlayFilm());
    }

    // 이 컷씬은 마스터 코루틴 하나로 진행하므로 대화 콜백은 쓰지 않는다.
    protected override void HandleDialogueEnd(string dialogueId) { }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    // ================================================================ 마스터 플로우

    private IEnumerator Co_PlayFilm()
    {
        _cutsceneRunning = true;

        LockPlayer(true);
        SetFadeCurtain(_openFromWhite ? Color.white : Color.black);

        // CutsceneManager.Start()가 FadeImage를 다시 불투명하게 만들기 때문에
        // 다른 Start가 전부 돌고 난 다음 프레임부터 연출을 시작한다.
        yield return null;
        SetFadeCurtain(_openFromWhite ? Color.white : Color.black);

        if (_disableDuringCutscene != null)
        {
            foreach (GameObject go in _disableDuringCutscene)
            {
                if (go != null)
                    go.SetActive(false);
            }
        }

        if (_fireworks != null)
            _fireworks.SetActive(false);

        if (_rain != null)
            _rain.SetActive(false);

        SetRainGrade(0f);

        BindCameraRig();

        yield return Co_Scene1();
        yield return Co_Scene2();
        yield return Co_Scene3();
        yield return Co_Scene4();

        state = SummerBreezeState.Finished;
        PersistPuzzleState();
        _cutsceneRunning = false;

        if (!string.IsNullOrEmpty(_nextSceneName))
            SceneManager.LoadScene(_nextSceneName);
        else
            Debug.LogWarning(
                "[SummerBreeze] 다음 씬 이름이 비어 있어 페이드 아웃 상태로 멈춘다. "
                    + "[6-2] 상영실 씬을 만들면 _nextSceneName에 넣을 것.",
                this
            );
    }

    // ---------------------------------------------------------------- S#1

    private IEnumerator Co_Scene1()
    {
        state = SummerBreezeState.S1;
        PersistPuzzleState();

        yield return Co_SetMood(_moodClear);
        // S#1은 씬에 배치해둔 하루의 자리에서 그대로 시작한다.
        // 순간이동시키면 블로킹해둔 위치가 무시돼서 구도가 어긋난다.
        if (_s1UseHaruScenePosition)
            HoldHaru(Vector2.up);
        else
            PlaceHaru(_s1HaruStand, Vector2.up);

        PlaceLuna(_s1LunaEnter, Vector2.right);
        MoveCameraTo(_camS1Start);
        PlayBGM(_bgmBeach);

        // 1번. 흰 화면이 걷히는 동안 정류장에서 해변으로 카메라가 올라간다.
        Coroutine pan = StartCoroutine(Co_PanCamera(_camS1Start, _camS1Beach, _introPanDuration));
        yield return Co_FadeIn(_openFadeDuration);
        yield return pan;

        // 2번. 소녀가 촬영 방향을 왼쪽에서 오른쪽으로 가로지르다 셔터 소리와 함께 멈춘다.
        FaceHaru(Vector2.up);
        yield return MoveLunaTo(_s1LunaShutter);

        Shoot();
        yield return new WaitForSeconds(_shootHoldDuration);

        FaceLunaTowards(_haru != null ? _haru.transform : null);
        FaceHaruTowards(_luna);
        yield return new WaitForSeconds(_beatPause);

        yield return Co_Play("s1_1");

        // 3번. 소녀가 오른쪽으로 이어서 걸어 화면 밖으로 나가고, 소년은 그 방향을 본 채 멈춘다.
        yield return MoveLunaPath(_s1LunaExitPath);
        HideLuna();

        FaceHaru(Vector2.right);
        yield return new WaitForSeconds(_s1HoldAfterExit);

        // 4번. 페이드 아웃.
        yield return Co_FadeOut(_fadeOutDuration);
    }

    // ---------------------------------------------------------------- S#2

    private IEnumerator Co_Scene2()
    {
        state = SummerBreezeState.S2;
        PersistPuzzleState();

        // 1번. 페이드 인. 소년이 방파제 끝에서 2~3칸 떨어진 곳에서 먼 섬을 찍는다.
        yield return Co_SetMood(_moodClear);
        PlaceHaru(_s2HaruStand, Vector2.up);
        PlaceLuna(_s2LunaEnter, Vector2.up);
        MoveCameraTo(_camS2Start);

        yield return new WaitForSeconds(_blackHoldDuration);
        yield return Co_FadeIn(_fadeInDuration);

        Shoot();
        yield return new WaitForSeconds(_shootHoldDuration);

        // 소녀가 걸어와 소년에게서 1~2칸 떨어진 곳에 멈춘다.
        yield return MoveLunaTo(_s2LunaStop);
        FaceLunaTowards(_haru != null ? _haru.transform : null);
        FaceHaruTowards(_luna);
        yield return new WaitForSeconds(_beatPause);

        // 2번. 서로 여행을 왔는지 묻는다.
        yield return Co_Play("s2_1");

        // 3번. 소녀가 방파제 끝으로, 소년이 한 칸 거리를 두고 따라간다. 카메라도 함께 따라간다.
        Coroutine camFollow = StartCoroutine(
            Co_PanCamera(_camS2Start, _camS2End, _s2CameraFollowDuration)
        );

        Coroutine lunaMove = StartCoroutine(Co_MoveLunaThenFace(_s2LunaEnd, Vector2.up));
        yield return MoveHaruTo(_s2HaruEnd);
        FaceHaru(Vector2.up);

        yield return lunaMove;
        yield return camFollow;
        yield return new WaitForSeconds(_beatPause);

        // 두 사람은 바다를 바라보며 섬 이야기를 나눈다.
        yield return Co_Play("s2_12");

        // 4번. 소녀가 소년의 촬영 방향 안으로 한 칸 들어가고, 소년이 섬과 소녀를 함께 찍는다.
        yield return MoveLunaTo(_s2LunaFrameIn);
        FaceLuna(Vector2.up);
        yield return new WaitForSeconds(_beatPause);

        FaceHaru(Vector2.up);
        Shoot();
        yield return new WaitForSeconds(_shootHoldDuration);

        yield return Co_FadeOut(_fadeOutDuration);
    }

    // ---------------------------------------------------------------- S#3

    private IEnumerator Co_Scene3()
    {
        state = SummerBreezeState.S3;
        PersistPuzzleState();

        // 1번. M2 비 상태의 정류장. 소녀가 바다를 향해 서 있다.
        yield return Co_SetMood(_moodRain);
        PlaceHaru(_s3HaruEnter, Vector2.right);
        PlaceLuna(_s3LunaStand, Vector2.up);
        MoveCameraTo(_camS3);

        yield return Co_SwitchBGM(_bgmRainBeach);
        yield return new WaitForSeconds(_blackHoldDuration);
        yield return Co_FadeIn(_fadeInDuration);

        // 소년이 정류장 안으로 걸어와 한 칸 떨어져 멈춘다.
        yield return MoveHaruTo(_s3HaruStop);
        FaceHaruTowards(_luna);
        FaceLunaTowards(_haru != null ? _haru.transform : null);
        yield return new WaitForSeconds(_beatPause);

        // 2번. 빗속에서 보이지 않는 섬 이야기.
        yield return Co_Play("s3_1");

        // 3번. 소년이 빗속의 바다를 바라보며 사진을 찍는다. 소녀는 소년을 바라본다.
        FaceHaru(Vector2.up);
        yield return new WaitForSeconds(_beatPause);

        Shoot();
        yield return new WaitForSeconds(_shootHoldDuration);

        FaceLunaTowards(_haru != null ? _haru.transform : null);
        yield return Co_Play("s3_6");

        // 4번. 불꽃축제에 대한 대사를 이어간다.
        yield return Co_Play("s3_12");

        // 5번. 소년이 소녀 쪽으로 방향을 돌렸다가 다시 바다를 향한다.
        FaceHaruTowards(_luna);
        yield return new WaitForSeconds(_s3LookAtLunaHold);

        FaceHaru(Vector2.up);
        FaceLuna(Vector2.up);
        yield return new WaitForSeconds(_beatPause);

        yield return Co_FadeOut(_fadeOutDuration);
    }

    // ---------------------------------------------------------------- S#4

    private IEnumerator Co_Scene4()
    {
        state = SummerBreezeState.S4;
        PersistPuzzleState();

        // 1번. 페이드 인. 정류장 의자 앞, 소녀는 여행 가방 옆에 서 있다.
        yield return Co_SetMood(_moodEvening);
        PlaceHaru(_s4HaruEnter, Vector2.right);
        PlaceLuna(_s4LunaStand, Vector2.down);
        MoveCameraTo(_camS4BusStop);

        if (_travelBag != null)
            _travelBag.SetActive(true);

        yield return Co_SwitchBGM(_bgmBeach);
        yield return new WaitForSeconds(_blackHoldDuration);
        yield return Co_FadeIn(_fadeInDuration);

        // 소년이 걸어와 한 칸 떨어진 곳에 멈추고, 소녀가 소년 쪽을 바라본다.
        yield return MoveHaruTo(_s4HaruStop);
        FaceHaruTowards(_luna);
        FaceLunaTowards(_haru != null ? _haru.transform : null);
        yield return new WaitForSeconds(_beatPause);

        yield return Co_Play("s4_1");

        // 2번. 소녀가 몸을 반대로 돌린다. 페이드 아웃 뒤 소녀와 여행 가방을 비활성화하고,
        //      버스 효과음이 끝나면 페이드 인 되어 소년만 남는다.
        FaceLuna(Vector2.down);
        yield return new WaitForSeconds(_beatPause);

        yield return Co_FadeOut(_fadeOutDuration);

        HideLuna();
        if (_travelBag != null)
            _travelBag.SetActive(false);

        PlaySFX(_sfxBusDeparture);
        yield return new WaitForSeconds(GetClipLength(_sfxBusDeparture, 2f));

        yield return Co_FadeIn(_fadeInDuration);

        // 3번. 소년은 도로를 향한 채 3초간 멈춘다. 폭죽 소리가 들리기 시작하면 다시 페이드 아웃.
        FaceHaru(Vector2.down);
        yield return new WaitForSeconds(_s4HoldAfterBus);

        StartFireworksSound();
        yield return new WaitForSeconds(_beatPause);
        yield return Co_FadeOut(_fadeOutDuration);

        // 4번. 페이드 인 되면 소년은 혼자 방파제 끝에 앉아 있다.
        MoveCameraTo(_camS4Pier);
        PlaceHaru(_s4HaruSeat, Vector2.up);
        SitHaru();

        // 밤은 별도 맵을 만들지 않고 저녁 위에 더 어두운 색과 불꽃을 얹어서 표현한다.
        yield return Co_SetMood(_moodNight);

        if (_fireworks != null)
            _fireworks.SetActive(true);

        yield return new WaitForSeconds(_blackHoldDuration);
        yield return Co_FadeIn(_fadeInDuration);

        // 5번. 바다를 바라보던 하루가 옆 빈자리를 바라본 뒤 다시 바다를 바라보며 사진을 찍는다.
        yield return new WaitForSeconds(_s4LookAtEmptySeatHold);

        FaceHaruTowards(_s4EmptySeat);
        yield return new WaitForSeconds(_s4LookAtEmptySeatHold);

        FaceHaru(Vector2.up);
        yield return new WaitForSeconds(_beatPause);

        // 6번. 셔터와 함께 화면이 페이드 아웃되고 [6-2] 상영실로 이어진다.
        Shoot();
        yield return new WaitForSeconds(_beatPause);

        StopFireworksSound();
        yield return Co_FadeOut(_fadeOutDuration);
    }

    // ================================================================ 대사

    /// <summary>
    /// 대사 블록 하나를 재생하고 nextId 체인이 전부 끝날 때까지 기다린다.
    /// DialogueManager.EndDialogue가 조작을 되돌려놓기 때문에 끝나면 다시 잠근다.
    /// </summary>
    private IEnumerator Co_Play(string startId)
    {
        dialogueManager.StartDialogue(startId);

        if (!dialogueManager.isRunning)
        {
            Debug.LogWarning(
                $"[SummerBreeze] 대사 '{startId}'를 찾지 못해 건너뛴다. "
                    + "SummerBreeze.json과 sceneId를 확인할 것.",
                this
            );
            yield break;
        }

        yield return new WaitWhile(() => dialogueManager.isRunning);

        LockPlayer(true);
        yield return new WaitForSeconds(_beatPause);
    }

    // ================================================================ 맵 / 카메라

    /// <summary>
    /// 맵은 한 벌이고 색 오버레이와 비 파티클만 바꾼다.
    /// 장면 전환은 전부 검은 화면 아래에서 일어나므로 기본값은 즉시 적용이다.
    /// </summary>
    private IEnumerator Co_SetMood(MapMood mood)
    {
        if (mood == null)
            yield break;

        if (_rain != null)
            _rain.SetActive(mood.rain);

        // 비 보정(Color Adjustments + 곱하기 + 색상 닷지)은 비 상태에서만 켠다
        float gradeFrom = ScreenBlendLayersFeature.Weight;
        float gradeTo = mood.rain ? 1f : 0f;

        Color target = new Color(mood.tint.r, mood.tint.g, mood.tint.b, mood.strength);
        Color from = _moodOverlay != null ? _moodOverlay.color : target;
        float elapsed = 0f;

        while (elapsed < _moodFadeDuration)
        {
            float t = elapsed / _moodFadeDuration;

            if (_moodOverlay != null)
                _moodOverlay.color = Color.Lerp(from, target, t);

            SetRainGrade(Mathf.Lerp(gradeFrom, gradeTo, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_moodOverlay != null)
            _moodOverlay.color = target;

        SetRainGrade(gradeTo);
    }

    /// <summary>
    /// 비 보정 세기. 0이면 꺼짐, 1이면 프로파일·머티리얼에 설정한 그대로.
    /// </summary>
    private void SetRainGrade(float weight)
    {
        ScreenBlendLayersFeature.Weight = weight;

        if (_rainVolume == null && _rainVolumeProfile != null)
        {
            _rainVolume = gameObject.AddComponent<Volume>();
            _rainVolume.isGlobal = true;
            _rainVolume.sharedProfile = _rainVolumeProfile;
        }

        if (_rainVolume != null)
            _rainVolume.weight = weight;
    }

    /// <summary>
    /// 컷씬은 전부 고정 구도라 카메라가 하루를 따라다니면 안 된다.
    /// Virtual Camera의 Follow를 리그로 돌려두고, 구도는 리그를 옮겨서 잡는다.
    /// </summary>
    private void BindCameraRig()
    {
        if (_cameraRig == null)
            return;

        if (_vcam == null)
            _vcam = FindObjectOfType<CinemachineVirtualCamera>();

        if (_vcam == null)
        {
            Debug.LogWarning(
                "[SummerBreeze] Virtual Camera를 찾지 못해 카메라 구도를 잡을 수 없다.",
                this
            );
            return;
        }

        _vcam.Follow = _cameraRig;
        _vcam.LookAt = null;
    }

    private void MoveCameraTo(Transform point)
    {
        if (_cameraRig == null || point == null)
            return;

        Vector3 p = point.position;
        p.z = _cameraRig.position.z;
        _cameraRig.position = p;
    }

    private IEnumerator Co_PanCamera(Transform from, Transform to, float duration)
    {
        if (_cameraRig == null || from == null || to == null)
            yield break;

        float z = _cameraRig.position.z;
        Vector3 a = from.position;
        Vector3 b = to.position;
        a.z = z;
        b.z = z;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _cameraRig.position = Vector3.Lerp(a, b, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _cameraRig.position = b;
    }

    // ================================================================ 페이드

    /// <summary>페이드 이미지를 지정한 색으로 완전히 덮어둔다.</summary>
    private void SetFadeCurtain(Color rgb)
    {
        var img = CutsceneManager.Instance != null ? CutsceneManager.Instance.FadeImage : null;
        if (img == null)
            return;

        img.gameObject.SetActive(true);
        img.color = new Color(rgb.r, rgb.g, rgb.b, 1f);
    }

    private IEnumerator Co_FadeIn(float duration)
    {
        if (CutsceneManager.Instance == null)
            yield break;

        bool done = false;
        CutsceneManager.Instance.FadeFromBlack(() => done = true, duration);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator Co_FadeOut(float duration)
    {
        if (CutsceneManager.Instance == null)
            yield break;

        // 첫 장면만 흰색에서 열고, 장면 사이 전환은 전부 검은색으로 되돌린다.
        var img = CutsceneManager.Instance.FadeImage;
        if (img != null)
            img.color = new Color(0f, 0f, 0f, img.color.a);

        bool done = false;
        CutsceneManager.Instance.FadeToBlack(() => done = true, duration);
        yield return new WaitUntil(() => done);
    }

    // ================================================================ 사운드

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null || SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlayBGM(clip);
        SoundManager.Instance.SetBGMSourceVolume(_bgmVolume);
    }

    /// <summary>
    /// BGM 소스가 하나뿐이라 진짜 크로스페이드는 안 된다.
    /// 화면이 검은 동안 볼륨을 내렸다가 클립을 바꾸고 다시 올린다.
    /// </summary>
    private IEnumerator Co_SwitchBGM(AudioClip clip)
    {
        if (clip == null || SoundManager.Instance == null)
            yield break;

        SoundManager.Instance.SetBGMSourceVolume(0f, _bgmFadeDuration);
        yield return new WaitForSeconds(_bgmFadeDuration);

        SoundManager.Instance.PlayBGM(clip);
        SoundManager.Instance.SetBGMSourceVolume(_bgmVolume, _bgmFadeDuration);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySFX(clip, _sfxVolume);
    }

    private void StartFireworksSound()
    {
        if (_sfxFireworks == null || SoundManager.Instance == null)
            return;

        // 마지막 장면 내내 깔려야 해서 PlayOneShot이 아니라 루프 채널을 쓴다.
        SoundManager.Instance.PlayLoopSFX(_sfxFireworks, _fireworksVolume, 1f);
    }

    private void StopFireworksSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopLoopSFX(_fadeOutDuration);
    }

    private static float GetClipLength(AudioClip clip, float fallback)
    {
        return clip != null ? clip.length : fallback;
    }

    // ================================================================ 인물 제어

    /// <summary>옮기지 않고 지금 서 있는 자리에서 시선만 맞춘다.</summary>
    private void HoldHaru(Vector2 facing)
    {
        if (_haru == null)
            return;

        _haru.SetFacing(facing);
        LockPlayer(true);
    }

    private void PlaceHaru(Transform point, Vector2 facing)
    {
        if (_haru == null || point == null)
            return;

        _haru.Teleport(point.position);
        _haru.SetFacing(facing);

        // Teleport가 canMove를 되돌려놓기 때문에 여기서 다시 잠근다.
        LockPlayer(true);
    }

    private void PlaceLuna(Transform point, Vector2 facing)
    {
        if (_luna == null)
            return;

        _luna.gameObject.SetActive(true);

        if (point != null)
            _luna.position = point.position;

        TileActorMover.SetWalking(_lunaAnimator, _lunaAnim, false, _lunaMoveSpeed);
        TileActorMover.SetFacing(_lunaAnimator, _lunaAnim, facing);
    }

    private void HideLuna()
    {
        if (_luna != null)
            _luna.gameObject.SetActive(false);
    }

    private IEnumerator MoveHaruTo(Transform point)
    {
        if (_haru == null || point == null)
            yield break;

        yield return TileActorMover.MoveTo(
            _haru.transform,
            _haru.animator,
            _haruAnim,
            point.position,
            _haruMoveSpeed
        );
    }

    private IEnumerator MoveLunaTo(Transform point)
    {
        if (_luna == null || point == null)
            yield break;

        yield return TileActorMover.MoveTo(
            _luna,
            _lunaAnimator,
            _lunaAnim,
            point.position,
            _lunaMoveSpeed
        );
    }

    private IEnumerator MoveLunaPath(Transform[] path)
    {
        if (_luna == null || path == null || path.Length == 0)
            yield break;

        yield return TileActorMover.MovePath(_luna, _lunaAnimator, _lunaAnim, path, _lunaMoveSpeed);
    }

    /// <summary>이동이 끝나는 즉시 시선을 돌린다. 도착하고 나서 한 박자 뒤에 돌면 어색하다.</summary>
    private IEnumerator Co_MoveLunaThenFace(Transform point, Vector2 facing)
    {
        yield return MoveLunaTo(point);
        FaceLuna(facing);
    }

    private void FaceHaru(Vector2 dir)
    {
        if (_haru != null)
            _haru.SetFacing(dir);
    }

    private void FaceLuna(Vector2 dir)
    {
        TileActorMover.SetFacing(_lunaAnimator, _lunaAnim, dir);
    }

    private void FaceHaruTowards(Transform target)
    {
        if (_haru == null || target == null)
            return;

        Vector2 dir = target.position - _haru.transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            _haru.SetFacing(dir);
    }

    private void FaceLunaTowards(Transform target)
    {
        if (_luna == null || target == null)
            return;

        Vector2 dir = target.position - _luna.position;
        if (dir.sqrMagnitude > 0.0001f)
            TileActorMover.SetFacing(_lunaAnimator, _lunaAnim, dir);
    }

    /// <summary>사진 촬영 모션 + 셔터음.</summary>
    private void Shoot()
    {
        if (_haru != null && _haru.animator != null)
            _haru.animator.SetTrigger("TakeShoot");

        PlaySFX(_sfxShutter);
    }

    private void SitHaru()
    {
        if (_haru == null || _haru.animator == null)
            return;

        _haru.animator.ResetTrigger("SitDownEnd");
        _haru.animator.SetTrigger("SitDown");
    }

    // ================================================================ 입력 잠금

    /// <summary>
    /// 컷씬 동안에는 이동/상호작용/촬영 입력을 전부 막는다.
    /// DialogueManager.EndDialogue가 대사가 끝날 때마다 PlayerMove를 다시 켜기 때문에
    /// 대사 블록이 끝날 때마다 여기를 다시 호출해야 한다.
    /// </summary>
    private void LockPlayer(bool locked)
    {
        if (_haru == null)
            return;

        _haru.enabled = !locked;

        if (!locked)
            return;

        // enabled를 끄는 것만으로는 한 프레임 틈이 생길 수 있어서 canMove도 같이 내려둔다.
        // (PlayerMove_Test_Lerp.Update는 canMove가 false면 입력을 아예 읽지 않는다)
        _haru.canMove = false;

        if (_haru.animator != null)
        {
            _haru.animator.SetBool("Walking", false);
            _haru.animator.SetBool("Pushing", false);
        }

        if (_haruInputScripts == null)
        {
            var interaction = _haru.GetComponent<InteractionSystem>();
            var action = _haru.GetComponent<PlayerActionController>();

            _haruInputScripts = new MonoBehaviour[] { interaction, action };
        }

        foreach (MonoBehaviour script in _haruInputScripts)
        {
            if (script != null)
                script.enabled = false;
        }
    }
}
