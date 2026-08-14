using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum S8S1State
{
    None,     // T1 미발동. 하루가 E 통로에서 중앙으로 걸어가는 구간
    Talking,  // T1 발동 이후. 루나와의 대화가 진행 중
    Finished, // 대화 종료. 8-2로 넘어가기 직전
}

/// <summary>
/// 시퀀스8 씬1 "재회" 대화 컨트롤러.
/// 구현 문서의 플로우 2~11번을 대사 블록 종료 시점에 맞춰 실행한다.
///
/// 이동은 전부 타일 단위 4방향이다. 대각선 이동이 없는 게임이라
/// 목표까지 직선으로 Lerp하지 않고 한 축씩 한 칸씩 움직인다.
/// 장애물(중앙 석상 등)을 피해야 하는 경로는 웨이포인트를 여러 개 찍어서 처리한다.
/// </summary>
public class Sequence8Scene1DialogueController : DialogueController<S8S1State>
{
    /// <summary>
    /// 캐릭터마다 애니메이터 파라미터 이름이 달라서 따로 받는다.
    /// 하루: DirX / DirY / Walking / AnimSpeed
    /// 루나: DirX / Diry / IsWalking  (AnimSpeed 없음)
    /// </summary>
    [System.Serializable]
    private class ActorAnimParams
    {
        public string dirX = "DirX";
        public string dirY = "DirY";
        public string walk = "Walking";

        [Tooltip("애니메이션 재생 속도 Float. 컨트롤러에 없으면 비워둘 것.")]
        public string animSpeed = "AnimSpeed";
    }

    /// <summary>
    /// 정원 색조 한 단계.
    /// Global Light 2D의 밝기·색과, Volume의 채도를 함께 바꾼다.
    /// 조명만으로는 스프라이트 원본 색이 남아서 "흑백에 가깝게"가 안 되므로
    /// Color Adjustments의 Saturation을 같이 내린다.
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

    [Tooltip("1 = 플레이어 걷기와 동일(타일당 0.2초). 낮출수록 느려진다.")]
    [SerializeField]
    private float _haruMoveSpeed = 1f;

    [SerializeField]
    private ActorAnimParams _haruAnimParams = new ActorAnimParams();

    [Header("Luna")]
    [SerializeField]
    private Transform _luna;

    [SerializeField]
    private Animator _lunaAnimator;

    [Tooltip("1 = 플레이어 걷기와 동일(타일당 0.2초). 낮출수록 느려진다.")]
    [SerializeField]
    private float _lunaMoveSpeed = 0.6f;

    [SerializeField]
    private ActorAnimParams _lunaAnimParams = new ActorAnimParams
    {
        dirX = "DirX",
        dirY = "Diry",
        walk = "IsWalking",
        animSpeed = "",
    };

    [Header("Triggers")]
    [Tooltip("T1 접근 트리거. 한 번 발동하면 꺼진다.")]
    [SerializeField]
    private GameObject _t1Trigger;

    [Header("Waypoints - 경로는 한 칸씩 4방향으로만 이동한다")]
    [Tooltip("3번: 하루가 대화 구도를 위해 서는 고정 위치까지의 경로")]
    [SerializeField]
    private Transform[] _haruTalkPath;

    [Tooltip("3번: 루나가 하루 앞으로 내려오는 경로. 중앙 석상을 피해서 찍을 것.")]
    [SerializeField]
    private Transform[] _lunaApproachPath;

    [Tooltip("5번: 하루가 중앙 석상 앞까지 가는 경로")]
    [SerializeField]
    private Transform[] _haruStatuePath;

    [Tooltip("5번: 루나가 중앙 석상 앞까지 가는 경로")]
    [SerializeField]
    private Transform[] _lunaStatuePath;

    [Tooltip("10번: 루나가 D를 따라 서쪽으로 빠지는 경로")]
    [SerializeField]
    private Transform[] _lunaExitPath;

    [Tooltip("10번: 하루가 루나를 따라 한 칸 움직이는 방향")]
    [SerializeField]
    private Vector2 _haruFollowStep = Vector2.left;

    [Header("대화 구도 - 시선 고정")]
    [Tooltip("구현 문서 1장: 어디로 T1에 들어오든 항상 같은 구도가 되도록 시선을 고정한다. "
        + "(0,0)으로 두면 서로의 위치에서 방향을 계산한다.")]
    [SerializeField]
    private Vector2 _haruTalkFacing = Vector2.left;

    [SerializeField]
    private Vector2 _lunaTalkFacing = Vector2.right;

    [Header("연출 - 정원 색조")]
    [Tooltip("정원 전체를 비추는 Global Light 2D. "
        + "씬에 세팅해둔 값이 0단계(기본)이므로 여기서 따로 지정하지 않는다.")]
    [SerializeField]
    private Light2D _gardenLight;

    [Tooltip("Color Adjustments 오버라이드가 들어 있는 Global Volume. "
        + "비워두면 채도는 건드리지 않고 조명만 바뀐다.")]
    [SerializeField]
    private Volume _gardenVolume;

    [Tooltip("5번: 석상 앞 대화 시작과 함께 적용")]
    [SerializeField]
    private LightStage _lightStage1 = new LightStage
    {
        intensity = 0.75f,
        color = new Color(0.86f, 0.86f, 0.90f),
        saturation = -40f,
    };

    [Tooltip("8번: '곧 따라올 거야' 뒤 휴지와 함께 적용")]
    [SerializeField]
    private LightStage _lightStage2 = new LightStage
    {
        intensity = 0.5f,
        color = new Color(0.80f, 0.80f, 0.84f),
        saturation = -75f,
    };

    [SerializeField]
    private float _lightFadeDuration = 1.5f;

    // Volume 프로파일에서 찾아둔 Color Adjustments 오버라이드
    private ColorAdjustments _colorAdjustments;

    [Header("연출 - 사운드")]
    [Tooltip("낮은 영사기 작동음. 5번(석상 앞 이동)부터 씬이 끝날 때까지 SoundManager의 루프 채널로 깔린다.")]
    [SerializeField]
    private AudioClip _projectorLoopClip;

    [Range(0f, 1f)]
    [Tooltip("영사기음 볼륨. 대사를 덮지 않게 낮게 깐다.")]
    [SerializeField]
    private float _projectorVolume = 0.35f;

    [Tooltip("영사기음이 서서히 올라오는 시간. 0이면 바로 최대 볼륨.")]
    [SerializeField]
    private float _projectorFadeInDuration = 1.5f;

    [Tooltip("11번 페이드 아웃과 함께 영사기음이 잦아드는 시간")]
    [SerializeField]
    private float _projectorFadeOutDuration = 1.5f;

    [Header("연출 - 타이밍")]
    [Tooltip("8번: 색조를 한 단계 더 낮추기 전 휴지")]
    [SerializeField]
    private float _pauseBeforeTint2 = 2f;

    [Tooltip("9번: 봉투를 띄우기 전 휴지")]
    [SerializeField]
    private float _pauseBeforeLetter = 1f;

    [Tooltip("6번: 루나가 제자리걸음 하는 시간")]
    [SerializeField]
    private float _lunaStepInPlaceDuration = 1f;

    [Header("씬 전환")]
    [SerializeField]
    private string _nextSceneName = "Sequence8S#2";

    [SerializeField]
    private float _fadeOutDuration = 2f;

    // 타일 한 칸 이동에 걸리는 시간. PlayerMove_Test_Lerp / AstarEnemy와 같은 기준.
    private const float StepSeconds = 0.2f;

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
                    "[S8S1] Volume 프로파일에 Color Adjustments 오버라이드가 없어 채도를 바꿀 수 없다.",
                    this
                );
        }
    }

    protected override void ApplyWorldByState()
    {
        if (state == S8S1State.Finished)
        {
            // 세이브 복원으로 다시 들어온 경우: 연출을 재생하지 않고 결과만 맞춰둔다
            if (_t1Trigger != null)
                _t1Trigger.SetActive(false);

            ApplyLightStageImmediate(_lightStage2);
            return;
        }

        // 카메라 인트로가 끝나면 Sequence8Scene1Controller의 OnIntroFinished가
        // StartIntroDialogue()를 호출한다. 여기서는 T1만 잠가둔다.
        if (_t1Trigger != null)
            _t1Trigger.SetActive(false);

        // 0단계 조명은 씬에 세팅해둔 Light2D 값을 그대로 쓴다
    }

    /// <summary>카메라 인트로 종료 시 호출 (2번 - 하루의 첫 대사)</summary>
    public void StartIntroDialogue()
    {
        dialogueManager.StartDialogue("dialogue_1");
    }

    /// <summary>T1 접근 트리거에서 호출 (3번)</summary>
    public void OnPlayerEnteredT1()
    {
        if (state != S8S1State.None)
            return;

        state = S8S1State.Talking;
        PersistPuzzleState();

        if (_t1Trigger != null)
            _t1Trigger.SetActive(false);

        StartCoroutine(Co_LunaApproach());
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        // DialogueManager.EndDialogue()가 대화가 끝날 때마다 조작을 되돌려놓기 때문에
        // (DialogueManager.cs의 _playerMove.enabled = true)
        // 연출이 이어지는 구간에서는 여기서 다시 잠가야 한다.
        if (dialogueId != "dialogue_1")
            LockPlayer(true);

        switch (dialogueId)
        {
            // 2번: 첫 대사 이후 조작 활성화, T1 개방
            case "dialogue_1":
                LockPlayer(false);
                if (_t1Trigger != null)
                    _t1Trigger.SetActive(true);
                break;

            // 5번: 영사기음 + 석상 앞 이동 + 색조 1단계
            case "dialogue_18":
                StartCoroutine(Co_MoveToStatue());
                break;

            // 6번: 루나가 제자리걸음 후 다시 하루 앞에 선다
            case "dialogue_37":
                StartCoroutine(Co_LunaStepInPlace());
                break;

            // 7번: 하루가 추락 지점을 봤다가 다시 루나를 본다
            case "dialogue_42":
                StartCoroutine(Co_HaruLooksBack());
                break;

            // 8번: 휴지 후 색조 2단계
            case "dialogue_44":
                StartCoroutine(Co_PauseThenTint2());
                break;

            // 9번: 휴지 후 봉투 일러스트
            case "dialogue_68":
                StartCoroutine(Co_PauseThenLetter());
                break;

            // 10번: 루나가 D를 따라 서쪽으로, 하루는 한 칸 따라가다 멈춘다
            case "dialogue_76":
                StartCoroutine(Co_LunaLeaves());
                break;

            // 11번: 루나 소멸 후 페이드 아웃 → 8-2
            case "dialogue_82":
                state = S8S1State.Finished;
                PersistPuzzleState();
                StartCoroutine(Co_Outro());
                break;
        }
    }

    protected override void HandleOption(string text, string nextId) { }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    // ---------- 3번 ----------
    private IEnumerator Co_LunaApproach()
    {
        LockPlayer(true);

        // 어느 위치로 T1에 들어왔든 항상 같은 구도가 되도록 하루를 고정 위치로 맞춘다.
        // 각자 이동이 끝나는 즉시 시선을 돌린다. 상대를 기다렸다가 한꺼번에 돌리면
        // 기다리는 동안 마지막 이동 방향(하루는 오른쪽)을 보고 있어서 어색하다.
        Coroutine haruMove = StartCoroutine(Co_MoveThenFaceHaru(_haruTalkPath));
        Coroutine lunaMove = StartCoroutine(Co_MoveThenFaceLuna(_lunaApproachPath));

        yield return haruMove;
        yield return lunaMove;

        // 시선을 위치에서 계산하는 설정이면 둘 다 멈춘 지금 다시 한 번 맞춘다
        ApplyTalkFraming();

        dialogueManager.StartDialogue("dialogue_2");
    }

    // ---------- 5번 ----------
    private IEnumerator Co_MoveToStatue()
    {
        StartProjectorLoop();

        StartCoroutine(Co_FadeLight(_lightStage1));

        // 하루와 루나를 동시에 석상 앞으로 보낸다.
        // 먼저 도착한 쪽이 이동 방향을 본 채로 서 있지 않도록,
        // 각자 도착하는 즉시 석상(위쪽)을 바라보게 한다.
        Coroutine haruMove = StartCoroutine(Co_MoveThenFaceHaru(_haruStatuePath, Vector2.up));
        Coroutine lunaMove = StartCoroutine(Co_MoveThenFaceLuna(_lunaStatuePath, Vector2.up));

        yield return haruMove;
        yield return lunaMove;

        dialogueManager.StartDialogue("dialogue_19");
    }

    // ---------- 6번 ----------
    private IEnumerator Co_LunaStepInPlace()
    {
        SetWalking(_lunaAnimator, _lunaAnimParams, true, _lunaMoveSpeed);
        yield return new WaitForSeconds(_lunaStepInPlaceDuration);
        SetWalking(_lunaAnimator, _lunaAnimParams, false, _lunaMoveSpeed);

        ApplyTalkFraming();

        dialogueManager.StartDialogue("dialogue_38");
    }

    // ---------- 7번 ----------
    private IEnumerator Co_HaruLooksBack()
    {
        if (_playerMove != null)
        {
            _playerMove.SetFacing(Vector2.down);
            yield return new WaitForSeconds(0.8f);
        }

        ApplyTalkFraming();

        dialogueManager.StartDialogue("dialogue_43");
    }

    // ---------- 8번 ----------
    private IEnumerator Co_PauseThenTint2()
    {
        yield return new WaitForSeconds(_pauseBeforeTint2);
        yield return Co_FadeLight(_lightStage2);

        dialogueManager.StartDialogue("dialogue_45");
    }

    // ---------- 9번 ----------
    private IEnumerator Co_PauseThenLetter()
    {
        yield return new WaitForSeconds(_pauseBeforeLetter);

        // 봉투 일러스트는 dialogue_69의 imageName으로 표시된다
        dialogueManager.StartDialogue("dialogue_69");
    }

    // ---------- 10번 ----------
    private IEnumerator Co_LunaLeaves()
    {
        // 루나가 D를 따라 서쪽으로 이동하기 시작
        Coroutine lunaExit = StartCoroutine(Co_MoveLuna(_lunaExitPath));

        // 하루는 한 칸 따라갔다가 멈춘다
        if (_playerMove != null)
        {
            Vector2 step = SnapTo4Dir(_haruFollowStep);
            Vector3 from = _playerMove.transform.position;
            Vector3 oneStep = from + (Vector3)step;

            yield return Co_GridMove(
                _playerMove.transform,
                _playerMove.animator,
                _haruAnimParams,
                oneStep,
                _haruMoveSpeed
            );

            SetWalking(_playerMove.animator, _haruAnimParams, false, _haruMoveSpeed);

            // 실제로 움직인 방향을 그대로 바라본다.
            // (인스펙터 값과 최종 시선이 어긋나지 않도록 이동 결과에서 뽑는다)
            Vector2 moved = _playerMove.transform.position - from;
            _playerMove.SetFacing(moved.sqrMagnitude > 0.0001f ? moved : step);
        }

        dialogueManager.StartDialogue("dialogue_77");

        yield return lunaExit;
    }

    // ---------- 11번 ----------
    private IEnumerator Co_Outro()
    {
        // 루나가 이미 경로 끝(어두운 구역)에 도착해 있다면 그대로 감춘다
        if (_luna != null)
            _luna.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // 화면이 어두워지는 동안 같이 잦아들게 둔다. 여기서 끊으면 소리만 뚝 끊긴다.
        StopProjectorLoop();

        CutsceneManager.Instance.FadeToBlack(
            () =>
            {
                if (!string.IsNullOrEmpty(_nextSceneName))
                    SceneManager.LoadScene(_nextSceneName);
            },
            _fadeOutDuration
        );
    }

    // ---------- 사운드 ----------
    private void StartProjectorLoop()
    {
        if (_projectorLoopClip == null)
        {
            Debug.LogWarning("[S8S1] 영사기 작동음 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlayLoopSFX(
            _projectorLoopClip,
            _projectorVolume,
            _projectorFadeInDuration
        );
    }

    private void StopProjectorLoop()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopLoopSFX(_projectorFadeOutDuration);
    }

    // ---------- 이동 ----------
    /// <summary>
    /// 경로 이동이 끝나는 즉시 지정한 방향을 바라본다.
    /// facing이 (0,0)이면 대화 구도 기본 시선을 쓴다.
    /// </summary>
    private IEnumerator Co_MoveThenFaceHaru(Transform[] path, Vector2 facing = default)
    {
        yield return Co_MoveHaru(path);

        if (facing == Vector2.zero)
            ApplyHaruTalkFacing();
        else if (_playerMove != null)
            _playerMove.SetFacing(facing);
    }

    private IEnumerator Co_MoveThenFaceLuna(Transform[] path, Vector2 facing = default)
    {
        yield return Co_MoveLuna(path);

        if (facing == Vector2.zero)
            ApplyLunaTalkFacing();
        else
            SetFacing(_lunaAnimator, _lunaAnimParams, facing);
    }

    private IEnumerator Co_MoveLuna(Transform[] path)
    {
        yield return Co_MovePath(_luna, _lunaAnimator, _lunaAnimParams, path, _lunaMoveSpeed);
    }

    private IEnumerator Co_MoveHaru(Transform[] path)
    {
        if (_playerMove == null)
            yield break;

        yield return Co_MovePath(
            _playerMove.transform,
            _playerMove.animator,
            _haruAnimParams,
            path,
            _haruMoveSpeed
        );
    }

    private IEnumerator Co_MovePath(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        Transform[] path,
        float speed
    )
    {
        if (mover == null || path == null)
            yield break;

        foreach (Transform wp in path)
        {
            if (wp == null)
                continue;

            yield return Co_GridMove(mover, animator, anim, wp.position, speed);
        }

        // 도착. 마지막 웨이포인트까지 온 뒤에야 걷기를 끈다.
        SetWalking(animator, anim, false, speed);
    }

    /// <summary>
    /// 목표 지점까지 타일 단위 4방향으로 이동한다.
    /// 가로를 먼저 맞춘 뒤 세로를 맞추는 ㄱ자 경로.
    /// 대각선으로 가로지르지 않으므로, 꺾이는 지점에 장애물이 있으면
    /// 웨이포인트를 나눠서 찍어야 한다.
    /// </summary>
    private IEnumerator Co_GridMove(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        Vector3 destination,
        float speed
    )
    {
        if (mover == null)
            yield break;

        yield return Co_GridMoveAxis(mover, animator, anim, destination.x, true, speed);
        yield return Co_GridMoveAxis(mover, animator, anim, destination.y, false, speed);
    }

    private IEnumerator Co_GridMoveAxis(
        Transform mover,
        Animator animator,
        ActorAnimParams anim,
        float targetValue,
        bool horizontal,
        float speed
    )
    {
        float current = horizontal ? mover.position.x : mover.position.y;
        float diff = targetValue - current;

        if (Mathf.Abs(diff) < 0.001f)
            yield break;

        float sign = Mathf.Sign(diff);
        Vector3 stepDir = horizontal ? new Vector3(sign, 0f, 0f) : new Vector3(0f, sign, 0f);

        SetFacing(animator, anim, stepDir);
        SetWalking(animator, anim, true, speed);

        float stepSeconds = StepSeconds / Mathf.Max(0.01f, speed);
        int fullSteps = Mathf.FloorToInt(Mathf.Abs(diff) + 0.001f);

        for (int i = 0; i < fullSteps; i++)
            yield return Co_SingleStep(mover, mover.position + stepDir, stepSeconds);

        // 타일에 딱 떨어지지 않는 나머지가 있으면 마지막에 한 번 더 보정
        Vector3 finalPos = mover.position;
        if (horizontal)
            finalPos.x = targetValue;
        else
            finalPos.y = targetValue;

        if ((finalPos - mover.position).sqrMagnitude > 0.000001f)
            yield return Co_SingleStep(mover, finalPos, stepSeconds);

        // 걷기를 여기서 끄면 ㄱ자로 꺾일 때마다 한 프레임씩 서 있는 게 보인다.
        // 경로가 전부 끝나는 지점(Co_MovePath / 호출부)에서 한 번만 끈다.
    }

    private IEnumerator Co_SingleStep(Transform mover, Vector3 to, float duration)
    {
        Vector3 from = mover.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            mover.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mover.position = to;
    }

    // ---------- 방향 ----------
    /// <summary>
    /// 대화 구도를 잡는다. 인스펙터에 시선을 지정해두면 그 값을 그대로 쓰고,
    /// (0,0)이면 서로의 위치에서 방향을 계산한다.
    ///
    /// 위치 계산에만 의존하면 참조가 하나라도 비었을 때 조용히 넘어가서
    /// 직전 이동 방향(대개 가로)이 그대로 남는다. 그래서 고정값을 기본으로 둔다.
    /// </summary>
    private void ApplyTalkFraming()
    {
        ApplyHaruTalkFacing();
        ApplyLunaTalkFacing();
    }

    private void ApplyHaruTalkFacing()
    {
        if (_playerMove == null)
        {
            Debug.LogWarning("[S8S1] _playerMove가 비어 있어 하루 시선을 잡을 수 없다.", this);
            return;
        }

        Vector2 facing = _haruTalkFacing;

        if (facing == Vector2.zero)
        {
            if (_luna == null)
            {
                Debug.LogWarning("[S8S1] _luna가 비어 있고 하루 시선 고정값도 없다.", this);
                return;
            }

            facing = _luna.position - _playerMove.transform.position;
        }

        _playerMove.SetFacing(facing);
    }

    private void ApplyLunaTalkFacing()
    {
        if (_lunaAnimator == null)
            return;

        Vector2 facing = _lunaTalkFacing;

        if (facing == Vector2.zero)
        {
            if (_luna == null || _playerMove == null)
            {
                Debug.LogWarning("[S8S1] 루나 시선을 계산할 참조가 비어 있다.", this);
                return;
            }

            facing = _playerMove.transform.position - _luna.position;
        }

        SetFacing(_lunaAnimator, _lunaAnimParams, facing);
    }

    private void SetFacing(Animator animator, ActorAnimParams anim, Vector2 dir)
    {
        if (animator == null || dir == Vector2.zero)
            return;

        Vector2 snapped = SnapTo4Dir(dir);

        if (!string.IsNullOrEmpty(anim.dirX))
            animator.SetFloat(anim.dirX, snapped.x);

        if (!string.IsNullOrEmpty(anim.dirY))
            animator.SetFloat(anim.dirY, snapped.y);
    }

    /// <summary>애니메이터가 4방향 기준이라 대각선은 허용하지 않는다.</summary>
    private static Vector2 SnapTo4Dir(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return Vector2.down;

        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }

    private void SetWalking(Animator animator, ActorAnimParams anim, bool walking, float speed)
    {
        if (animator == null)
            return;

        // 컨트롤러에 없는 파라미터를 건드리면 경고만 나고 아무 일도 일어나지 않는다
        if (!string.IsNullOrEmpty(anim.animSpeed))
            animator.SetFloat(anim.animSpeed, speed);

        if (!string.IsNullOrEmpty(anim.walk))
            animator.SetBool(anim.walk, walking);
    }

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
        float fromSaturation =
            _colorAdjustments != null ? _colorAdjustments.saturation.value : 0f;

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
                _colorAdjustments.saturation.value =
                    Mathf.Lerp(fromSaturation, to.saturation, t);

            yield return null;
        }

        ApplyLightStageImmediate(to);
    }
}
