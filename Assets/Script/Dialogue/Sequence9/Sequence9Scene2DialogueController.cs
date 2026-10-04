using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public enum S9S2State
{
    None,          // 씬 시작 직후. 도입 혼잣말 전
    PhotoStudio,   // 5-1 변형된 사진관 (로비 / 암실)
    House,         // 5-2 변형된 집. 전화벨이 울리는 중
    HouseCallDone, // 5-2 통화 종료. 옷장이 열려 있음
    CinemaLobby,   // 5-4 변형된 영화관 로비
    Screen,        // 5-5 스크린 연출 재생 중
    ScreenDone,    // 5-5 스크린 문 등장. 자유 조작
    Corridor,      // 5-6 반복되는 복도
    FinalCorridor, // 5-6 문이 전부 402호가 된 복도. 케이 사살 연출
    KeiDead,       // 5-6 연출 종료. 케이가 나온 문만 열려 있음
    TvRoom,        // 5-7 텔레비전 공간
    Finished,      // 시퀀스 10으로 넘어가기 직전
}

/// <summary>
/// 시퀀스9 씬2 "상영 중" 컨트롤러.
/// 변형된 사진관 → 집 → 영화관 → 스크린 → 반복되는 복도 → 텔레비전 공간을
/// 한 씬 안에서 공간 단위로 순간이동시키며 진행한다.
///
/// 일러스트/스크린/문패 등 아직 없는 리소스는 비워두면 임시 표시(라벨)로 대신 진행한다.
/// 리소스가 들어오면 인스펙터에 스프라이트/클립만 꽂으면 된다.
/// </summary>
public class Sequence9Scene2DialogueController : DialogueController<S9S2State>
{
    /// <summary>케이 사살 직후 연속으로 띄우는 일러스트 한 장</summary>
    [System.Serializable]
    private class ShotCut
    {
        public Sprite sprite;

        [Tooltip("스프라이트가 비어 있을 때 대신 띄울 설명")]
        public string placeholderLabel;

        [Tooltip("노출 시간(초)")]
        public float duration = 1f;
    }

    [Header("하루")]
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [SerializeField]
    private ActorAnimParams _haruAnimParams = new ActorAnimParams();

    [Tooltip("암실에서 켜지는 하루의 조명. 로비↔암실은 원본 ChangeLightTrigger가 켜고 끄고, 선반 문으로 나갈 때는 여기서 끈다.")]
    [SerializeField]
    private Light2D _haruLight;

    [Header("케이")]
    [SerializeField]
    private GameObject _kei;

    [SerializeField]
    private Animator _keiAnimator;

    [Tooltip("케이 애니메이터에는 AnimSpeed가 없다")]
    [SerializeField]
    private ActorAnimParams _keiAnimParams = new ActorAnimParams { animSpeed = "" };

    [Header("공간별 도착 지점")]
    [SerializeField]
    private Transform _photoLobbySpawn;

    [SerializeField]
    private Transform _darkroomSpawn;

    [SerializeField]
    private Transform _houseSpawn;

    [SerializeField]
    private Transform _cinemaLobbySpawn;

    [Tooltip("스크린 공간에 들어선 지점")]
    [SerializeField]
    private Transform _screenRoomSpawn;

    [Tooltip("하루가 걸어가서 멈추는 스크린 바로 앞")]
    [SerializeField]
    private Transform _screenStandPoint;

    [Tooltip("복도의 시작 지점. 어느 문으로 들어가도 여기로 돌아온다.")]
    [SerializeField]
    private Transform _corridorSpawn;

    [Tooltip("402호 복도에 도착했을 때 하루가 서는 위치. 케이가 나오는 문 아래쪽.")]
    [SerializeField]
    private Transform _corridorFinalPoint;

    [Tooltip("1-1과 같은 텔레비전 앞 하루의 위치")]
    [SerializeField]
    private Transform _tvRoomSpawn;

    [Header("공간별 카메라")]
    [SerializeField]
    private CinemachineVirtualCamera _photoLobbyCam;

    [SerializeField]
    private CinemachineVirtualCamera _darkroomCam;

    [SerializeField]
    private CinemachineVirtualCamera _houseCam;

    [SerializeField]
    private CinemachineVirtualCamera _cinemaLobbyCam;

    [SerializeField]
    private CinemachineVirtualCamera _screenRoomCam;

    [Tooltip("틸트 업 이후 스크린을 화면 가득 담는 고정 카메라")]
    [SerializeField]
    private CinemachineVirtualCamera _screenCloseupCam;

    [SerializeField]
    private CinemachineVirtualCamera _corridorCam;

    [SerializeField]
    private CinemachineVirtualCamera _tvRoomCam;

    [Header("문")]
    [SerializeField]
    private S9S2Door _wardrobeDoor;

    [SerializeField]
    private S9S2Door _screenDoor;

    [Tooltip("복도 문 4개. 왼쪽부터 순서대로.")]
    [SerializeField]
    private S9S2Door[] _corridorDoors;

    [Tooltip("케이가 나오는 문 (왼쪽에서 세 번째 = 2)")]
    [SerializeField]
    private int _keiDoorIndex = 2;

    [Tooltip("기획서 4-5는 '네 문 모두 열림', 5-6/8-2는 '세 번째 문만 열림'. 기본은 플로우(세 번째 문만)를 따른다.")]
    [SerializeField]
    private bool _openAllDoorsAfterKei;

    [Header("조사 오브젝트")]
    [SerializeField]
    private S9S2Interactable _photoWall;

    [SerializeField]
    private S9S2Interactable _telephone;

    [SerializeField]
    private S9S2Interactable _pistol;

    [Tooltip("문을 통한 이동을 이 횟수만큼 반복하면 권총이 생긴다")]
    [SerializeField]
    private int _loopsBeforePistol = 2;

    [Header("전체화면 일러스트")]
    [SerializeField]
    private Image _illustrationImage;

    [Tooltip("일러스트가 비어 있을 때 쓰는 임시 라벨")]
    [SerializeField]
    private TMP_Text _illustrationPlaceholder;

    [Tooltip("일러스트 위를 덮는 검은 Image. 하드컷 암전에 쓴다. (CutsceneManager의 페이드는 코루틴이라 프레임이 밀린다)")]
    [SerializeField]
    private Image _blackImage;

    [Tooltip("6. 암실 사진 벽: 여러 사진이 루나의 얼굴을 이루는 일러스트")]
    [SerializeField]
    private Sprite _photoWallSprite;

    [SerializeField]
    private ShotCut[] _keiShotCuts =
    {
        new ShotCut { placeholderLabel = "1. 정면을 바라보는 케이의 클로즈업", duration = 2f },
        new ShotCut { placeholderLabel = "2. 권총을 발사하는 하루 (리버스 쇼트)", duration = 1f },
        new ShotCut { placeholderLabel = "3. 기괴하게 일그러진 케이의 클로즈업", duration = 3f },
        new ShotCut { placeholderLabel = "4. 놀람과 슬픔이 뒤섞인 하루", duration = 1.5f },
        new ShotCut { placeholderLabel = "5. 하루와 케이의 얼굴이 겹친 얼굴", duration = 5f },
    };

    [Tooltip("첫 총성 뒤 암전 유지 시간")]
    [SerializeField]
    private float _blackoutAfterFirstShot = 1f;

    [Tooltip("2번과 3번 사이 암전 프레임")]
    [SerializeField]
    private float _blackFrameBeforeCut3 = 0.1f;

    [Header("케이 사살 연출")]
    [Tooltip("케이 이동 속도 상한. 1 = 플레이어 걷기와 동일. 복도가 짧으면 아래 '다가오는 시간'에 맞춰 더 느려진다.")]
    [SerializeField]
    private float _keiMoveSpeed = 0.25f;

    [Tooltip("슬로우 구간의 애니메이션 재생 속도")]
    [SerializeField]
    private float _slowAnimSpeed = 0.4f;

    [Tooltip("줌 끝 크기. 원래 크기보다 작을수록 가깝다.")]
    [SerializeField]
    private float _finalZoomOrthoSize = 3f;

    [Tooltip("케이가 문에서 하루 바로 앞까지 오는 시간 = 줌 시간. 줌이 끝나는 순간 총성이 울린다.")]
    [SerializeField]
    private float _finalZoomDuration = 8f;

    [Tooltip("케이가 있던 자리에 남는 검은 그을음")]
    [SerializeField]
    private GameObject _soot;

    [Header("스크린")]
    [Tooltip("스크린 영상(시퀀스 2~8 장면 → 현재의 하루). 비워두면 아래 슬라이드로 대신한다.")]
    [SerializeField]
    private VideoClip _screenClip;

    [SerializeField]
    private VideoPlayer _screenVideoPlayer;

    [Tooltip("월드 스페이스 캔버스 위의 스크린 화면")]
    [SerializeField]
    private RawImage _screenImage;

    [SerializeField]
    private TMP_Text _screenPlaceholder;

    [Tooltip("영상이 없을 때 스크린에 순서대로 띄울 임시 장면들")]
    [SerializeField]
    private Sprite[] _screenFallbackSlides;

    [SerializeField]
    private float _fallbackSlideSeconds = 1.5f;

    [Tooltip("틸트 업 / 복귀 블렌드 시간")]
    [SerializeField]
    private float _screenBlendSeconds = 2f;

    [Header("텔레비전 공간")]
    [Tooltip("1-1의 텔레비전 오브젝트")]
    [SerializeField]
    private Transform _tvTransform;

    [Tooltip("TV OFF 애니메이션 트리거. 애니메이션이 나오면 이름을 적을 것. 비우면 코드로 브라운관 꺼짐을 흉내낸다.")]
    [SerializeField]
    private string _tvOffTrigger = "";

    [SerializeField]
    private float _tvOffDuration = 0.6f;

    [Tooltip("하루의 뒷모습을 보여주는 시간")]
    [SerializeField]
    private float _tvHoldBeforeOff = 4f;

    [Tooltip("텔레비전이 꺼진 뒤 페이드 아웃 (기획: 3초)")]
    [SerializeField]
    private float _endFadeOutDuration = 3f;

    [Header("사운드")]
    [Tooltip("BGM_9-2. 갑툭튀 SFX가 나올 때까지 재생")]
    [SerializeField]
    private AudioClip _bgm;

    [Tooltip("마지막 복도에서 따로 시작할 BGM이 있으면 지정. 비우면 9-2 BGM을 그대로 이어간다.")]
    [SerializeField]
    private AudioClip _finalCorridorBgm;

    [SerializeField]
    private AudioClip _phoneRingSfx;

    [Range(0f, 1f)]
    [SerializeField]
    private float _phoneRingVolume = 0.7f;

    [SerializeField]
    private AudioClip _gunshotSfx;

    [SerializeField]
    private AudioClip _jumpScareSfx;

    [SerializeField]
    private AudioClip _tvOffSfx;

    [Header("타이밍")]
    [SerializeField]
    private float _introFadeInDuration = 1.5f;

    [SerializeField]
    private float _doorFadeOutDuration = 0.4f;

    [SerializeField]
    private float _doorHoldBlack = 0.3f;

    [SerializeField]
    private float _doorFadeInDuration = 0.5f;

    [Header("문 진입")]
    [Tooltip("문 앞 칸에서 문 쪽 방향키를 누르면 들어간다. (Space 조사로도 들어갈 수 있다)")]
    [SerializeField]
    private bool _enterDoorByWalking = true;

    [Header("씬 전환")]
    [SerializeField]
    private string _nextSceneName = "Sequence10S#1";

    // 저장 키
    private const string KEY_LOOPS = "CorridorLoops";
    private const string KEY_GUN = "GunTaken";
    private const string KEY_PHOTO = "PhotoWallSeen";

    private int _corridorLoops;
    private bool _gunTaken;
    private bool _photoWallSeen;

    private bool _busy;

    // 문을 지난 뒤에는 방향키를 한 번 뗐다가 다시 눌러야 다음 문에 들어간다.
    // 안 그러면 ↑를 누른 채로 복도 문을 연달아 통과해버린다.
    private bool _walkInArmed = true;

    private RenderTexture _screenTexture;
    private readonly List<CinemachineVirtualCamera> _roomCams = new List<CinemachineVirtualCamera>();

    protected override void Awake()
    {
        base.Awake();

        foreach (
            CinemachineVirtualCamera cam in new[]
            {
                _photoLobbyCam,
                _darkroomCam,
                _houseCam,
                _cinemaLobbyCam,
                _screenRoomCam,
                _screenCloseupCam,
                _corridorCam,
                _tvRoomCam,
            }
        )
        {
            if (cam != null && !_roomCams.Contains(cam))
                _roomCams.Add(cam);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_screenTexture != null)
        {
            _screenTexture.Release();
            Destroy(_screenTexture);
        }
    }

    protected override void PersistExtra()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetInt(Key(KEY_LOOPS), _corridorLoops);
        GameManager.Instance.SetInt(Key(KEY_GUN), _gunTaken ? 1 : 0);
        GameManager.Instance.SetInt(Key(KEY_PHOTO), _photoWallSeen ? 1 : 0);
    }

    protected override void RestoreExtra()
    {
        if (GameManager.Instance == null)
            return;

        _corridorLoops = GameManager.Instance.GetInt(Key(KEY_LOOPS), 0);
        _gunTaken = GameManager.Instance.GetInt(Key(KEY_GUN), 0) == 1;
        _photoWallSeen = GameManager.Instance.GetInt(Key(KEY_PHOTO), 0) == 1;
    }

    // ================================================================ 상태 적용

    protected override void ApplyWorldByState()
    {
        HideIllustration();
        SetBlack(false);
        ShowScreenPlaceholder(false);

        if (_kei != null)
            _kei.SetActive(false);

        // 복원 지점은 전부 조명이 꺼진 상태로 시작한다 (사진관은 로비에서 시작)
        SetHaruLight(false);

        if (_photoWall != null)
            _photoWall.SetSparkle(!_photoWallSeen);

        if (_wardrobeDoor != null)
            _wardrobeDoor.SetOpen(state >= S9S2State.HouseCallDone);

        if (_screenDoor != null)
            _screenDoor.SetPresent(state >= S9S2State.ScreenDone);

        ApplyCorridorWorld();

        LockPlayer(true);

        // 연출 도중(Screen / FinalCorridor)에 저장된 경우는 그 연출의 시작부터 다시 재생한다
        switch (state)
        {
            case S9S2State.None:
                PlaceHaru(_photoLobbySpawn, Vector2.down, _photoLobbyCam);
                StartCoroutine(Co_Intro());
                break;

            case S9S2State.PhotoStudio:
                PlaceHaru(_photoLobbySpawn, Vector2.down, _photoLobbyCam);
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.House:
                PlaceHaru(_houseSpawn, Vector2.down, _houseCam);
                StartPhoneRing();
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.HouseCallDone:
                PlaceHaru(_houseSpawn, Vector2.down, _houseCam);
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.CinemaLobby:
            case S9S2State.Screen:
                state = S9S2State.CinemaLobby;
                PlaceHaru(_cinemaLobbySpawn, Vector2.down, _cinemaLobbyCam);
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.ScreenDone:
                PlaceHaru(_screenStandPoint, Vector2.up, _screenRoomCam);
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.Corridor:
                PlaceHaru(_corridorSpawn, Vector2.up, _corridorCam);
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.FinalCorridor:
                PlaceHaru(_corridorFinalPoint, Vector2.up, _corridorCam);
                StartCoroutine(Co_RestoreThenFinalCorridor());
                break;

            case S9S2State.KeiDead:
                PlaceHaru(_corridorFinalPoint, Vector2.up, _corridorCam);
                StartCoroutine(Co_RestoreFadeIn());
                break;

            case S9S2State.TvRoom:
            case S9S2State.Finished:
                StartCoroutine(Co_TvRoom());
                break;
        }
    }

    private IEnumerator Co_RestoreFadeIn()
    {
        // CutsceneManager.Start()가 화면을 검게 덮으므로 한 프레임 넘긴 뒤에 걷어낸다
        yield return null;

        PlayBgm(_bgm);
        yield return Co_FadeFromBlack(_introFadeInDuration);

        LockPlayer(false);
    }

    private IEnumerator Co_RestoreThenFinalCorridor()
    {
        yield return null;
        yield return Co_FadeFromBlack(_introFadeInDuration);
        yield return Co_FinalCorridor();
    }

    // ================================================================ 대화 이벤트

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            // 5-1: 혼잣말 이후 조작이 바로 이어진다
            case "intro":
                state = S9S2State.PhotoStudio;
                PersistPuzzleState();
                break;

            // 5-2: 통화가 끝나면 옷장이 열린다
            case "phone_call":
                state = S9S2State.HouseCallDone;
                PersistPuzzleState();
                if (_wardrobeDoor != null)
                    _wardrobeDoor.SetOpen(true);
                if (_telephone != null)
                    _telephone.SetSparkle(false);
                break;
        }

        // 연출 도중에 끝난 대사가 아니면 DialogueManager.EndDialogue가 조작을 돌려준다.
        // 연출 중이면 다시 잠근다.
        if (_busy)
            LockPlayer(true);
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    // ================================================================ 5-1 도입

    private IEnumerator Co_Intro()
    {
        yield return null;

        // 9-1의 붉은 커튼을 통과한 직후. 9-2 BGM과 함께 밝힌다.
        PlayBgm(_bgm);
        yield return Co_FadeFromBlack(_introFadeInDuration);

        dialogueManager.StartDialogue("intro");
    }

    // ================================================================ 문

    private void Update()
    {
        if (!_enterDoorByWalking)
            return;

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input == Vector2.zero)
        {
            _walkInArmed = true;
            return;
        }

        if (_walkInArmed)
            TryWalkIntoDoor(TileActorMover.SnapTo4Dir(input));
    }

    /// <summary>
    /// 문 앞 칸에서 문 쪽으로 걸으면 들어간다. 문은 벽 칸이라 실제로 밟을 수는 없어서,
    /// InteractionSystem과 같은 레이캐스트(거리 1)로 바로 앞의 문을 찾는다.
    /// 잠긴 문은 무시한다. (Space로 조사하면 잠김 대사가 나온다)
    /// </summary>
    public bool TryWalkIntoDoor(Vector2 dir)
    {
        if (_busy || _playerMove == null || !_playerMove.enabled || !_playerMove.canMove)
            return false;

        if (dialogueManager != null && dialogueManager.isRunning)
            return false;

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(_playerMove.transform.position, dir, 1f, _playerMove.NoPass))
        {
            S9S2Door door = hit.collider != null ? hit.collider.GetComponent<S9S2Door>() : null;

            if (door != null && CanWalkInto(door))
            {
                OnDoorInspected(door);
                return true;
            }
        }

        return false;
    }

    private bool CanWalkInto(S9S2Door door)
    {
        switch (door.Kind)
        {
            case S9S2Door.DoorKind.DarkroomFromLobby:
                return true;
            case S9S2Door.DoorKind.ShelfToHouse:
                return state <= S9S2State.PhotoStudio;
            case S9S2Door.DoorKind.Wardrobe:
                return state == S9S2State.HouseCallDone;
            case S9S2Door.DoorKind.CinemaToScreen:
                return state == S9S2State.CinemaLobby;
            case S9S2Door.DoorKind.ScreenDoor:
                return state == S9S2State.ScreenDone;
            case S9S2Door.DoorKind.Corridor:
                return state == S9S2State.Corridor
                    || (state == S9S2State.KeiDead && (_openAllDoorsAfterKei || door.CorridorIndex == _keiDoorIndex));
            default:
                return false;
        }
    }

    /// <summary>S9S2Door(Space 조사) 또는 문 쪽으로 걷기에서 호출</summary>
    public void OnDoorInspected(S9S2Door door)
    {
        if (_busy || door == null)
            return;

        _walkInArmed = false;

        switch (door.Kind)
        {
            // 5-1: 로비 상단 왼쪽 문 → 암실
            case S9S2Door.DoorKind.DarkroomFromLobby:
                StartCoroutine(Co_MoveRoom(_darkroomSpawn, Vector2.up, _darkroomCam, null, null));
                break;

            // 5-1 → 5-2: 선반 자리의 열린 문 → 변형된 집. 도착하면 전화벨.
            case S9S2Door.DoorKind.ShelfToHouse:
                if (state > S9S2State.PhotoStudio)
                    break;

                state = S9S2State.House;
                PersistPuzzleState();
                StartCoroutine(
                    Co_MoveRoom(
                        _houseSpawn,
                        Vector2.down,
                        _houseCam,
                        () =>
                        {
                            // 암실에서 켜진 하루 조명을 끄고 도착하자마자 전화벨
                            SetHaruLight(false);
                            StartPhoneRing();
                        },
                        null
                    )
                );
                break;

            // 5-2 → 5-4: 통화가 끝나 열린 옷장 → 영화관 로비, 도착 후 혼잣말
            case S9S2Door.DoorKind.Wardrobe:
                if (state == S9S2State.House)
                {
                    dialogueManager.StartDialogue("wardrobe_closed");
                    break;
                }

                if (state != S9S2State.HouseCallDone)
                    break;

                state = S9S2State.CinemaLobby;
                PersistPuzzleState();
                StartCoroutine(Co_MoveRoom(_cinemaLobbySpawn, Vector2.down, _cinemaLobbyCam, null, "cinema_lobby"));
                break;

            // 5-4 → 5-5: 오른쪽 위 문 → 스크린 공간
            case S9S2Door.DoorKind.CinemaToScreen:
                if (state != S9S2State.CinemaLobby)
                    break;

                StartCoroutine(Co_ScreenSequence());
                break;

            // 5-5 → 5-6: 스크린 하단에 생긴 검은 문 → 반복되는 복도
            case S9S2Door.DoorKind.ScreenDoor:
                if (state != S9S2State.ScreenDone)
                    break;

                state = S9S2State.Corridor;
                _corridorLoops = 0;
                PersistPuzzleState();
                StartCoroutine(
                    Co_MoveRoom(_corridorSpawn, Vector2.up, _corridorCam, ApplyCorridorWorld, "corridor_enter")
                );
                break;

            case S9S2Door.DoorKind.Corridor:
                OnCorridorDoor(door);
                break;
        }
    }

    // ================================================================ 5-6 반복되는 복도

    private void OnCorridorDoor(S9S2Door door)
    {
        switch (state)
        {
            case S9S2State.Corridor:
                if (_gunTaken)
                {
                    // 권총을 든 뒤 한 번 더 → 모든 문이 402호, 케이 등장
                    state = S9S2State.FinalCorridor;
                    PersistPuzzleState();
                    StartCoroutine(Co_EnterFinalCorridor());
                }
                else
                {
                    // 어느 문이든 같은 복도의 시작 지점으로. 정답 경로는 없다.
                    _corridorLoops++;
                    PersistPuzzleState();
                    StartCoroutine(Co_MoveRoom(_corridorSpawn, Vector2.up, _corridorCam, ApplyCorridorWorld, null));
                }
                break;

            case S9S2State.KeiDead:
                if (_openAllDoorsAfterKei || door.CorridorIndex == _keiDoorIndex)
                    StartCoroutine(Co_TvRoom());
                else
                    dialogueManager.StartDialogue("door_locked");
                break;
        }
    }

    /// <summary>복도의 문패 / 권총 / 열린 문 / 그을음을 현재 진행 상태에 맞춘다</summary>
    private void ApplyCorridorWorld()
    {
        bool all402 = state >= S9S2State.FinalCorridor;
        bool keiDead = state >= S9S2State.KeiDead;

        if (_corridorDoors != null)
        {
            foreach (S9S2Door d in _corridorDoors)
            {
                if (d == null)
                    continue;

                d.SetPlate(all402 ? "402" : d.OriginalPlate);
                d.SetOpen(keiDead && (_openAllDoorsAfterKei || d.CorridorIndex == _keiDoorIndex));
            }
        }

        if (_pistol != null)
        {
            bool show = state == S9S2State.Corridor && !_gunTaken && _corridorLoops >= _loopsBeforePistol;
            _pistol.gameObject.SetActive(show);
            _pistol.SetSparkle(show);
        }

        if (_soot != null)
            _soot.SetActive(keiDead);
    }

    private IEnumerator Co_EnterFinalCorridor()
    {
        _busy = true;
        LockPlayer(true);

        yield return Co_FadeToBlack(_doorFadeOutDuration);

        PlaceHaru(_corridorFinalPoint, Vector2.up, _corridorCam);
        ApplyCorridorWorld();

        if (_doorHoldBlack > 0f)
            yield return new WaitForSeconds(_doorHoldBlack);

        yield return Co_FadeFromBlack(_doorFadeInDuration);

        yield return Co_FinalCorridor();
    }

    // ================================================================ 8-2 케이 사살 및 복도 종료 연출

    private IEnumerator Co_FinalCorridor()
    {
        _busy = true;
        LockPlayer(true);

        // 마지막 복도에 도착하면 조작 제한과 동시에 BGM
        if (_finalCorridorBgm != null)
            PlayBgm(_finalCorridorBgm);

        S9S2Door keiDoor = GetCorridorDoor(_keiDoorIndex);
        Transform haru = _playerMove != null ? _playerMove.transform : null;

        if (_kei == null || keiDoor == null || haru == null)
        {
            Debug.LogWarning("[S9S2] 케이 / 케이의 문 / 하루 중 비어 있는 것이 있어 사살 연출을 건너뛴다.", this);
            yield return Co_AfterKeiShot(haru != null ? haru.position : Vector3.zero);
            yield break;
        }

        float originalOrtho = _corridorCam != null ? _corridorCam.m_Lens.OrthographicSize : 5f;
        Coroutine zoom = StartCoroutine(Co_Zoom(_corridorCam, originalOrtho, _finalZoomOrthoSize, _finalZoomDuration));

        // 모든 움직임에 슬로우
        Animator haruAnimator = _playerMove.animator;
        SetAnimSpeed(haruAnimator, _slowAnimSpeed);
        SetAnimSpeed(_keiAnimator, _slowAnimSpeed);

        // 케이가 세 번째 문을 열고 나타난다
        keiDoor.SetOpen(true);

        Vector3 doorPos = keiDoor.transform.position;
        Vector3 haruPos = haru.position;

        _kei.transform.position = new Vector3(doorPos.x, doorPos.y, _kei.transform.position.z);
        _kei.SetActive(true);
        TileActorMover.SetFacing(_keiAnimator, _keiAnimParams, Vector2.down);

        // 경로: 문 → 하루 두 칸 앞 → (하루가 물러나는 동안) 하루가 있던 칸 = 바로 앞.
        // 뒤가 벽이면 하루는 물러나지 못하고 그 자리에서 케이를 맞는다. (벽 속으로 밀려 들어가면 이후 조작이 막힌다)
        Vector2 towardKei = SnapDir(doorPos - haruPos);
        Vector3 nearPoint = haruPos + (Vector3)(towardKei * 2f);
        Vector3 bendPoint = new Vector3(doorPos.x, nearPoint.y, 0f);

        bool canBackStep = IsWalkable(haruPos, haruPos - (Vector3)towardKei);
        Vector3 keiStop = canBackStep ? haruPos : haruPos + (Vector3)towardKei;

        // 복도 길이와 상관없이 줌이 끝나는 순간 케이가 도착하도록 속도를 맞춘다
        float tiles = Manhattan(doorPos, bendPoint) + Manhattan(bendPoint, nearPoint) + Manhattan(nearPoint, keiStop);
        float keiSpeed = _keiMoveSpeed;
        if (tiles > 0f && _finalZoomDuration > 0f)
            keiSpeed = Mathf.Min(_keiMoveSpeed, tiles * TileActorMover.StepSeconds / _finalZoomDuration);

        float stepSeconds = TileActorMover.StepSeconds / Mathf.Max(0.01f, keiSpeed);

        yield return TileActorMover.MovePath(
            _kei.transform,
            _keiAnimator,
            _keiAnimParams,
            new[] { MakeTempPoint(bendPoint), MakeTempPoint(nearPoint) },
            keiSpeed
        );

        // 하루는 케이의 마지막 한 걸음과 같은 박자로 물러난다
        Coroutine keiStep = StartCoroutine(
            TileActorMover.MoveTo(_kei.transform, _keiAnimator, _keiAnimParams, keiStop, keiSpeed)
        );

        if (canBackStep)
            yield return Co_HaruBackStep(-towardKei, towardKei, stepSeconds);

        yield return keiStep;

        // 바로 앞까지 오면 첫 번째 총성 + 즉시 암전 + BGM 끊김
        PlaySfx(_gunshotSfx);
        SetBlack(true);
        StopBgm();

        if (zoom != null)
            StopCoroutine(zoom);

        // 암전 아래에서 원래대로 돌려놓는다
        SetAnimSpeed(haruAnimator, 1f);
        SetAnimSpeed(_keiAnimator, 1f);
        if (_corridorCam != null)
            _corridorCam.m_Lens.OrthographicSize = originalOrtho;

        Vector3 keiPos = _kei.transform.position;
        _kei.SetActive(false);

        yield return Co_AfterKeiShot(keiPos);
    }

    /// <summary>암전 이후 일러스트 5장 → 복도 복귀</summary>
    private IEnumerator Co_AfterKeiShot(Vector3 keiPos)
    {
        SetBlack(true);

        if (_blackoutAfterFirstShot > 0f)
            yield return new WaitForSeconds(_blackoutAfterFirstShot);

        float jumpScareStart = -1f;

        for (int i = 0; i < _keiShotCuts.Length; i++)
        {
            ShotCut cut = _keiShotCuts[i];

            // 2번과 3번 사이: 짧은 암전 프레임으로 갑툭튀를 강조
            if (i == 2 && _blackFrameBeforeCut3 > 0f)
            {
                HideIllustration();
                SetBlack(true);
                yield return new WaitForSeconds(_blackFrameBeforeCut3);
            }

            // 1~4번은 하드컷 (페이드 없음)
            ShowIllustration(cut.sprite, cut.placeholderLabel);
            SetBlack(false);

            float hold = cut.duration;

            switch (i)
            {
                case 1: // 두 번째 총성
                    PlaySfx(_gunshotSfx);
                    break;

                case 2: // 갑툭튀 SFX 시작 (9-2 BGM은 첫 총성에서 이미 끊겼다)
                    StopBgm();
                    PlaySfx(_jumpScareSfx);
                    jumpScareStart = Time.time;
                    break;

                case 3: // 세 번째 총성이 갑툭튀 위에 겹친다
                    PlaySfx(_gunshotSfx);
                    break;

                case 4: // 3번에서 시작한 갑툭튀 SFX가 끝날 때까지 유지
                    if (_jumpScareSfx != null && jumpScareStart >= 0f)
                    {
                        float remain = _jumpScareSfx.length - (Time.time - jumpScareStart);
                        hold = Mathf.Max(hold, remain);
                    }
                    break;
            }

            if (hold > 0f)
                yield return new WaitForSeconds(hold);
        }

        // 페이드 없이 복도 화면으로
        HideIllustration();
        SetBlack(false);

        if (_soot != null)
            _soot.transform.position = new Vector3(keiPos.x, keiPos.y, _soot.transform.position.z);

        state = S9S2State.KeiDead;
        PersistPuzzleState();
        ApplyCorridorWorld();

        _busy = false;
        LockPlayer(false);
    }

    private IEnumerator Co_HaruBackStep(Vector2 moveDir, Vector2 faceDir, float seconds)
    {
        if (_playerMove == null)
            yield break;

        Transform haru = _playerMove.transform;
        Animator animator = _playerMove.animator;

        // 뒷걸음질: 케이를 바라본 채로 물러난다
        TileActorMover.SetFacing(animator, _haruAnimParams, faceDir);
        TileActorMover.SetWalking(animator, _haruAnimParams, true, _slowAnimSpeed);

        Vector3 from = haru.position;
        Vector3 to = from + (Vector3)moveDir;
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            haru.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }

        haru.position = to;
        TileActorMover.SetWalking(animator, _haruAnimParams, false, _slowAnimSpeed);
        _playerMove.SetFacing(faceDir);
    }

    private IEnumerator Co_Zoom(CinemachineVirtualCamera cam, float from, float to, float duration)
    {
        if (cam == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cam.m_Lens.OrthographicSize = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cam.m_Lens.OrthographicSize = to;
    }

    private S9S2Door GetCorridorDoor(int index)
    {
        if (_corridorDoors == null)
            return null;

        foreach (S9S2Door d in _corridorDoors)
        {
            if (d != null && d.CorridorIndex == index)
                return d;
        }

        return null;
    }

    // ================================================================ 조사 오브젝트

    /// <summary>S9S2Interactable에서 호출</summary>
    public void OnInteract(S9S2Interactable target)
    {
        if (_busy || target == null)
            return;

        switch (target.Kind)
        {
            // 6. 암실 사진 벽: 루나의 얼굴 일러스트 팝업. 조사 후 반짝임이 사라진다.
            case S9S2Interactable.InteractKind.PhotoWall:
                StartCoroutine(Co_PhotoWall());
                break;

            // 5-2: 전화를 받으면 402호로 오라는 목소리
            case S9S2Interactable.InteractKind.Telephone:
                if (state != S9S2State.House)
                    break;

                StopPhoneRing();
                dialogueManager.StartDialogue("phone_call");
                break;

            // 5-6: 벽에 붙은 탁자 위 권총
            case S9S2Interactable.InteractKind.Pistol:
                if (state != S9S2State.Corridor || _gunTaken)
                    break;

                _gunTaken = true;
                PersistPuzzleState();
                ApplyCorridorWorld();
                dialogueManager.StartDialogue("pistol_get");
                break;
        }
    }

    private IEnumerator Co_PhotoWall()
    {
        _busy = true;
        LockPlayer(true);

        ShowIllustration(_photoWallSprite, "여러 장의 사진이 모여 루나의 얼굴을 이룬다");

        // 조사에 쓴 Space가 같은 프레임에 팝업을 닫지 않도록 한 프레임 넘긴다
        yield return null;
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return));

        HideIllustration();

        _photoWallSeen = true;
        PersistPuzzleState();

        if (_photoWall != null)
            _photoWall.SetSparkle(false);

        _busy = false;
        LockPlayer(false);
    }

    // ================================================================ 5-5 스크린 공간

    private IEnumerator Co_ScreenSequence()
    {
        _busy = true;
        state = S9S2State.Screen;
        PersistPuzzleState();

        yield return Co_MoveRoomCore(_screenRoomSpawn, Vector2.up, _screenRoomCam, null);

        // 하루가 스크린 바로 앞까지 다가간다 (조작 제한)
        if (_playerMove != null && _screenStandPoint != null)
        {
            yield return TileActorMover.MoveTo(
                _playerMove.transform,
                _playerMove.animator,
                _haruAnimParams,
                _screenStandPoint.position,
                1f
            );
            _playerMove.SetFacing(Vector2.up);
        }

        // 카메라 틸트 업: 스크린이 뷰포트를 가득 채운다
        CinemachineBrain brain = GetBrain();
        CinemachineBlendDefinition savedBlend = brain != null ? brain.m_DefaultBlend : default;

        if (brain != null)
            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.EaseInOut,
                _screenBlendSeconds
            );

        SwitchCamera(_screenCloseupCam);
        yield return new WaitForSeconds(_screenBlendSeconds);

        yield return Co_PlayScreen();

        // 카메라 원상복구 후 스크린 하단에 검은 문
        SwitchCamera(_screenRoomCam);
        yield return new WaitForSeconds(_screenBlendSeconds);

        if (brain != null)
            brain.m_DefaultBlend = savedBlend;

        if (_screenDoor != null)
            _screenDoor.SetPresent(true);

        state = S9S2State.ScreenDone;
        PersistPuzzleState();

        _busy = false;
        LockPlayer(false);
    }

    private IEnumerator Co_PlayScreen()
    {
        if (_screenClip != null && _screenVideoPlayer != null && _screenImage != null)
        {
            ShowScreenPlaceholder(false);

            if (_screenTexture == null)
            {
                int w = (int)Mathf.Max(16, _screenClip.width);
                int h = (int)Mathf.Max(16, _screenClip.height);
                _screenTexture = new RenderTexture(w, h, 0);
            }

            _screenVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _screenVideoPlayer.targetTexture = _screenTexture;
            _screenVideoPlayer.clip = _screenClip;
            _screenVideoPlayer.isLooping = false;
            _screenImage.texture = _screenTexture;
            _screenImage.color = Color.white;

            _screenVideoPlayer.Prepare();
            yield return new WaitUntil(() => _screenVideoPlayer.isPrepared);

            bool ended = false;
            VideoPlayer.EventHandler onEnd = _ => ended = true;
            _screenVideoPlayer.loopPointReached += onEnd;
            _screenVideoPlayer.Play();

            // loopPointReached가 안 오는 경우를 대비해 길이 + 여유로 상한
            float limit = (float)_screenClip.length + 2f;
            float elapsed = 0f;

            while (!ended && elapsed < limit)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _screenVideoPlayer.loopPointReached -= onEnd;
            _screenVideoPlayer.Stop();
            yield break;
        }

        // 영상이 아직 없으면 지나온 장면 일러스트를 순서대로 띄운다
        Debug.LogWarning("[S9S2] 스크린 영상이 비어 있어 임시 슬라이드로 대신한다.", this);

        bool anySlide = false;

        if (_screenImage != null && _screenFallbackSlides != null)
        {
            foreach (Sprite slide in _screenFallbackSlides)
            {
                if (slide == null)
                    continue;

                anySlide = true;
                ShowScreenPlaceholder(false);
                _screenImage.texture = slide.texture;
                _screenImage.color = Color.white;
                yield return new WaitForSeconds(_fallbackSlideSeconds);
            }
        }

        if (!anySlide)
        {
            ShowScreenPlaceholder(true);
            yield return new WaitForSeconds(3f);
        }

        // 마지막: 스크린 속 하루가 아직 없는 문으로 들어간다 (영상 리소스 몫)
        if (_screenImage != null)
        {
            _screenImage.texture = null;
            _screenImage.color = Color.black;
        }

        ShowScreenPlaceholder(false);
    }

    private void ShowScreenPlaceholder(bool show)
    {
        if (_screenPlaceholder != null)
            _screenPlaceholder.gameObject.SetActive(show);
    }

    // ================================================================ 5-7 텔레비전 공간

    private IEnumerator Co_TvRoom()
    {
        _busy = true;
        state = S9S2State.TvRoom;
        PersistPuzzleState();
        LockPlayer(true);

        yield return Co_FadeToBlack(_doorFadeOutDuration);

        PlaceHaru(_tvRoomSpawn, Vector2.up, _tvRoomCam);
        HideIllustration();
        SetBlack(false);

        // 1-1과 같은 구도: TV를 바라보는 하루의 뒷모습으로 고정
        Animator haruAnimator = _playerMove != null ? _playerMove.animator : null;
        if (haruAnimator != null)
        {
            TileActorMover.SetWalking(haruAnimator, _haruAnimParams, false, 1f);
            haruAnimator.Update(0f);
            haruAnimator.enabled = false;
        }

        yield return new WaitForSeconds(_doorHoldBlack);
        yield return Co_FadeFromBlack(_introFadeInDuration);

        if (_tvHoldBeforeOff > 0f)
            yield return new WaitForSeconds(_tvHoldBeforeOff);

        // 텔레비전이 꺼진다
        PlaySfx(_tvOffSfx);
        yield return Co_TvOff();

        state = S9S2State.Finished;
        PersistPuzzleState();

        // 3초간 페이드 아웃 → 시퀀스 10
        yield return Co_FadeToBlack(_endFadeOutDuration);

        if (string.IsNullOrEmpty(_nextSceneName))
            yield break;

        if (Application.CanStreamedLevelBeLoaded(_nextSceneName))
            SceneManager.LoadScene(_nextSceneName);
        else
            Debug.LogWarning($"[S9S2] 다음 씬 '{_nextSceneName}'이 빌드 설정에 없어 암전 상태로 멈춘다.", this);
    }

    private IEnumerator Co_TvOff()
    {
        if (_tvTransform == null)
            yield break;

        Animator tvAnimator = _tvTransform.GetComponent<Animator>();

        if (tvAnimator != null && !string.IsNullOrEmpty(_tvOffTrigger))
        {
            tvAnimator.SetTrigger(_tvOffTrigger);
            yield return new WaitForSeconds(_tvOffDuration);
            yield break;
        }

        // TV OFF 애니메이션이 오기 전까지: 브라운관이 꺼지듯 세로로 접혔다가 점으로 사라진다
        if (tvAnimator != null)
            tvAnimator.enabled = false;

        Vector3 baseScale = _tvTransform.localScale;
        float squash = _tvOffDuration * 0.4f;
        float shrink = _tvOffDuration * 0.6f;

        yield return Co_Scale(_tvTransform, baseScale, new Vector3(baseScale.x, baseScale.y * 0.03f, baseScale.z), squash);
        yield return Co_Scale(
            _tvTransform,
            _tvTransform.localScale,
            new Vector3(0f, baseScale.y * 0.03f, baseScale.z),
            shrink
        );

        _tvTransform.gameObject.SetActive(false);
        _tvTransform.localScale = baseScale;
    }

    private static IEnumerator Co_Scale(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        t.localScale = to;
    }

    // ================================================================ 공간 이동

    /// <summary>암전 → 하루/카메라 이동 → 밝힘 → (혼잣말) → 조작 복귀</summary>
    private IEnumerator Co_MoveRoom(
        Transform spawn,
        Vector2 facing,
        CinemachineVirtualCamera cam,
        System.Action atBlack,
        string dialogueAfter
    )
    {
        _busy = true;

        yield return Co_MoveRoomCore(spawn, facing, cam, atBlack);

        _busy = false;

        if (!string.IsNullOrEmpty(dialogueAfter))
            dialogueManager.StartDialogue(dialogueAfter);
        else
            LockPlayer(false);
    }

    private IEnumerator Co_MoveRoomCore(
        Transform spawn,
        Vector2 facing,
        CinemachineVirtualCamera cam,
        System.Action atBlack
    )
    {
        LockPlayer(true);

        yield return Co_FadeToBlack(_doorFadeOutDuration);

        PlaceHaru(spawn, facing, cam);
        atBlack?.Invoke();

        if (_doorHoldBlack > 0f)
            yield return new WaitForSeconds(_doorHoldBlack);

        yield return Co_FadeFromBlack(_doorFadeInDuration);
    }

    private void PlaceHaru(Transform spawn, Vector2 facing, CinemachineVirtualCamera cam)
    {
        if (_playerMove != null)
        {
            if (spawn != null)
            {
                Vector3 p = spawn.position;
                _playerMove.Teleport(new Vector3(p.x, p.y, _playerMove.transform.position.z));
            }
            else
            {
                Debug.LogWarning("[S9S2] 도착 지점이 비어 있어 하루를 옮기지 못했다.", this);
            }

            if (_playerMove.animator != null)
                _playerMove.animator.enabled = true;

            _playerMove.SetFacing(facing);
        }

        SwitchCamera(cam);

        // 같은 카메라 안에서 순간이동해도 댐핑으로 미끄러지지 않게 즉시 붙인다
        if (cam != null)
            cam.PreviousStateIsValid = false;
    }

    private void SwitchCamera(CinemachineVirtualCamera target)
    {
        if (target == null)
            return;

        // 먼저 켠 뒤에 끄지 않으면 카메라가 하나도 없는 프레임이 생긴다
        target.gameObject.SetActive(true);

        foreach (CinemachineVirtualCamera cam in _roomCams)
        {
            if (cam != null && cam != target)
                cam.gameObject.SetActive(false);
        }
    }

    private static CinemachineBrain GetBrain()
    {
        Camera main = Camera.main;
        return main != null ? main.GetComponent<CinemachineBrain>() : null;
    }

    // ================================================================ 일러스트 / 암전

    private void ShowIllustration(Sprite sprite, string placeholder)
    {
        if (_illustrationImage == null)
            return;

        _illustrationImage.gameObject.SetActive(true);

        if (sprite != null)
        {
            _illustrationImage.sprite = sprite;
            _illustrationImage.color = Color.white;
        }
        else
        {
            // 일러스트가 오기 전까지는 어두운 판 위에 설명만 띄워 타이밍을 확인할 수 있게 한다
            _illustrationImage.sprite = null;
            _illustrationImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        }

        if (_illustrationPlaceholder != null)
        {
            _illustrationPlaceholder.gameObject.SetActive(sprite == null);
            _illustrationPlaceholder.text = "[임시] " + placeholder;
        }
    }

    private void HideIllustration()
    {
        if (_illustrationImage != null)
            _illustrationImage.gameObject.SetActive(false);

        if (_illustrationPlaceholder != null)
            _illustrationPlaceholder.gameObject.SetActive(false);
    }

    private void SetBlack(bool on)
    {
        if (_blackImage == null)
            return;

        _blackImage.color = Color.black;
        _blackImage.gameObject.SetActive(on);
    }

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

    // ================================================================ 사운드

    private void PlayBgm(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[S9S2] BGM 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(clip);
    }

    private static void StopBgm()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(clip);
    }

    private void StartPhoneRing()
    {
        if (_telephone != null)
            _telephone.SetSparkle(true);

        if (_phoneRingSfx == null)
        {
            Debug.LogWarning("[S9S2] 전화벨 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayLoopSFX(_phoneRingSfx, _phoneRingVolume);
    }

    private static void StopPhoneRing()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopLoopSFX();
    }

    // ================================================================ 기타

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

    private void SetHaruLight(bool on)
    {
        if (_haruLight != null)
            _haruLight.enabled = on;
    }

    private static void SetAnimSpeed(Animator animator, float speed)
    {
        if (animator != null)
            animator.speed = speed;
    }

    /// <summary>PlayerMove_Test_Lerp와 같은 규칙(NoPassing Linecast)으로 한 칸 이동이 가능한지</summary>
    private bool IsWalkable(Vector3 from, Vector3 to)
    {
        if (_playerMove == null)
            return false;

        var col = _playerMove.GetComponent<Collider2D>();
        bool was = col != null && col.enabled;
        if (col != null)
            col.enabled = false;

        bool blocked = Physics2D.Linecast(from, to, _playerMove.NoPass).collider != null;

        if (col != null)
            col.enabled = was;

        return !blocked;
    }

    private static float Manhattan(Vector3 a, Vector3 b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private static Vector2 SnapDir(Vector3 delta) => TileActorMover.SnapTo4Dir(new Vector2(delta.x, delta.y));

    // TileActorMover.MovePath는 Transform 배열을 받으므로 계산한 지점을 임시 오브젝트로 만든다
    private Transform MakeTempPoint(Vector3 pos)
    {
        var go = new GameObject("S9S2_TempPoint");
        go.transform.position = pos;
        Destroy(go, 30f);
        return go.transform;
    }
}
