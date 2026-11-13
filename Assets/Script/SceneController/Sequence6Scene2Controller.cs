using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Sequence6Scene2Controller : SceneController
{
    // =====================================================
    // DIALOGUE
    // =====================================================

    [Header("Dialogue")]
    [SerializeField]
    private Sequence6Scene2DialogueController dialogueController;


    // =====================================================
    // INTRO CHARACTERS
    // =====================================================

    [Header("Seated Characters")]
    [SerializeField]
    private GameObject seatedHaruObject;

    [SerializeField]
    private GameObject seatedRayObject;


    // =====================================================
    // GAMEPLAY CHARACTERS
    // =====================================================

    [Header("Gameplay Characters")]
    [SerializeField]
    private GameObject playerObject;

    [SerializeField]
    private GameObject rayCompanionObject;

    [SerializeField]
    private PlayerMove_Test_Lerp playerMove;

    [SerializeField]
    private PlayerActionController playerActionController;


    // =====================================================
    // KEI
    // =====================================================

    [Header("Kei")]
    [SerializeField]
    private Sequence6Scene2KeiInteractObject keiInteractObject;


    // =====================================================
    // FIRST SHOT
    // =====================================================

    [Header("First Shot")]

    [Tooltip("화면 전체를 덮는 검은색 UI Image")]
    [SerializeField]
    private Image fadeImage;

    [Tooltip("케이가 하루를 바라본 뒤 암전 + 총소리까지의 대기 시간")]
    [SerializeField]
    private float keiTurnDelay = 0.5f;

    [Tooltip("첫 총격 후 완전 암전 상태를 유지하는 시간")]
    [SerializeField]
    private float firstBlackHoldTime = 0.3f;

    [Tooltip("검은 화면이 완전히 걷히는 데 걸리는 시간")]
    [SerializeField]
    private float blackRevealDuration = 0.4f;


    // =====================================================
    // REI DEATH
    // =====================================================

    [Header("Rei Death")]

    [Tooltip("레이 Animator의 사망 State 이름")]
    [SerializeField]
    private string reiDeathStateName = "Rei_Death";

    [Tooltip("레이가 가운데 위치일 때 아래로 떨어지는 거리")]
    [SerializeField]
    private float reiCenterFallDistance = 1.75f;

    [Tooltip("레이가 왼쪽 위치일 때 아래로 떨어지는 거리")]
    [SerializeField]
    private float reiLeftFallDistance = 2.75f;

    [Tooltip("레이가 왼쪽 위치일 때 오른쪽으로 이동하는 거리")]
    [SerializeField]
    private float reiLeftXOffset = 0.5f;

    [Tooltip("사망 애니메이션에서 실제 이동을 시작하는 지점")]
    [Range(0f, 1f)]
    [SerializeField]
    private float reiMoveStartNormalized = 0.22f;

    [Tooltip("사망 애니메이션에서 실제 이동이 끝나는 지점")]
    [Range(0f, 1f)]
    [SerializeField]
    private float reiMoveEndNormalized = 0.70f;

    [Tooltip("레이가 굴러가는 위치 이동 속도 곡선")]
    [SerializeField]
    private AnimationCurve reiFallMoveCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    private Animator reiAnimator;


    // =====================================================
    // HARU BACK STEP
    // =====================================================

    [Header("Haru Back Step")]

    [Tooltip("하루 뒷걸음질 Animator State 이름")]
    [SerializeField]
    private string haruBackStepStateName = "Haru_BackStep";

    [Tooltip("하루가 뒤로 물러나는 거리")]
    [SerializeField]
    private float haruBackStepDistance = 0.75f;

    [Tooltip("레이 사망 애니메이션 종료 후 하루가 움직이기 전 대기 시간")]
    [SerializeField]
    private float haruBackStepDelay = 0.2f;

    [Tooltip("하루가 뒤로 이동하는 데 걸리는 시간")]
    [SerializeField]
    private float haruBackStepDuration = 0.9f;

    [Tooltip("하루 뒷걸음질 이동 속도 곡선")]
    [SerializeField]
    private AnimationCurve haruBackStepCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    private Animator haruAnimator;


    // =====================================================
    // FINAL SHOT
    // =====================================================

    [Header("Final Shot")]

    [Tooltip("두 번째 총소리 + 암전 후 씬 전환까지의 시간")]
    [SerializeField]
    private float finalSceneDelay = 1f;

    [Tooltip("다음 씬 이름")]
    [SerializeField]
    private string nextSceneName;


    // =====================================================
    // STATE
    // =====================================================

    private bool introStarted;
    private bool introFinished;
    private bool firstShotStarted;
    private bool finalShotStarted;


    // =====================================================
    // START
    // =====================================================

    protected new void Start()
    {
        base.Start();

        if (rayCompanionObject != null)
        {
            reiAnimator =
                rayCompanionObject.GetComponent<Animator>();
        }

        if (playerObject != null)
        {
            haruAnimator =
                playerObject.GetComponent<Animator>();
        }

        SetBlackScreen(false);

        StartIntroSequence();
    }


    // =====================================================
    // INTRO
    // =====================================================

    private void StartIntroSequence()
    {
        if (introStarted)
            return;

        introStarted = true;

        if (seatedHaruObject != null)
            seatedHaruObject.SetActive(true);

        if (seatedRayObject != null)
            seatedRayObject.SetActive(true);

        if (playerObject != null)
            playerObject.SetActive(false);

        if (rayCompanionObject != null)
            rayCompanionObject.SetActive(false);

        if (dialogueController != null)
        {
            dialogueController.OnStartedDialogue(
                "S6S2_Intro_Ray1"
            );
        }
    }


    public void FinishIntroSequence()
    {
        if (introFinished)
            return;

        introFinished = true;

        if (seatedHaruObject != null)
            seatedHaruObject.SetActive(false);

        if (seatedRayObject != null)
            seatedRayObject.SetActive(false);

        if (playerObject != null)
            playerObject.SetActive(true);

        if (rayCompanionObject != null)
            rayCompanionObject.SetActive(true);
    }


    // =====================================================
    // FIRST SHOT START
    // =====================================================

    public void TurnKeiDown()
    {
        if (firstShotStarted)
            return;

        firstShotStarted = true;

        StartCoroutine(
            FirstShotSequence()
        );
    }


    // =====================================================
    // FIRST SHOT SEQUENCE
    // =====================================================

    private IEnumerator FirstShotSequence()
    {
        LockPlayerControls(true);


        // 1. 케이가 하루를 바라봄
        if (keiInteractObject != null)
        {
            keiInteractObject.FaceDown();
        }


        // 2. 방향 전환 후 잠깐 대기
        yield return new WaitForSeconds(
            keiTurnDelay
        );


        // 3. 암전
        SetBlackScreen(true);


        // 4. 첫 번째 총소리
        if (keiInteractObject != null)
        {
            keiInteractObject.PlayGunshot();
        }


        // 5. 암전 유지
        yield return new WaitForSeconds(
            firstBlackHoldTime
        );


        // 6. 암전 해제
        yield return StartCoroutine(
            RevealBlackScreen()
        );


        // 7. 레이 사망 + 낙하
        yield return StartCoroutine(
            PlayReiDeathSequence()
        );


        // 8. 잠시 정적
        yield return new WaitForSeconds(
            haruBackStepDelay
        );


        // 9. 하루 뒷걸음질
        yield return StartCoroutine(
            PlayHaruBackStep()
        );


        // 10. 후속 대화
        if (dialogueController != null)
        {
            dialogueController.OnStartedDialogue(
                "S6S2_AfterShot_Kei1"
            );
        }
    }


    // =====================================================
    // REI FALL OFFSET
    // =====================================================

    private Vector3 GetReiFallOffset()
    {
        if (rayCompanionObject == null)
        {
            return new Vector3(
                0f,
                -reiCenterFallDistance,
                0f
            );
        }

        float reiX =
            rayCompanionObject.transform.position.x;


        // 왼쪽 위치
        // 예: X = -0.5
        if (reiX < 0f)
        {
            Debug.Log(
                $"S6S2: 레이 왼쪽 위치 / X={reiX} / X +{reiLeftXOffset}, Y -{reiLeftFallDistance}"
            );

            return new Vector3(
                reiLeftXOffset,
                -reiLeftFallDistance,
                0f
            );
        }


        // 가운데 위치
        // 예: X = 0.5
        Debug.Log(
            $"S6S2: 레이 가운데 위치 / X={reiX} / Y -{reiCenterFallDistance}"
        );

        return new Vector3(
            0f,
            -reiCenterFallDistance,
            0f
        );
    }


    // =====================================================
    // REI DEATH
    // =====================================================

    private IEnumerator PlayReiDeathSequence()
    {
        if (
            reiAnimator == null ||
            rayCompanionObject == null
        )
        {
            yield break;
        }


        // 레이를 Companion 상태에서 해제
        if (
            playerMove != null &&
            playerMove.Companion != null
        )
        {
            playerMove.ReleaseCompanion();
        }


        Transform reiTransform =
            rayCompanionObject.transform;


        Vector3 startPosition =
            reiTransform.position;


        // 현재 X 위치에 따라 낙하 방향 결정
        Vector3 fallOffset =
            GetReiFallOffset();


        Vector3 targetPosition =
            startPosition +
            fallOffset;


        // 사망 애니메이션 시작
        reiAnimator.Play(
            reiDeathStateName,
            0,
            0f
        );


        // Animator State 갱신 대기
        yield return null;


        while (true)
        {
            AnimatorStateInfo stateInfo =
                reiAnimator.GetCurrentAnimatorStateInfo(0);


            float normalized =
                stateInfo.normalizedTime;


            // 아직 실제 위치 이동 전
            if (
                normalized <
                reiMoveStartNormalized
            )
            {
                reiTransform.position =
                    startPosition;
            }


            // 실제 굴러가는 구간
            else if (
                normalized <
                reiMoveEndNormalized
            )
            {
                float moveProgress =
                    Mathf.InverseLerp(
                        reiMoveStartNormalized,
                        reiMoveEndNormalized,
                        normalized
                    );


                float curvedProgress =
                    reiFallMoveCurve.Evaluate(
                        moveProgress
                    );


                reiTransform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        curvedProgress
                    );
            }


            // 위치 이동 완료
            else
            {
                reiTransform.position =
                    targetPosition;
            }


            // 애니메이션 자체가 끝날 때까지 기다림
            if (normalized >= 1f)
            {
                break;
            }


            yield return null;
        }


        reiTransform.position =
            targetPosition;
    }


    // =====================================================
    // HARU BACK STEP
    // =====================================================

    private IEnumerator PlayHaruBackStep()
    {
        if (
            playerObject == null ||
            haruAnimator == null
        )
        {
            yield break;
        }


        Transform haruTransform =
            playerObject.transform;


        Vector3 startPosition =
            haruTransform.position;


        Vector3 targetPosition =
            startPosition +
            Vector3.down * haruBackStepDistance;


        // 계속 케이를 바라봄
        haruAnimator.SetFloat(
            "DirX",
            0f
        );

        haruAnimator.SetFloat(
            "DirY",
            1f
        );


        // 뒷걸음질 애니메이션
        haruAnimator.Play(
            haruBackStepStateName,
            0,
            0f
        );


        float elapsedTime = 0f;


        while (
            elapsedTime <
            haruBackStepDuration
        )
        {
            elapsedTime +=
                Time.deltaTime;


            float normalized =
                Mathf.Clamp01(
                    elapsedTime /
                    haruBackStepDuration
                );


            float curvedProgress =
                haruBackStepCurve.Evaluate(
                    normalized
                );


            haruTransform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curvedProgress
                );


            yield return null;
        }


        haruTransform.position =
            targetPosition;


        // 이동 후에도 케이를 바라봄
        haruAnimator.SetFloat(
            "DirX",
            0f
        );

        haruAnimator.SetFloat(
            "DirY",
            1f
        );
    }


    // =====================================================
    // FINAL SHOT
    // =====================================================

    public void StartFinalShotSequence()
    {
        if (finalShotStarted)
            return;

        finalShotStarted = true;

        StartCoroutine(
            FinalShotSequence()
        );
    }


    private IEnumerator FinalShotSequence()
    {
        LockPlayerControls(true);


        // 마지막 대화 종료 즉시 암전
        SetBlackScreen(true);


        // 두 번째 총소리
        if (keiInteractObject != null)
        {
            keiInteractObject.PlayGunshot();
        }


        // 암전 상태 유지
        yield return new WaitForSeconds(
            finalSceneDelay
        );


        // 다음 씬
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(
                nextSceneName
            );
        }
        else
        {
            Debug.LogWarning(
                "Sequence6Scene2Controller: Next Scene Name이 비어 있습니다."
            );
        }
    }


    // =====================================================
    // BLACK SCREEN
    // =====================================================

    private void SetBlackScreen(
        bool black
    )
    {
        if (fadeImage == null)
            return;


        fadeImage.gameObject.SetActive(
            true
        );


        Color color =
            fadeImage.color;


        color.a =
            black ? 1f : 0f;


        fadeImage.color =
            color;


        fadeImage.raycastTarget =
            black;
    }


    // =====================================================
    // BLACK SCREEN REVEAL
    // =====================================================

    private IEnumerator RevealBlackScreen()
    {
        if (fadeImage == null)
            yield break;


        Color color =
            fadeImage.color;


        if (blackRevealDuration <= 0f)
        {
            color.a = 0f;

            fadeImage.color =
                color;

            fadeImage.raycastTarget =
                false;

            yield break;
        }


        float elapsedTime = 0f;


        while (
            elapsedTime <
            blackRevealDuration
        )
        {
            elapsedTime +=
                Time.deltaTime;


            float normalized =
                Mathf.Clamp01(
                    elapsedTime /
                    blackRevealDuration
                );


            color.a =
                1f - normalized;


            fadeImage.color =
                color;


            yield return null;
        }


        color.a = 0f;


        fadeImage.color =
            color;


        fadeImage.raycastTarget =
            false;
    }


    // =====================================================
    // PLAYER LOCK
    // =====================================================

    public void LockPlayerControls(
        bool locked
    )
    {
        if (playerMove != null)
        {
            playerMove.canMove =
                !locked;
        }


        if (playerActionController != null)
        {
            playerActionController
                .IsLockedByEvent =
                locked;
        }
    }


    // =====================================================
    // SCENE CONTROLLER
    // =====================================================

    protected override void OnStopIntervalReached()
    {
    }
}