
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

public class Sequence9S1_BRoomController : MonoBehaviour
{
    [System.Serializable]
    public class RouteTile
    {
        [Tooltip("도플갱어가 밟을 타일의 중앙 위치")]
        public Transform tile;

        [Tooltip("해당 타일에 잔상을 표시할지 여부")]
        public bool showAfterimage = true;
    }

    [Header("References")]
    [SerializeField] private PlayerMove_Test_Lerp player;
    [SerializeField] private GameObject doppelganger;
    [SerializeField] private Animator doppelAnimator;

    [Header("Doppelganger Positions")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Movement Route")]
    [SerializeField]
    private List<RouteTile> routeTiles =
        new List<RouteTile>();

    [Header("Movement Settings")]
    [Tooltip("1칸 이동에 걸리는 시간. 기본 0.2초")]
    [SerializeField, Min(0.01f)]
    private float secondsPerTile = 0.2f;

    [Header("Afterimage")]
    [SerializeField] private GameObject afterimagePrefab;
    [SerializeField] private Transform afterimageParent;

    [Header("Camera Settings")]
    [SerializeField] private SceneController sceneController;

    [Tooltip("평상시 플레이어를 추적하는 카메라")]
    [SerializeField] private CinemachineVirtualCamera followCamera;

    [Tooltip("도플갱어 시연 중 방 전체를 보여주는 카메라")]
    [SerializeField] private CinemachineVirtualCamera demoCamera;

    [Tooltip("시연 카메라가 바라볼 방 중앙 지점")]
    [SerializeField] private Transform cameraCenter;

    [Tooltip("시연 카메라 전환 후 대기 시간")]
    [SerializeField, Min(0f)]
    private float cameraSettleTime = 0.3f;

    [Tooltip("플레이어 카메라 복귀 후 대기 시간")]
    [SerializeField, Min(0f)]
    private float cameraReturnTime = 0.3f;

    [Header("Audio")]
    [Tooltip("시연이 끝나고 플레이어 카메라로 돌아올 때 재생")]
    [SerializeField] private AudioClip demonstrationEndSFX;

    [Header("Testing")]
    [Tooltip("테스트용. 실제 방 입장 트리거를 연결한 후에는 해제")]
    [SerializeField] private bool playOnStartForTest = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onDemonstrationFinished;

    private readonly List<GameObject> spawnedAfterimages =
        new List<GameObject>();

    private bool isRunning;
    private bool playerWasEnabled;
    private bool playerWasMovable;

    public bool IsRunning => isRunning;
    public IReadOnlyList<RouteTile> CorrectPath => routeTiles;

    private void Awake()
    {
        if (doppelganger != null && doppelAnimator == null)
        {
            doppelAnimator =
                doppelganger.GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        if (playOnStartForTest)
            BeginDemonstration();
    }

    /// <summary>
    /// 도플갱어의 경로 시연을 시작한다.
    /// 텔레포트 완료 이벤트에서 호출할 수 있다.
    /// </summary>
    public void BeginDemonstration()
    {
        if (isRunning)
            return;

        if (player == null || doppelganger == null ||
            startPoint == null || exitPoint == null)
        {
            Debug.LogWarning(
                "[Sequence9S1_B] 필수 참조가 누락되었습니다.",
                this
            );
            return;
        }

        isRunning = true;
        StartCoroutine(DemonstrationCoroutine());
    }

    private IEnumerator DemonstrationCoroutine()
    {
        ClearAfterimages();

        // 1) 플레이어 상태 저장 및 조작 제한
        playerWasEnabled = player.enabled;
        playerWasMovable = player.canMove;

        player.StopMovement();
        player.enabled = false;

        // 2) 시연 카메라로 전환
        ChangeToDemoCamera();

        if (cameraSettleTime > 0f)
            yield return new WaitForSeconds(cameraSettleTime);

        // 3) 도플갱어 시작 위치 배치
        doppelganger.SetActive(true);

        Vector3 startPosition = startPoint.position;
        startPosition.z = doppelganger.transform.position.z;

        doppelganger.transform.position = startPosition;

        SetWalking(false);

        // 4) 지정된 경로를 순서대로 이동
        foreach (RouteTile step in routeTiles)
        {
            if (step.tile == null)
            {
                Debug.LogWarning(
                    "[Sequence9S1_B] 경로 타일이 비어 있습니다.",
                    this
                );
                continue;
            }

            yield return MoveTo(step.tile.position);

            // 선택한 타일에만 잔상 생성
            if (step.showAfterimage && afterimagePrefab != null)
            {
                GameObject afterimage = Instantiate(
                    afterimagePrefab,
                    step.tile.position,
                    Quaternion.identity,
                    afterimageParent
                );

                spawnedAfterimages.Add(afterimage);
            }
        }

        // 5) 출구 너머로 이동
        yield return MoveTo(exitPoint.position);

        SetWalking(false);

        // 6) 도플갱어 퇴장
        doppelganger.SetActive(false);

        // 7) 시연 종료 SFX
        if (demonstrationEndSFX != null &&
            SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(demonstrationEndSFX);
        }

        // 8) 카메라를 플레이어에게 복귀
        ChangeToFollowCamera();

        if (cameraReturnTime > 0f)
            yield return new WaitForSeconds(cameraReturnTime);

        // 9) 플레이어 조작 복구
        RestorePlayer();

        isRunning = false;

        // 10) 시연 종료 이벤트
        onDemonstrationFinished?.Invoke();
    }

    /// <summary>
    /// 도플갱어를 지정 지점으로 이동한다.
    /// 타일 간 거리와 secondsPerTile을 기준으로 이동한다.
    /// </summary>
    private IEnumerator MoveTo(Vector3 destination)
    {
        Transform target = doppelganger.transform;

        Vector3 start = target.position;
        destination.z = start.z;

        Vector3 difference = destination - start;

        if (difference.sqrMagnitude < 0.0001f)
            yield break;

        // 이동 방향 설정
        Vector2 direction = difference;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            direction = new Vector2(Mathf.Sign(direction.x), 0f);
        else
            direction = new Vector2(0f, Mathf.Sign(direction.y));

        if (doppelAnimator != null)
        {
            doppelAnimator.SetFloat("DirX", direction.x);
            doppelAnimator.SetFloat("DirY", direction.y);
        }

        // 이동 경로 중에는 Walk Tree 유지
        SetWalking(true);

        float distance = difference.magnitude;
        float duration = secondsPerTile * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            target.position = Vector3.Lerp(
                start,
                destination,
                elapsed / duration
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.position = destination;
    }

    private void SetWalking(bool walking)
    {
        if (doppelAnimator == null)
            return;

        doppelAnimator.SetBool("Walking", walking);
        doppelAnimator.SetFloat("AnimSpeed", 1f);
    }

    /// <summary>
    /// 방 전체를 보여주는 카메라로 변경한다.
    /// </summary>
    private void ChangeToDemoCamera()
    {
        if (sceneController == null || demoCamera == null)
            return;

        if (cameraCenter != null)
            demoCamera.Follow = cameraCenter;

        sceneController.ChangeCinemachineCamera(demoCamera);
    }

    /// <summary>
    /// 플레이어 추적 카메라로 복귀한다.
    /// </summary>
    private void ChangeToFollowCamera()
    {
        if (sceneController == null || followCamera == null)
            return;

        if (player != null)
            followCamera.Follow = player.transform;

        sceneController.ChangeCinemachineCamera(followCamera);
    }

    private void RestorePlayer()
    {
        if (player == null)
            return;

        player.canMove = playerWasMovable;
        player.enabled = playerWasEnabled;
    }

    /// <summary>
    /// 재시작 시 기존 잔상을 제거한다.
    /// </summary>
    private void ClearAfterimages()
    {
        foreach (GameObject obj in spawnedAfterimages)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedAfterimages.Clear();
    }

    private void OnDisable()
    {
        if (!isRunning)
            return;

        StopAllCoroutines();

        isRunning = false;

        SetWalking(false);
        ChangeToFollowCamera();
        RestorePlayer();
    }
}
