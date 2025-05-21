using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoSceneController : MonoBehaviour
{
    [Tooltip("Inspector에 VideoPlayer 컴포넌트를 할당하세요.")]
    public VideoPlayer videoPlayer;

    [Tooltip("영상 재생이 끝난 뒤 이동할 씬 이름")]
    public string nextSceneName = "MainScene";

    void Start()
    {
        // 재생 완료 콜백 등록
        videoPlayer.loopPointReached += OnVideoFinished;
        // 필요하다면 Prepare 후 Play
        videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // 씬 전환
        SceneManager.LoadScene(nextSceneName);
    }
}
