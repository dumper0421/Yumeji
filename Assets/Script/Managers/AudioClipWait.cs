using System.Collections;
using UnityEngine;

/// <summary>
/// AudioClip 길이만큼 기다리는 헬퍼.
/// 웹빌드(WebGL)는 오디오를 비동기로 디코딩해서, 로드가 끝나기 전에는 clip.length 가 0 이다.
/// 그대로 WaitForSeconds(clip.length) 를 쓰면 기다리지 않고 바로 넘어가므로, 로드가 끝날 때까지 기다린 뒤 길이를 읽는다.
/// PC에서는 보통 이미 로드되어 있어 기존과 같다.
/// </summary>
public static class AudioClipWait
{
    /// <summary>클립이 로드될 때까지(최대 loadTimeout초) 기다린 뒤, 클립 길이만큼 기다린다.</summary>
    public static IEnumerator ForLength(AudioClip clip, float loadTimeout = 5f)
    {
        if (clip == null)
            yield break;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        float t = 0f;
        while (clip.loadState == AudioDataLoadState.Loading && t < loadTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (clip.length > 0f)
            yield return new WaitForSeconds(clip.length);
    }

    /// <summary>클립 길이. 아직 로드되지 않아 0이면 fallback.</summary>
    public static float LengthOr(AudioClip clip, float fallback)
    {
        return clip != null && clip.length > 0f ? clip.length : fallback;
    }
}
