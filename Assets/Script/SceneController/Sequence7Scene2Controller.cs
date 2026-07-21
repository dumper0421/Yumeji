using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [7-2] 호텔 1층 씬 컨트롤러.
/// 7-1에서 넘어온 직후: 페이드 인 → 하루의 독백(레이가 사라지고 불이 꺼진 것을 깨달음).
/// 시야 제한(플레이어 반경 조명)과 전력 복구 처리는 Sequence7Scene2DialogueController가 담당.
/// </summary>
public class Sequence7Scene2Controller : SceneController
{
    [Header("Dialogue")]
    [SerializeField] private Sequence7Scene2DialogueController _dialogueController;

    [Header("Fade")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeInDuration = 2f;

    protected override void OnStopIntervalReached()
    {
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(FadeInThenStartMonologue());
    }

    private IEnumerator FadeInThenStartMonologue()
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.enabled = false;

        yield return StartCoroutine(FadeIn());

        if (playerMoveTestLerp != null)
            playerMoveTestLerp.enabled = true;

        if (_dialogueController != null)
            _dialogueController.PlayOpeningMonologueIfNeeded();
    }

    private IEnumerator FadeIn()
    {
        if (_fadeImage == null) yield break;

        Color c = _fadeImage.color;
        _fadeImage.color = new Color(c.r, c.g, c.b, 1f);

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = _fadeInDuration <= 0f ? 1f : elapsed / _fadeInDuration;
            _fadeImage.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        _fadeImage.color = new Color(c.r, c.g, c.b, 0f);
    }
}
