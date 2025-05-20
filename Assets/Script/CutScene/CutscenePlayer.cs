using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

// 1. 이벤트 타입 정의
[Serializable]
public enum CutsceneActionType
{
    FadeIn,        // 화면 페이드 인
    FadeOut,       // 화면 페이드 아웃
    PlayBGM,       // BGM 재생
    StopBGM,       // BGM 정지
    PlaySFX,       // SFX 재생
    ShowText,      // 자막(텍스트) 표시
    HideText,      // 자막 숨기기
    Wait,          // 대기
    CameraCut      // 카메라 전환
}

// 2. 각 컷씬 액션 데이터 컨테이너
[Serializable]
public class CutsceneAction
{
    public CutsceneActionType actionType;
    public float duration;       // 페이드나 대기 시간
    public string parameter;     // 오디오 클립 이름, 자막 내용, 카메라 이름 등
}

// 3. 컷씬 자산: 에디터에서 액션 리스트를 순서대로 정의
[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscene/Create Cutscene Asset")]
public class CutsceneData : ScriptableObject
{
    public List<CutsceneAction> actions = new List<CutsceneAction>();
}

// 4. 컷씬 플레이어: MonoBehaviour로 게임오브젝트에 붙여 사용
public class CutscenePlayer : MonoBehaviour
{
    [Header("Cutscene Data")]
    public CutsceneData cutsceneData;

    [Header("References")]
    public CanvasGroup fadeCanvas;         // 화면 전체 덮는 CanvasGroup
    public TextMeshProUGUI subtitleText;  // 자막용 TMP Text
    public AudioSource bgmSource;         // BGM 오디오 소스
    public AudioSource sfxSource;         // SFX 오디오 소스
    public AudioMixer audioMixer;         // Exposed params: Master/Bgm/SFX
    public Camera[] cameras;              // 컷별 카메라 리스트

    int currentCamIndex = 0;

    public void PlayCutscene()
    {
        StartCoroutine(RunCutscene());
    }

    private IEnumerator RunCutscene()
    {
        foreach (var action in cutsceneData.actions)
        {
            switch (action.actionType)
            {
                case CutsceneActionType.FadeIn:
                    yield return Fade(1f, 0f, action.duration);
                    break;
                case CutsceneActionType.FadeOut:
                    yield return Fade(0f, 1f, action.duration);
                    break;
                case CutsceneActionType.PlayBGM:
                    PlayAudio(bgmSource, action.parameter, loop: true);
                    break;
                case CutsceneActionType.StopBGM:
                    bgmSource.Stop();
                    break;
                case CutsceneActionType.PlaySFX:
                    PlayAudio(sfxSource, action.parameter, loop: false);
                    break;
                case CutsceneActionType.ShowText:
                    subtitleText.text = action.parameter;
                    subtitleText.gameObject.SetActive(true);
                    break;
                case CutsceneActionType.HideText:
                    subtitleText.gameObject.SetActive(false);
                    break;
                case CutsceneActionType.CameraCut:
                    SwitchCamera(action.parameter);
                    break;
                case CutsceneActionType.Wait:
                    yield return new WaitForSeconds(action.duration);
                    break;
            }
            yield return null;
        }
    }

    // 화면 페이드 코루틴
    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        fadeCanvas.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }

    // 오디오 재생 헬퍼
    private void PlayAudio(AudioSource source, string clipName, bool loop)
    {
        var clip = Resources.Load<AudioClip>(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[{name}] AudioClip '{clipName}' not found in Resources.");
            return;
        }
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }

    // 카메라 전환
    private void SwitchCamera(string camName)
    {
        foreach (var cam in cameras) cam.gameObject.SetActive(cam.name == camName);
    }
}
