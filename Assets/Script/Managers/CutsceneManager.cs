using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class CutsceneManager : Singleton<CutsceneManager>
{

    [Header("페이드 설정 (Fade Settings)")]
    [Tooltip("암전 및 페이드에 사용될 검정 이미지")] public Image FadeImage;
    [Tooltip("페이드 동작에 걸리는 시간 (초)")] public float FadeDuration = 1f;

    protected override void Init()
    {
    }

    private void Start()
    {
        if (FadeImage != null)
        {
            // 초기 시작 시 암전(검정 화면)
            FadeImage.gameObject.SetActive(true);
            Color c = FadeImage.color;
            FadeImage.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    /// <summary>
    /// 암전 상태에서 화면을 페이드 인(Fade In)합니다. (검정 -> 투명)
    /// </summary>
    /// <param name="onComplete">페이드 완료 후 호출될 콜백</param>
    public void FadeFromBlack(Action onComplete = null)
    {
        StartCoroutine(Fade(1f, 0f, onComplete));
    }

    /// <summary>
    /// 화면을 페이드 아웃(Fade Out)하여 암전 상태로 만듭니다. (투명 -> 검정)
    /// </summary>
    /// <param name="onComplete">페이드 완료 후 호출될 콜백</param>
    public void FadeToBlack(Action onComplete = null)
    {
        StartCoroutine(Fade(0f, 1f, onComplete));
    }

    /// <summary>
    /// 알파 값을 보간하여 페이드 동작을 수행하는 코루틴
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha, Action onComplete)
    {
        if (FadeImage == null)
            yield break;

        float elapsed = 0f;
        Color c = FadeImage.color;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / FadeDuration);
            FadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        // 정확히 최종 알파 값으로 설정
        FadeImage.color = new Color(c.r, c.g, c.b, endAlpha);
        onComplete?.Invoke();
    }


}
