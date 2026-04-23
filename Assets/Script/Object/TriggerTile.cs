using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriggerTile : MonoBehaviour
{
    private static readonly string[] Stage3GlitchTexts =
    {
        "L?FT",
        "R!GHT",
        "?EFT",
        "RI?HT",
        "L3FT",
        "R1GHT",
        "LE?T",
        "RIG?T",
        "???",
        "LEFT",
        "RIGHT",
        "L#F?",
        "R@GH!",
        "?I?H?",
        "R?GHT",
        "L?FT?",
        "RI##T"
    };

    [SerializeField] private CanvasGroup _group;
    [SerializeField] private int _stage;
    [SerializeField] private float _hintDuration = 2f;
    [SerializeField] private Image _hintBackground;
    [SerializeField] private AudioClip _sfx;

    [Header("Stage1")]
    [SerializeField] private List<Sprite> _changeSprites;
    [SerializeField] private float _totalDuration = 0.3f;
    [SerializeField] private float _changeDuration = 0.05f;

    [Header("Stage2")]
    [SerializeField] private Image _hourHandImage;
    [SerializeField] private Sprite _stage2BackGroundSprite;
    [SerializeField] private float _rotateSpeed = 1080f;
    [SerializeField] private float _snapSpeed = 1440f;
    [SerializeField] private float _rotateDuration = 1f;

    [Header("Stage3")]
    [SerializeField] private TMP_Text _hintTMP;
    [SerializeField] private Sprite _stage3BackGroundSprite;
    [SerializeField] private float _textTotalDuration = 0.3f;
    [SerializeField] private float _textChangeInterval = 0.05f;
    [SerializeField] private string _finalStage3Text = "RIGHT";

    private bool _isPlaying;
    private RectTransform _hourHandRect;

    private void Awake()
    {
        if (_hourHandImage != null)
            _hourHandRect = _hourHandImage.rectTransform;
    }

    private void Start()
    {
        HideAllHints();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (_isPlaying)
            return;

        _group.alpha = 0.4f;
        StartCoroutine(Co_Hint());
    }

    private IEnumerator Co_Hint()
    {
        _isPlaying = true;
        SoundManager.Instance.PlaySFX(_sfx);
        switch (_stage)
        {
            case 1:
                yield return RunStage1();
                break;

            case 2:
                yield return RunStage2();
                break;

            case 3:
                yield return RunStage3();
                break;
        }

        HideAllHints();
        _isPlaying = false;

    }

    private IEnumerator RunStage1()
    {
        float totalElapsed = 0f;
        float changeElapsed = 0f;
        int idx = 0;

        while (totalElapsed < _totalDuration)
        {
            float dt = Time.deltaTime;
            totalElapsed += dt;
            changeElapsed += dt;

            if (changeElapsed >= _changeDuration)
            {
                idx = 1 - idx;
                _hintBackground.sprite = _changeSprites[idx];
                changeElapsed = 0f;
            }

            yield return null;
        }

        _hintBackground.sprite = _changeSprites[1];
        yield return WaitRemain(_totalDuration);
    }

    private IEnumerator RunStage2()
    {
        _hintBackground.sprite = _stage2BackGroundSprite;
        _hourHandImage.enabled = true;

        float elapsed = 0f;
        float currentZ = _hourHandRect.localEulerAngles.z;

        while (elapsed < _rotateDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            currentZ -= _rotateSpeed * dt;
            _hourHandRect.localEulerAngles = new Vector3(0f, 0f, currentZ);

            yield return null;
        }

        const float targetZ = 90f;

        while (Mathf.Abs(Mathf.DeltaAngle(_hourHandRect.localEulerAngles.z, targetZ)) > 0.1f)
        {
            float nextZ = Mathf.MoveTowardsAngle(
                _hourHandRect.localEulerAngles.z,
                targetZ,
                _snapSpeed * Time.deltaTime
            );

            _hourHandRect.localEulerAngles = new Vector3(0f, 0f, nextZ);
            yield return null;
        }

        _hourHandRect.localEulerAngles = new Vector3(0f, 0f, targetZ);
        yield return WaitRemain(_rotateDuration);
    }

    private IEnumerator RunStage3()
    {
        _hintBackground.sprite = _stage3BackGroundSprite;

        _hourHandImage.enabled = false;
        _hintTMP.gameObject.SetActive(true);

        float totalElapsed = 0f;
        WaitForSeconds intervalWait = new WaitForSeconds(_textChangeInterval);

        while (totalElapsed < _textTotalDuration)
        {
            _hintTMP.text = GetRandomGlitchText();
            yield return intervalWait;
            totalElapsed += _textChangeInterval;
        }

        _hintTMP.text = _finalStage3Text;
        yield return WaitRemain(_textTotalDuration);
    }

    private string GetRandomGlitchText()
    {
        return Stage3GlitchTexts[Random.Range(0, Stage3GlitchTexts.Length)];
    }

    private WaitForSeconds WaitRemain(float usedDuration)
    {
        return new WaitForSeconds(Mathf.Max(0f, _hintDuration - usedDuration));
    }

    private void HideAllHints()
    {
        if (_group != null)
            _group.alpha = 0f;

        if (_hourHandImage != null)
            _hourHandImage.enabled = false;

        if (_hintTMP != null)
            _hintTMP.gameObject.SetActive(false);

    }
}