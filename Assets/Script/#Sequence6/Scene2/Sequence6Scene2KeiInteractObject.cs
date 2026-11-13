using UnityEngine;

public class Sequence6Scene2KeiInteractObject : DialogueObject
{
    [Header("Scene")]
    [SerializeField]
    private Sequence6Scene2Controller sceneController;

    [Header("Player Check")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private Animator playerAnimator;

    [Header("Interaction Check")]
    [Tooltip("케이 아래에서 위를 바라볼 때의 DirY 값")]
    [SerializeField]
    private float requiredDirY = 1f;

    [Tooltip("한 타일의 월드 거리")]
    [SerializeField]
    private float tileDistance = 1f;

    [Tooltip("좌표 비교 오차")]
    [SerializeField]
    private float positionTolerance = 0.05f;

    [Header("SFX")]
    [Tooltip("총소리")]
    [SerializeField]
    private AudioClip gunshotClip;

    private Animator keiAnimator;
    private AudioSource audioSource;

    private bool eventStarted = false;

    private void Awake()
    {
        // 같은 케이 오브젝트에서 자동으로 가져옴
        keiAnimator =
            GetComponent<Animator>();

        audioSource =
            GetComponent<AudioSource>();
    }

    private void Start()
    {
        // 씬 시작 시 케이는 위쪽을 바라본다.
        FaceUp();
    }

    protected override void OnInspect()
    {
        if (eventStarted)
            return;

        if (!IsPlayerOneTileBelow())
            return;

        if (!IsPlayerFacingUp())
            return;

        eventStarted = true;

        // 케이와의 대화 시작과 동시에
        // 플레이어 조작 잠금
        if (sceneController != null)
        {
            sceneController.LockPlayerControls(true);
        }

        // DialogueObject의 기존 대화 실행
        base.OnInspect();
    }

    // =====================================================
    // DIRECTION
    // =====================================================

    public void FaceDown()
    {
        SetKeiDirection(
            0f,
            -1f
        );
    }

    public void FaceUp()
    {
        SetKeiDirection(
            0f,
            1f
        );
    }

    private void SetKeiDirection(
        float x,
        float y
    )
    {
        if (keiAnimator == null)
        {
            Debug.LogWarning(
                "Sequence6Scene2KeiInteractObject: Animator를 찾을 수 없습니다."
            );

            return;
        }

        keiAnimator.SetFloat(
            "DirX",
            x
        );

        keiAnimator.SetFloat(
            "DirY",
            y
        );
    }

    // =====================================================
    // SFX
    // =====================================================

    public void PlayGunshot()
    {
        if (audioSource == null)
        {
            Debug.LogWarning(
                "Sequence6Scene2KeiInteractObject: AudioSource가 없습니다."
            );

            return;
        }

        if (gunshotClip == null)
        {
            Debug.LogWarning(
                "Sequence6Scene2KeiInteractObject: Gunshot Clip이 없습니다."
            );

            return;
        }

        audioSource.PlayOneShot(
            gunshotClip
        );
    }

    // =====================================================
    // INTERACTION CHECK
    // =====================================================

    private bool IsPlayerFacingUp()
    {
        if (playerAnimator == null)
            return false;

        float dirY =
            playerAnimator.GetFloat("DirY");

        return Mathf.Approximately(
            dirY,
            requiredDirY
        );
    }

    private bool IsPlayerOneTileBelow()
    {
        if (player == null)
            return false;

        Vector3 playerPos =
            player.position;

        Vector3 keiPos =
            transform.position;

        bool sameX =
            Mathf.Abs(
                playerPos.x - keiPos.x
            )
            <= positionTolerance;

        bool oneTileBelow =
            Mathf.Abs(
                playerPos.y -
                (keiPos.y - tileDistance)
            )
            <= positionTolerance;

        return sameX && oneTileBelow;
    }
}