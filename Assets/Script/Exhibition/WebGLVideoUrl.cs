using UnityEngine;
using UnityEngine.Video;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// WebGL은 VideoClip 에셋 재생을 지원하지 않는다. 웹빌드에서만 같은 영상을 URL로 바꿔 재생한다.
/// 영상 바이트(.bytes)를 게임 데이터에 넣어 두고 브라우저 Blob URL로 재생하므로, 별도 파일을 못 올리는 호스트(Unity Play)에서도 동작한다.
/// _videoBytes는 전시 웹빌드 때만 빌드 스크립트가 채우고 빌드 후 비운다 (PC 빌드 용량이 늘지 않게).
/// 에디터와 PC 빌드에서는 아무것도 하지 않아 기존 VideoClip 그대로 쓴다.
/// </summary>
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(VideoPlayer))]
public class WebGLVideoUrl : MonoBehaviour
{
    [Tooltip("브라우저가 재생할 수 있는 H.264/AAC mp4를 확장자만 .bytes로 바꾼 파일. 전시 빌드 스크립트가 자동으로 채운다.")]
    [SerializeField]
    private TextAsset _videoBytes;

    [SerializeField]
    private string _mimeType = "video/mp4";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string YumejiCreateVideoBlobUrl(byte[] data, int length, string mimeType);

    private void Awake()
    {
        if (_videoBytes == null)
        {
            Debug.LogWarning($"[WebGLVideoUrl] {name}: 영상 데이터가 없다. 재생을 건너뛴다.");
            return;
        }

        var vp = GetComponent<VideoPlayer>();
        bool play = vp.playOnAwake;

        vp.Stop();
        byte[] bytes = _videoBytes.bytes;
        vp.source = VideoSource.Url;
        vp.url = YumejiCreateVideoBlobUrl(bytes, bytes.Length, _mimeType);

        // 웹에서는 AudioSource 출력을 쓸 수 없다
        if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource)
            vp.audioOutputMode = VideoAudioOutputMode.Direct;

        if (play)
            vp.Play();
    }
#endif
}
