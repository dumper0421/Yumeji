using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 시퀀스8 씬1 오프닝 카메라 연출.
/// 중앙 석상(C)과 그 옆의 루나(L)를 먼저 보여준 뒤,
/// 아래로 내려오며 H 지점에 서 있는 하루(Player)에게 카메라를 돌려준다.
/// </summary>
public class Sequence8Scene1Controller : SceneController
{
    [Header("Cameras")]
    [Tooltip("연출 전용 카메라. Follow / Look At은 비워둘 것 (코드에서도 해제한다).")]
    [SerializeField]
    private CinemachineVirtualCamera _introCamera;

    [Tooltip("연출이 끝난 뒤 하루를 따라다닐 평상시 카메라 (씬의 Virtual Camera).")]
    [SerializeField]
    private CinemachineVirtualCamera _playerCamera;

    [Header("0. 암전에서 시작")]
    [Tooltip("CutsceneManager는 씬 시작 시 화면을 검게 덮는다. 석상을 비추며 밝아지는 시간.")]
    [SerializeField]
    private float _introFadeInDuration = 1.5f;

    [Header("1. 중앙 석상 & 루나 보여주기")]
    [Tooltip("석상과 루나가 한 화면에 잡히는 지점. 보통 석상(C)과 루나(L) 사이에 빈 오브젝트를 두고 연결한다.")]
    [SerializeField]
    private Transform _statueFocus;

    [Tooltip("석상을 비출 때의 화면 크기. 씬 기본값은 5. 키우면 더 넓게 잡히고, 팬 하는 동안 평상시 크기로 돌아온다.")]
    [SerializeField]
    private float _statueOrthoSize = 5f;

    [Tooltip("석상을 보여주고 있는 시간")]
    [SerializeField]
    private float _statueHoldDuration = 2f;

    [Header("2. 하루에게 내려오기")]
    [Tooltip("도착 지점. 비워두면 Player의 위치를 사용한다.")]
    [SerializeField]
    private Transform _haruFocus;

    [SerializeField]
    private float _panDuration = 3f;

    [SerializeField]
    private AnimationCurve _panEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("하루에 도착한 뒤 조작을 넘기기 전 잠깐 멈추는 시간")]
    [SerializeField]
    private float _arriveHoldDuration = 0.3f;

    [Header("3. 연출 종료 후")]
    [Tooltip("대사 시작 등 이어서 실행할 동작")]
    [SerializeField]
    private UnityEvent _onIntroFinished;

    protected override void Start()
    {
        base.Start();

        if (_panEase == null || _panEase.length == 0)
            _panEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        StartCoroutine(CoPlayIntro());
    }

    protected override void OnStopIntervalReached() { }

    private IEnumerator CoPlayIntro()
    {
        LockPlayer(true);

        // 연출 카메라를 석상 위치에 세팅하고, 평상시 화면 크기를 받아둔다
        float gameplayOrthoSize = SetUpIntroCamera();

        // CutsceneManager는 자기 Start()에서 화면을 검게 덮는다.
        // 컴포넌트 간 Start 순서가 보장되지 않으므로 한 프레임 넘긴 뒤에 걷어낸다.
        yield return null;
        CutsceneManager.Instance.FadeFromBlack(null, _introFadeInDuration);

        // 1. 중앙 석상과 루나를 보여준다
        if (_statueHoldDuration > 0f)
            yield return new WaitForSeconds(_statueHoldDuration);

        // 2. 하루에게 내려온다
        yield return CoPanToHaru(gameplayOrthoSize);

        if (_arriveHoldDuration > 0f)
            yield return new WaitForSeconds(_arriveHoldDuration);

        // 3. 평상시 카메라로 넘긴다. 이미 같은 위치/크기라 화면이 튀지 않는다.
        HandOverToPlayerCamera();

        LockPlayer(false);

        _onIntroFinished?.Invoke();
    }

    /// <summary>
    /// 연출 카메라를 켜고 석상 위치에 세운다.
    /// </summary>
    /// <returns>평상시 카메라의 화면 크기 (팬 종료 시 맞춰야 할 값)</returns>
    private float SetUpIntroCamera()
    {
        float gameplayOrthoSize = _statueOrthoSize;

        if (_playerCamera != null)
        {
            gameplayOrthoSize = _playerCamera.m_Lens.OrthographicSize;
            _playerCamera.gameObject.SetActive(false);
        }

        if (_introCamera == null)
            return gameplayOrthoSize;

        // Follow가 걸려 있으면 위치를 직접 제어할 수 없다
        _introCamera.Follow = null;
        _introCamera.LookAt = null;

        _introCamera.gameObject.SetActive(true);
        _introCamera.m_Lens.OrthographicSize = _statueOrthoSize;

        if (_statueFocus != null)
            MoveIntroCameraTo(_statueFocus.position);

        return gameplayOrthoSize;
    }

    private IEnumerator CoPanToHaru(float endOrthoSize)
    {
        if (_introCamera == null)
            yield break;

        Vector2 from = _introCamera.transform.position;
        Vector2 to = HaruPosition();
        float fromSize = _introCamera.m_Lens.OrthographicSize;

        if (_panDuration <= 0f)
        {
            MoveIntroCameraTo(to);
            _introCamera.m_Lens.OrthographicSize = endOrthoSize;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < _panDuration)
        {
            elapsed += Time.deltaTime;
            float k = _panEase.Evaluate(Mathf.Clamp01(elapsed / _panDuration));

            MoveIntroCameraTo(Vector2.Lerp(from, to, k));
            _introCamera.m_Lens.OrthographicSize = Mathf.Lerp(fromSize, endOrthoSize, k);

            yield return null;
        }

        MoveIntroCameraTo(to);
        _introCamera.m_Lens.OrthographicSize = endOrthoSize;
    }

    private void HandOverToPlayerCamera()
    {
        // 먼저 켠 뒤에 끄지 않으면 카메라가 하나도 없는 프레임이 생긴다
        if (_playerCamera != null)
            _playerCamera.gameObject.SetActive(true);

        if (_introCamera != null)
            _introCamera.gameObject.SetActive(false);
    }

    private Vector2 HaruPosition()
    {
        if (_haruFocus != null)
            return _haruFocus.position;

        if (Player != null)
            return Player.transform.position;

        return _introCamera.transform.position;
    }

    // 연출 카메라는 z를 유지한 채 xy만 움직인다
    private void MoveIntroCameraTo(Vector2 xy)
    {
        Vector3 p = _introCamera.transform.position;
        _introCamera.transform.position = new Vector3(xy.x, xy.y, p.z);
    }

    private void LockPlayer(bool locked)
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.enabled = !locked;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("Walking", false);
            playerAnimator.SetBool("Pushing", false);
        }
    }
}
