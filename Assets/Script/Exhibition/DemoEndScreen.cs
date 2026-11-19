using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 체험판 끝 안내 화면. 문구를 천천히 띄우고, 잠시 뒤 아무 키나 클릭으로(또는 시간이 지나면) 타이틀로 돌아간다.
/// </summary>
public class DemoEndScreen : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup _group;

    [SerializeField]
    private float _fadeIn = 2f;

    [Tooltip("이 시간이 지나야 키 입력으로 넘길 수 있다")]
    [SerializeField]
    private float _minHold = 3f;

    [Tooltip("입력이 없어도 이 시간이 지나면 타이틀로 돌아간다 (전시장에서 다음 관람객을 위해)")]
    [SerializeField]
    private float _autoReturn = 20f;

    [SerializeField]
    private float _fadeOut = 1.5f;

    [SerializeField]
    private string _titleScene = "TitleScene";

    private IEnumerator Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.StopAllSFX();
        }

        yield return Fade(0f, 1f, _fadeIn);

        float t = 0f;
        while (t < _autoReturn)
        {
            t += Time.unscaledDeltaTime;
            if (t >= _minHold && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
                break;
            yield return null;
        }

        yield return Fade(1f, 0f, _fadeOut);
        SceneManager.LoadScene(_titleScene);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (_group == null)
            yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        _group.alpha = to;
    }
}
