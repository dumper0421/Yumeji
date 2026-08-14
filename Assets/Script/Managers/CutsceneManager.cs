using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class CutsceneManager : Singleton<CutsceneManager>
{
    public Image FadeImage;
    public float FadeDuration = 1f;

    protected override void Init() { }

    private void Start()
    {
        if (FadeImage != null)
        {
            FadeImage.gameObject.SetActive(true);
            Color c = FadeImage.color;
            FadeImage.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    public void FadeFromBlack(Action onComplete = null, float duration = -1f)
    {
        if (FadeImage == null)
        {
            WarnNoFadeImage(nameof(FadeFromBlack));
            onComplete?.Invoke();
            return;
        }

        FadeImage.gameObject.SetActive(true);
        float d = duration > 0f ? duration : FadeDuration;
        StartCoroutine(Fade(1f, 0f, () =>
        {
            // 완전 투명해지면 꺼도 됨(선택)
            FadeImage.gameObject.SetActive(false);
            onComplete?.Invoke();
        }, d));
    }

    public void FadeToBlack(Action onComplete = null, float duration = -1f)
    {
        if (FadeImage == null)
        {
            // onComplete에 씬 전환이 걸려 있는 경우가 많다.
            // 여기서 그냥 return하면 연출만 빠지는 게 아니라 씬이 아예 넘어가지 않는다.
            WarnNoFadeImage(nameof(FadeToBlack));
            onComplete?.Invoke();
            return;
        }

        FadeImage.gameObject.SetActive(true);
        float d = duration > 0f ? duration : FadeDuration;
        StartCoroutine(Fade(FadeImage.color.a, 1f, onComplete, d));
    }

    private void WarnNoFadeImage(string caller)
    {
        Debug.LogWarning(
            $"[CutsceneManager] FadeImage가 없어 {caller} 연출을 건너뛴다. "
                + "씬에 CutSceneManager 프리팹이 있는지, 그 FadeImage에 PopupCanvas의 BlackImage가 물려 있는지 확인할 것.",
            this
        );
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, Action onComplete, float fadeDuration)
    {
        if (FadeImage == null) yield break;

        float elapsed = 0f;
        Color c = FadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : (elapsed / fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            FadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        FadeImage.color = new Color(c.r, c.g, c.b, endAlpha);
        onComplete?.Invoke();
    }
}
