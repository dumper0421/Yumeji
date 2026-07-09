using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Sequence7Scene1Controller : SceneController
{
    [Header("Dialogue")]
    [SerializeField] private DialogueManager _dialogueManager;
    [SerializeField] private string _firstDialogueId = "dialogue_1";

    [Header("Opening Cutscene")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private GameObject _openingIllustration;
    [SerializeField] private AudioClip _openingVoiceClip;
    [SerializeField] private float _fadeInDuration = 3f;
    [SerializeField] private float _fadeOutDuration = 3f;
    [SerializeField] private float _wakeFadeInDuration = 3f;

    protected override void OnStopIntervalReached()
    {
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(PlayOpeningThenStartDialogue());
    }

    private IEnumerator PlayOpeningThenStartDialogue()
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.enabled = false;

        // 1) 페이드 인 되며 일러스트 노출
        if (_openingIllustration != null)
            _openingIllustration.SetActive(true);

        yield return StartCoroutine(Fade(1f, 0f, _fadeInDuration));

        // 음성 재생
        if (_openingVoiceClip != null)
        {
            SoundManager.Instance.PlaySFX(_openingVoiceClip);
            yield return new WaitForSeconds(_openingVoiceClip.length);
        }

        // 2) 페이드 아웃
        yield return StartCoroutine(Fade(0f, 1f, _fadeOutDuration));

        if (_openingIllustration != null)
            _openingIllustration.SetActive(false);

        // 3) 호텔 로비에서 눈을 뜨는 연출: 다시 페이드 인
        yield return StartCoroutine(Fade(1f, 0f, _wakeFadeInDuration));

        if (playerMoveTestLerp != null)
            playerMoveTestLerp.enabled = true;

        if (_dialogueManager != null)
            _dialogueManager.StartDialogue(_firstDialogueId);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (_fadeImage == null) yield break;

        Color c = _fadeImage.color;
        _fadeImage.color = new Color(c.r, c.g, c.b, from);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : elapsed / duration;
            _fadeImage.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, t));
            yield return null;
        }

        _fadeImage.color = new Color(c.r, c.g, c.b, to);
    }
}
