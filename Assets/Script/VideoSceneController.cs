using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoSceneController : MonoBehaviour
{
    [Tooltip("Inspector에 VideoPlayer 컴포넌트를 할당하세요.")]
    public VideoPlayer videoPlayer;

    [Tooltip("영상 재생이 끝난 뒤 이동할 씬 이름")]
    public string nextSceneName = "MainScene";

    [Tooltip("이 시간 안에 영상이 시작되지 않으면(웹에서 영상을 못 불러온 경우 등) 멈추지 않고 다음 씬으로 넘어간다")]
    public float startTimeout = 10f;

    private bool _finished;

    void Start()
    {
        // 재생 완료 콜백 등록
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
        // 필요하다면 Prepare 후 Play
        videoPlayer.Play();
        StartCoroutine(CoStartWatchdog());
    }

    private IEnumerator CoStartWatchdog()
    {
        float t = 0f;
        while (t < startTimeout && !videoPlayer.isPrepared && videoPlayer.frame <= 0)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared && videoPlayer.frame <= 0)
        {
            Debug.LogWarning($"[VideoSceneController] {startTimeout}초 안에 영상이 시작되지 않아 다음 씬으로 넘어간다.");
            Finish();
        }
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        // 웹빌드: 씬이 켜질 때 VideoClip으로 먼저 재생을 시도하다 난 오류가, WebGLVideoUrl이 URL로 바꾼 뒤에 늦게 도착한다.
        // URL 재생과 무관한 이 오류는 무시한다 (진짜로 못 틀면 시작 타임아웃이 넘겨준다).
        if (vp.source == VideoSource.Url && message.Contains("clip"))
            return;

        Debug.LogWarning($"[VideoSceneController] 영상 오류로 다음 씬으로 넘어간다: {message}");
        Finish();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Finish();
    }

    private void Finish()
    {
        if (_finished)
            return;
        _finished = true;

        // 씬 전환
        SceneManager.LoadScene(nextSceneName);
    }
}
