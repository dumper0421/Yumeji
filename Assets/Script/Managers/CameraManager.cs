using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraManager : Singleton<CameraManager>
{
    CinemachineVirtualCamera _vcam;
    CinemachineBasicMultiChannelPerlin _noise;

    protected override void Init()
    {
        // 씬에 있는 VirtualCamera 하나를 찾습니다. 
        // 여러 개라면 tag나 이름으로 분기하세요.
        _vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (_vcam == null)
        {
            Debug.LogError("Cinemachine VirtualCamera를 찾을 수 없습니다.");
            return;
        }
        // Noise 컴포넌트 가져오기
        _noise = _vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (_noise == null)
        {
            Debug.LogError("VirtualCamera에 BasicMultiChannelPerlin이 없습니다.");
        }
    }

    /// <summary>
    /// Cinemachine Noise를 이용한 카메라 흔들기
    /// </summary>
    /// <param name="amplitude">흔들림 세기</param>
    /// <param name="frequency">흔들림 속도</param>
    /// <param name="duration">지속 시간</param>
    public IEnumerator Shake(float amplitude, float frequency, float duration = 0.3f)
    {
        if (_noise == null)
            yield break;

        // 원래 값 저장
        float origAmp = _noise.m_AmplitudeGain;
        float origFreq = _noise.m_FrequencyGain;

        // 흔들기 시작
        _noise.m_AmplitudeGain = amplitude;
        _noise.m_FrequencyGain = frequency;

        yield return new WaitForSeconds(duration);

        // 원상복구
        _noise.m_AmplitudeGain = origAmp;
        _noise.m_FrequencyGain = origFreq;
    }

    public IEnumerator CameraZoomIn(
       CinemachineVirtualCamera zoomCamera,
       float initialSize,    
       float zoomInSize,     
       float zoomDuration,
       float waitSeconds = 0f)
    {
        yield return ZoomOrthoSize(zoomCamera, initialSize, zoomInSize, zoomDuration);

        yield return new WaitForSeconds(waitSeconds);

        if (waitSeconds > 0f)
            yield return ZoomOrthoSize(zoomCamera, zoomInSize, initialSize, zoomDuration);
    }

    IEnumerator ZoomOrthoSize(
        CinemachineVirtualCamera zoomCamera,
        float from,
        float to,
        float duration)
    {
        float elapsed = 0f;

        var baseLens = zoomCamera.m_Lens;
        baseLens.Orthographic = true;
        zoomCamera.m_Lens = baseLens;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            var lens = zoomCamera.m_Lens;
            lens.OrthographicSize = Mathf.Lerp(from, to, t);
            zoomCamera.m_Lens = lens;

            yield return null;
        }

        // 정확히 목표치 고정
        var finalLens = zoomCamera.m_Lens;
        finalLens.OrthographicSize = to;
        zoomCamera.m_Lens = finalLens;
    }

}
