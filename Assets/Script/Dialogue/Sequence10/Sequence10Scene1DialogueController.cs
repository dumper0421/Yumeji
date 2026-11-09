using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum S10S1State
{
    None,        // 씬 시작 직후. 틸트 다운 전
    Common,      // 5-1 공통 대화
    StayEnding,  // 5-2 루나와 함께 남는다 → 10-2 (엔딩 1)
    LeaveEnding, // 5-3 현실로 돌아간다 → 10-3 (엔딩 2)
}

/// <summary>
/// 시퀀스10 씬1 "영사실" 컨트롤러.
/// 불타는 소형 상영관(3-4 맵)에서 루나와 마지막 대화를 나누고 두 엔딩 중 하나로 갈라진다.
///
/// 진입부터 분기 종료까지 이동/조사는 막고 대사 넘김과 선택지만 받는다. (기획서 4-2)
/// 불은 대화 중에도 계속 타고, 선택 제한 시간이나 화재 피해 판정은 없다.
/// </summary>
public class Sequence10Scene1DialogueController : DialogueController<S10S1State>
{
    [Header("하루")]
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    [SerializeField]
    private ActorAnimParams _haruAnimParams = new ActorAnimParams();

    [Header("루나")]
    [SerializeField]
    private Transform _luna;

    [SerializeField]
    private Animator _lunaAnimator;

    [Tooltip("루나 애니메이터는 DirX / Diry / IsWalking (AnimSpeed 없음)")]
    [SerializeField]
    private ActorAnimParams _lunaAnimParams = new ActorAnimParams
    {
        dirX = "DirX",
        dirY = "Diry",
        walk = "IsWalking",
        animSpeed = "",
    };

    [Tooltip("시작할 때 루나가 보는 방향. 기획 배치도에서는 통로 쪽(오른쪽)을 본다.")]
    [SerializeField]
    private Vector2 _lunaStartFacing = Vector2.right;

    [Header("케이와 필름")]
    [Tooltip("쓰러진 케이 위에 놓인 필름. 엔딩 1에서 하루가 집어 든다.")]
    [SerializeField]
    private Transform _film;

    [Tooltip("하루가 필름을 든 위치 (하루 기준 월드 오프셋)")]
    [SerializeField]
    private Vector3 _filmHoldOffset = new Vector3(0f, -0.1f, 0f);

    [Header("지점")]
    [SerializeField]
    private Transform _haruStart;

    [SerializeField]
    private Transform _lunaStart;

    [Tooltip("5-1: 루나가 내려와 하루 옆에 서는 자리")]
    [SerializeField]
    private Transform _lunaBesideHaru;

    [Tooltip("5-2: 루나가 계단을 올라가 서는 자리 (두 걸음 위)")]
    [SerializeField]
    private Transform _lunaStepUp;

    [Tooltip("5-2: 하루가 걸어 올라가 서는 쓰러진 케이 바로 앞")]
    [SerializeField]
    private Transform _haruBeforeKei;

    [Tooltip("5-3: 하루가 출구 쪽으로 몇 걸음 걷다가 멈추는 경로. 마지막 지점에서 루나를 돌아본다.")]
    [SerializeField]
    private Transform[] _haruLeavePath;

    [Tooltip("5-3: 마지막 대화 후 출구로 빠져나가는 경로")]
    [SerializeField]
    private Transform[] _haruExitPath;

    [Header("카메라")]
    [Tooltip("고정 구도 카메라. Body 없이 transform 위치가 곧 화면 중심이다.")]
    [SerializeField]
    private CinemachineVirtualCamera _vcam;

    [Tooltip("틸트 다운 시작 (맵 윗부분)")]
    [SerializeField]
    private Transform _camTop;

    [Tooltip("공통 대화 구도: 하루, 루나, 케이와 필름이 함께 보이는 위치")]
    [SerializeField]
    private Transform _camTalk;

    [Tooltip("5-3: 하루가 출구로 나갈 때 출구까지 보이도록 옮기는 위치")]
    [SerializeField]
    private Transform _camExit;

    [Header("불")]
    [Tooltip("시간이 지나며 하나씩 붙는 불. 가장자리에서 안쪽 순서로 넣을 것.")]
    [SerializeField]
    private GameObject[] _spreadFires;

    [SerializeField]
    private float _spreadInterval = 6f;

    [Tooltip("5-2: 하루 몸에 붙는 불")]
    [SerializeField]
    private GameObject _haruFire;

    [Tooltip("5-2: 이어서 루나 몸에 붙는 불")]
    [SerializeField]
    private GameObject _lunaFire;

    [Tooltip("몸에 붙는 불 위치. 캐릭터 스프라이트의 몸 중심 기준 월드 오프셋 (하루/루나 피봇이 달라서 transform이 아니라 몸 중심을 쓴다)")]
    [SerializeField]
    private Vector3 _bodyFireOffset = new Vector3(0f, -0.2f, 0f);

    [SerializeField]
    private float _lunaFireDelay = 1f;

    [Header("화면")]
    [Tooltip("5-2 화이트 아웃용 흰 Image. 씬에는 알파 0으로 꺼둔다.")]
    [SerializeField]
    private Image _whiteOverlay;

    [Tooltip("페이드 인 2초 (틸트 다운과 동시)")]
    [SerializeField]
    private float _fadeInDuration = 2f;

    [Tooltip("엔딩 1 화이트 아웃 약 3초")]
    [SerializeField]
    private float _whiteOutDuration = 3f;

    [Tooltip("엔딩 2 페이드 아웃 3초")]
    [SerializeField]
    private float _fadeOutDuration = 3f;

    [Header("사운드")]
    [Tooltip("불타는 소리. 씬 진입부터 대화/선택지 내내 유지")]
    [SerializeField]
    private AudioClip _burningLoop;

    [Range(0f, 1f)]
    [SerializeField]
    private float _burningVolume = 0.6f;

    [Tooltip("영사기 작동음. 엔딩 1 화이트 아웃이 시작되면 재생")]
    [SerializeField]
    private AudioClip _projectorLoop;

    [Tooltip("엔딩 2: 암전 뒤 불타는 소리를 유지하는 시간")]
    [SerializeField]
    private float _burningHoldAfterBlack = 1.5f;

    [SerializeField]
    private float _burningFadeOut = 2f;

    [Header("연출 타이밍")]
    [Tooltip("'대화가 잠시 끊긴다' 구간의 정적")]
    [SerializeField]
    private float _beatPause = 1f;

    [Tooltip("1 = 플레이어 걷기와 동일. 마지막 장면이라 조금 느리게.")]
    [SerializeField]
    private float _walkSpeed = 0.6f;

    [Tooltip("5-3: 하루가 루나를 바라보기만 하는 시간")]
    [SerializeField]
    private float _lookAtLunaHold = 1.5f;

    [Header("씬 전환")]
    [SerializeField]
    private string _stayEndingScene = "Sequence10S#2";

    [SerializeField]
    private string _leaveEndingScene = "Sequence10S#3";

    private bool _sequenceRunning;

    /// <summary>대사 사이 연출(이동/시선/불/화면 전환)이 진행 중인지</summary>
    public bool IsSequenceRunning => _sequenceRunning;

    // ================================================================ 상태 적용

    protected override void ApplyWorldByState()
    {
        LockPlayer(true);
        SetWhite(0f);

        if (_haruFire != null)
            _haruFire.SetActive(false);

        if (_lunaFire != null)
            _lunaFire.SetActive(false);

        if (_spreadFires != null)
        {
            foreach (GameObject f in _spreadFires)
            {
                if (f != null)
                    f.SetActive(false);
            }
        }

        // 분기 후 저장된 경우: 연출을 다시 보여주지 않고 해당 엔딩으로 넘긴다
        if (state == S10S1State.StayEnding || state == S10S1State.LeaveEnding)
        {
            StartCoroutine(Co_RestoreToEnding());
            return;
        }

        // 시작 배치: 통로에 하루와 루나, 그 앞(위)에 쓰러진 케이와 필름.
        // 루나가 꺼져 있으면 애니메이터가 방향을 받지 않고 몸에 붙는 불도 보이지 않는다.
        if (_luna != null)
            _luna.gameObject.SetActive(true);

        Place(_playerMove != null ? _playerMove.transform : null, _haruStart);
        Place(_luna, _lunaStart);
        FaceHaru(Vector2.up);
        TileActorMover.SetFacing(_lunaAnimator, _lunaAnimParams, _lunaStartFacing);

        if (_vcam != null && _camTop != null)
            MoveCamera(_camTop.position);

        StartCoroutine(Co_Intro());
    }

    private IEnumerator Co_RestoreToEnding()
    {
        yield return null;
        LoadEnding(state == S10S1State.StayEnding ? _stayEndingScene : _leaveEndingScene);
    }

    // ================================================================ 5-1 공통 대화

    private IEnumerator Co_Intro()
    {
        // CutsceneManager.Start()가 화면을 검게 덮으므로 한 프레임 넘긴다
        yield return null;

        PlayBurning();
        StartCoroutine(Co_SpreadFires());

        // 페이드 인 2초와 동시에 맵 윗부분에서 대화 구도까지 틸트 다운
        bool faded = false;
        CutsceneManager.Instance.FadeFromBlack(() => faded = true, _fadeInDuration);

        if (_vcam != null && _camTop != null && _camTalk != null)
            yield return Co_PanCamera(_camTop.position, _camTalk.position, _fadeInDuration);

        yield return new WaitUntil(() => faded);

        state = S10S1State.Common;
        PersistPuzzleState();

        dialogueManager.StartDialogue("c1_1");
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        // DialogueManager.EndDialogue가 대화가 끝날 때마다 조작을 돌려주므로 매번 다시 잠근다
        LockPlayer(true);

        switch (dialogueId)
        {
            // 대화가 잠시 끊기고 루나가 하루 옆으로 내려와 마주 본다
            case "c1_4":
                StartCoroutine(Co_LunaComesDown());
                break;

            // 대화가 잠시 끊기고 하루만 케이와 필름, 영사기 쪽(위)으로 시선을 돌린다
            case "c2_4":
                StartCoroutine(Co_HaruLooksUp());
                break;

            // 5-2: 루나가 계단을 한 칸(두 걸음) 올라간 뒤 한 마디
            case "stay_2":
                StartCoroutine(Co_LunaStepsUp());
                break;

            // 5-2: 하루가 케이 앞에서 필름을 집어 들고 불이 붙는다 → 화이트 아웃
            case "stay_3":
                StartCoroutine(Co_StayEnding());
                break;

            // 5-3: 출구로 몇 걸음 걷다가 멈춰 루나를 돌아본다
            case "leave_2":
                StartCoroutine(Co_LeaveFirstSteps());
                break;

            // 5-3: 잠시 바라보다가 출구로 빠져나가고 페이드 아웃
            case "leave_5":
                StartCoroutine(Co_LeaveEnding());
                break;
        }
    }

    /// <summary>
    /// 선택지는 hasIntermediateAction으로 두고 여기서 대사를 이어간다.
    /// (5-3은 대사 전에 하루가 먼저 뒤돌아야 한다)
    /// </summary>
    protected override void HandleOption(string text, string nextId)
    {
        switch (nextId)
        {
            case "stay_1":
                state = S10S1State.StayEnding;
                PersistPuzzleState();
                StartCoroutine(Co_StartNextFrame("stay_1", null));
                break;

            case "leave_1":
                state = S10S1State.LeaveEnding;
                PersistPuzzleState();
                // 하루가 뒤돌아 아래를 바라본다
                StartCoroutine(Co_StartNextFrame("leave_1", () => FaceHaru(Vector2.down)));
                break;
        }
    }

    protected override void OnPuzzleComplete() { }

    protected override void TryProgress() { }

    private IEnumerator Co_StartNextFrame(string dialogueId, System.Action before)
    {
        // 선택지 버튼 콜백 안에서 바로 새 대화를 열면 DialogueManager가 옵션을 정리하는 순서와 꼬인다
        yield return null;

        before?.Invoke();

        if (before != null && _beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        dialogueManager.StartDialogue(dialogueId);
    }

    private IEnumerator Co_LunaComesDown()
    {
        _sequenceRunning = true;

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause);

        if (_luna != null && _lunaBesideHaru != null)
            yield return TileActorMover.MoveTo(_luna, _lunaAnimator, _lunaAnimParams, _lunaBesideHaru.position, _walkSpeed);

        // 루나와 하루가 마주 본다
        FaceEachOther();

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        _sequenceRunning = false;
        dialogueManager.StartDialogue("c2_1");
    }

    private IEnumerator Co_HaruLooksUp()
    {
        _sequenceRunning = true;

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause);

        FaceHaru(Vector2.up);

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause);

        _sequenceRunning = false;
        dialogueManager.StartDialogue("c3_1");
    }

    // ================================================================ 5-2 루나와 함께 남는다

    private IEnumerator Co_LunaStepsUp()
    {
        _sequenceRunning = true;

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause);

        if (_luna != null && _lunaStepUp != null)
            yield return TileActorMover.MoveTo(_luna, _lunaAnimator, _lunaAnimParams, _lunaStepUp.position, _walkSpeed);

        // 올라간 뒤 하루를 내려다본다
        TileActorMover.SetFacing(_lunaAnimator, _lunaAnimParams, Vector2.down);

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        _sequenceRunning = false;
        dialogueManager.StartDialogue("stay_3");
    }

    private IEnumerator Co_StayEnding()
    {
        _sequenceRunning = true;

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        // 하루가 계단을 걸어 올라가 쓰러진 케이 앞에 선다
        if (_playerMove != null && _haruBeforeKei != null)
        {
            yield return TileActorMover.MoveTo(
                _playerMove.transform,
                _playerMove.animator,
                _haruAnimParams,
                _haruBeforeKei.position,
                _walkSpeed
            );
        }

        FaceHaru(Vector2.up);

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause);

        // 필름을 집어 들자 하루의 몸에 불이 붙고, 이어 루나에게도
        PickUpFilm();

        yield return new WaitForSeconds(0.4f);
        Ignite(_haruFire, _playerMove != null ? _playerMove.transform : null);

        if (_lunaFireDelay > 0f)
            yield return new WaitForSeconds(_lunaFireDelay);

        Ignite(_lunaFire, _luna);

        yield return new WaitForSeconds(1f);

        // 영사기 작동음과 함께 화이트 아웃. 빛 전환 중 불타는 소리는 멀어진다.
        PlayProjector();
        StopBurning(_whiteOutDuration);
        yield return Co_FadeWhite(1f, _whiteOutDuration);

        yield return new WaitForSeconds(0.5f);

        LoadEnding(_stayEndingScene);
    }

    private void PickUpFilm()
    {
        if (_film == null || _playerMove == null)
            return;

        _film.SetParent(_playerMove.transform, true);
        _film.position = _playerMove.transform.position + _filmHoldOffset;

        // 들고 있는 동안은 하루 위에 그린다
        var sr = _film.GetComponent<SpriteRenderer>();
        var haruSr = _playerMove.GetComponent<SpriteRenderer>();
        if (sr != null && haruSr != null)
        {
            sr.sortingLayerID = haruSr.sortingLayerID;
            sr.sortingOrder = haruSr.sortingOrder + 1;
        }
    }

    // ================================================================ 5-3 현실로 돌아간다

    private IEnumerator Co_LeaveFirstSteps()
    {
        _sequenceRunning = true;

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        // 출구로 몇 걸음 걷다가 멈춘다
        if (_playerMove != null && _haruLeavePath != null && _haruLeavePath.Length > 0)
        {
            yield return TileActorMover.MovePath(
                _playerMove.transform,
                _playerMove.animator,
                _haruAnimParams,
                _haruLeavePath,
                _walkSpeed
            );
        }

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        // 멈춰 서서 루나 쪽을 돌아본다
        FaceHaruToward(_luna);
        TileActorMover.SetFacing(_lunaAnimator, _lunaAnimParams, DirTo(_luna, _playerMove != null ? _playerMove.transform : null));

        if (_beatPause > 0f)
            yield return new WaitForSeconds(_beatPause * 0.5f);

        _sequenceRunning = false;
        dialogueManager.StartDialogue("leave_3");
    }

    private IEnumerator Co_LeaveEnding()
    {
        _sequenceRunning = true;

        // 하루가 잠시 루나를 바라보기만 한다
        if (_lookAtLunaHold > 0f)
            yield return new WaitForSeconds(_lookAtLunaHold);

        // 등을 돌려 출구로. 출구까지 보이도록 카메라를 함께 옮긴다.
        Coroutine pan = null;
        if (_vcam != null && _camExit != null)
            pan = StartCoroutine(Co_PanCamera(_vcam.transform.position, _camExit.position, ExitWalkSeconds()));

        if (_playerMove != null && _haruExitPath != null && _haruExitPath.Length > 0)
        {
            yield return TileActorMover.MovePath(
                _playerMove.transform,
                _playerMove.animator,
                _haruAnimParams,
                _haruExitPath,
                _walkSpeed
            );
        }

        if (pan != null)
            yield return pan;

        // 빠져나가면 페이드 아웃 3초. 암전 후 불타는 소리를 잠시 유지했다가 줄인다.
        bool done = false;
        CutsceneManager.Instance.FadeToBlack(() => done = true, _fadeOutDuration);
        yield return new WaitUntil(() => done);

        if (_burningHoldAfterBlack > 0f)
            yield return new WaitForSeconds(_burningHoldAfterBlack);

        StopBurning(_burningFadeOut);

        if (_burningFadeOut > 0f)
            yield return new WaitForSeconds(_burningFadeOut);

        LoadEnding(_leaveEndingScene);
    }

    private float ExitWalkSeconds()
    {
        if (_playerMove == null || _haruExitPath == null)
            return 1f;

        float tiles = 0f;
        Vector3 prev = _playerMove.transform.position;

        foreach (Transform p in _haruExitPath)
        {
            if (p == null)
                continue;

            tiles += Mathf.Abs(p.position.x - prev.x) + Mathf.Abs(p.position.y - prev.y);
            prev = p.position;
        }

        return Mathf.Max(0.5f, tiles * TileActorMover.StepSeconds / Mathf.Max(0.01f, _walkSpeed));
    }

    // ================================================================ 불

    /// <summary>가장자리에서 시작한 불이 대화 내내 조금씩 번진다</summary>
    private IEnumerator Co_SpreadFires()
    {
        if (_spreadFires == null)
            yield break;

        foreach (GameObject f in _spreadFires)
        {
            if (f == null)
                continue;

            yield return new WaitForSeconds(_spreadInterval);
            f.SetActive(true);
        }
    }

    private void Ignite(GameObject fire, Transform target)
    {
        if (fire == null || target == null)
            return;

        fire.transform.SetParent(target, true);
        fire.transform.position = BodyCenter(target) + _bodyFireOffset;
        fire.SetActive(true);
    }

    // ================================================================ 시선

    private void FaceEachOther()
    {
        if (_playerMove == null || _luna == null)
            return;

        FaceHaruToward(_luna);
        TileActorMover.SetFacing(_lunaAnimator, _lunaAnimParams, DirTo(_luna, _playerMove.transform));
    }

    private void FaceHaru(Vector2 dir)
    {
        if (_playerMove != null)
            _playerMove.SetFacing(dir);
    }

    private void FaceHaruToward(Transform target)
    {
        if (_playerMove != null && target != null)
            _playerMove.SetFacing(DirTo(_playerMove.transform, target));
    }

    /// <summary>
    /// 하루(피봇 가운데)와 루나(피봇 발밑)는 transform 기준이 달라서,
    /// 서로 보는 방향은 실제로 그려지는 몸 중심끼리 비교한다.
    /// </summary>
    private static Vector2 DirTo(Transform from, Transform to)
    {
        if (from == null || to == null)
            return Vector2.down;

        Vector3 d = BodyCenter(to) - BodyCenter(from);
        return TileActorMover.SnapTo4Dir(new Vector2(d.x, d.y));
    }

    private static Vector3 BodyCenter(Transform t)
    {
        var sr = t.GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.bounds.center : t.position;
    }

    // ================================================================ 카메라

    private IEnumerator Co_PanCamera(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            MoveCamera(Vector3.Lerp(from, to, t));
            yield return null;
        }

        MoveCamera(to);
    }

    private void MoveCamera(Vector3 p)
    {
        if (_vcam == null)
            return;

        _vcam.transform.position = new Vector3(p.x, p.y, _vcam.transform.position.z);
    }

    // ================================================================ 화면 / 씬 전환

    private void SetWhite(float alpha)
    {
        if (_whiteOverlay == null)
            return;

        _whiteOverlay.color = new Color(1f, 1f, 1f, alpha);
        _whiteOverlay.gameObject.SetActive(alpha > 0f);
    }

    private IEnumerator Co_FadeWhite(float to, float duration)
    {
        if (_whiteOverlay == null)
            yield break;

        _whiteOverlay.gameObject.SetActive(true);
        float from = _whiteOverlay.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetWhite(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetWhite(to);
    }

    private void LoadEnding(string sceneName)
    {
        _sequenceRunning = false;

        if (string.IsNullOrEmpty(sceneName))
            return;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"[S10S1] 다음 씬 '{sceneName}'이 빌드 설정에 없어 현재 화면에서 멈춘다.", this);
    }

    // ================================================================ 사운드

    private void PlayBurning()
    {
        if (_burningLoop == null)
        {
            Debug.LogWarning("[S10S1] 불타는 소리 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayLoopSFX(_burningLoop, _burningVolume);
    }

    private static void StopBurning(float fadeOut)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopLoopSFX(fadeOut);
    }

    private void PlayProjector()
    {
        if (_projectorLoop == null)
        {
            Debug.LogWarning("[S10S1] 영사기 작동음 클립이 비어 있어 소리 없이 진행한다.", this);
            return;
        }

        // 불타는 소리(루프 SFX 채널)와 겹쳐야 해서 BGM 채널로 튼다
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(_projectorLoop);
    }

    // ================================================================ 기타

    private static void Place(Transform who, Transform where)
    {
        if (who == null || where == null)
            return;

        who.position = new Vector3(where.position.x, where.position.y, who.position.z);
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
}
