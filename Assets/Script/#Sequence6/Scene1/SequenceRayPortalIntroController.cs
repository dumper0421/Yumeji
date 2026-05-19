using System.Collections;
using UnityEngine;
using Cinemachine;

public class SequenceRayPortalIntroController : MonoBehaviour
{
    [Header("Camera")]
    public CinemachineVirtualCamera vcam;
    public Transform playerFollowTarget;

    [Header("Camera Intro Points")]
    public Transform cameraIntroStartPoint;
    public Transform cameraIntroEndPoint;
    public Transform cameraIntroTarget;

    [Header("Camera Intro Timing")]
    public float tiltDownDuration = 2.0f;
    public AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player")]
    public GameObject player;

    [Header("Player Timing")]
    public float playerActiveDelayAfterPortalIdle = 0.5f;

    [Header("Portal")]
    public GameObject portalObject;
    public Animator portalAnimator;
    public AudioSource portalAudioSource;
    public AudioClip portalCreateSfx;

    [Header("Portal Animation State Names")]
    public string portalCreateStateName = "Portal_Create";
    public string portalIdleStateName = "Portal_Idle";

    [Header("Portal Timing")]
    public float portalFrameRate = 12f;
    public int createFrameCount = 3;

    private Transform originalFollow;

    private void Start()
    {
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // 1. 시작 시 플레이어와 포탈 숨김
        if (player != null)
            player.SetActive(false);

        if (portalObject != null)
            portalObject.SetActive(false);

        // 2. 기존 카메라 Follow 저장
        if (vcam != null)
            originalFollow = vcam.Follow;

        // 3. 인트로 타겟 시작 위치 설정
        if (cameraIntroTarget != null && cameraIntroStartPoint != null)
            cameraIntroTarget.position = cameraIntroStartPoint.position;

        // 4. 카메라가 인트로 타겟을 따라가게 변경
        if (vcam != null && cameraIntroTarget != null)
            vcam.Follow = cameraIntroTarget;

        yield return null;

        // 5. 틸트다운
        yield return StartCoroutine(PlayTiltDown());

        // 6. 카메라 Follow를 다시 플레이어로 복구
        if (vcam != null)
        {
            if (playerFollowTarget != null)
                vcam.Follow = playerFollowTarget;
            else
                vcam.Follow = originalFollow;
        }

        // 7. 포탈 생성 후 Idle 진입
        yield return StartCoroutine(PlayPortalCreateThenIdle());

        // 8. 포탈 Idle을 잠깐 보여준 뒤 플레이어 등장
        yield return new WaitForSeconds(playerActiveDelayAfterPortalIdle);

        if (player != null)
            player.SetActive(true);
    }

    private IEnumerator PlayTiltDown()
    {
        if (cameraIntroTarget == null || cameraIntroStartPoint == null || cameraIntroEndPoint == null)
            yield break;

        Vector3 startPos = cameraIntroStartPoint.position;
        Vector3 endPos = cameraIntroEndPoint.position;

        float elapsed = 0f;

        while (elapsed < tiltDownDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / tiltDownDuration;
            float curvedT = tiltCurve.Evaluate(t);

            cameraIntroTarget.position = Vector3.Lerp(startPos, endPos, curvedT);

            yield return null;
        }

        cameraIntroTarget.position = endPos;
    }

    private IEnumerator PlayPortalCreateThenIdle()
    {
        if (portalObject == null || portalAnimator == null)
            yield break;

        portalObject.SetActive(true);

        if (portalAudioSource != null && portalCreateSfx != null)
            portalAudioSource.PlayOneShot(portalCreateSfx);

        portalAnimator.Play(portalCreateStateName, 0, 0f);

        float createDuration = createFrameCount / portalFrameRate;
        yield return new WaitForSeconds(createDuration);

        portalAnimator.Play(portalIdleStateName, 0, 0f);
    }
}