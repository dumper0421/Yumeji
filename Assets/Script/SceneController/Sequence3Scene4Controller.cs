using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Sequence3Scene4Controller : SceneController
{
    [Header("Player")]
    [SerializeField] private PlayerActionController playerAction;

    [Header("Projector SFX")]
    [SerializeField] private AudioSource projectorSource;
    [SerializeField] private AudioClip projectorLoopClip;
    [SerializeField] private float projectorStartVolume = 0.2f;
    [SerializeField] private float projectorMaxVolume = 1f;
    [SerializeField] private float projectorVolumeUpDuration = 2f;

    [Header("White Flash Overlay")]
    [SerializeField] private CanvasGroup whiteFadeCanvasGroup;
    [SerializeField] private float whiteFadeDuration = 1.5f;
    [SerializeField] private float holdWhiteTime = 0.3f;

    [Header("Video Overlay")]
    [SerializeField] private CanvasGroup videoCanvasGroup;
    [SerializeField] private RawImage videoRawImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float videoFadeInDuration = 0.2f;

    [Header("Scene Flow")]
    [SerializeField] private string nextSceneName;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool eventRunning = false;

    private void Awake()
    {
        if (projectorSource != null)
        {
            projectorSource.playOnAwake = false;
            projectorSource.loop = true;
            projectorSource.volume = projectorStartVolume;
        }

        if (whiteFadeCanvasGroup != null)
        {
            whiteFadeCanvasGroup.alpha = 0f;
            whiteFadeCanvasGroup.interactable = false;
            whiteFadeCanvasGroup.blocksRaycasts = true;
        }

        if (videoCanvasGroup != null)
        {
            videoCanvasGroup.alpha = 0f;
            videoCanvasGroup.interactable = false;
            videoCanvasGroup.blocksRaycasts = true;
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
        }

        if (videoRawImage != null)
        {
            videoRawImage.enabled = true;
        }
    }

    public void StartFantasyLightEvent()
    {
        if (eventRunning) return;
        eventRunning = true;
        StartCoroutine(CoFantasyLightEvent());
    }

    private IEnumerator CoFantasyLightEvent()
    {
        if (debugLog)
            Debug.Log("[S3S4] 환상의 빛 이벤트 시작");

        LockPlayerControls(true);

        // 1. 영사기 루프 시작
        PlayProjectorLoop(true);

        // 2. 영사기 소리 점점 크게
        if (projectorSource != null)
            yield return StartCoroutine(FadeProjectorVolume(projectorStartVolume, projectorMaxVolume, projectorVolumeUpDuration));

        // 3. 화면 전체를 흰 빛으로 채움
        if (whiteFadeCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(whiteFadeCanvasGroup, 1f, whiteFadeDuration));

        // 4. 잠깐 흰 화면 유지
        if (holdWhiteTime > 0f)
            yield return new WaitForSeconds(holdWhiteTime);

        // 5. 영상 오버레이 표시
        if (videoCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, videoFadeInDuration));
        /*
        // 6. mp4 재생
        if (videoPlayer != null)
        {
            if (debugLog)
                Debug.Log("[S3S4] mp4 영상 재생 시작");

            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);

            videoPlayer.Play();

            yield return new WaitUntil(() => !videoPlayer.isPlaying && videoPlayer.frame > 0);
        }
        else
        {
            Debug.LogWarning("[S3S4] videoPlayer가 비어있었다. 영상 없이 다음 씬으로 이동했다.");
            yield return new WaitForSeconds(1f);
        }
        */

        // 7. 다음 씬 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (debugLog)
                Debug.Log($"[S3S4] 다음 씬 이동: {nextSceneName}");

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[S3S4] nextSceneName이 비어있었다.");
        }
    }

    private void LockPlayerControls(bool locked)
    {
        if (playerMoveTestLerp != null)
        {
            playerMoveTestLerp.canMove = !locked;

            if (locked && playerMoveTestLerp.animator != null)
                playerMoveTestLerp.animator.SetBool("Walking", false);
        }

        if (playerAction != null)
            playerAction.IsLockedByEvent = locked;
    }

    private void PlayProjectorLoop(bool play)
    {
        if (projectorSource == null || projectorLoopClip == null) return;

        if (play)
        {
            projectorSource.clip = projectorLoopClip;
            projectorSource.volume = projectorStartVolume;

            if (!projectorSource.isPlaying)
                projectorSource.Play();
        }
        else
        {
            if (projectorSource.isPlaying)
                projectorSource.Stop();
        }
    }

    private IEnumerator FadeProjectorVolume(float from, float to, float duration)
    {
        if (projectorSource == null) yield break;

        float elapsed = 0f;
        projectorSource.volume = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            projectorSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        projectorSource.volume = to;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    protected override void OnStopIntervalReached()
    {
    }
}