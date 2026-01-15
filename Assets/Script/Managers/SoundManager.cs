// SoundManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : Singleton<SoundManager>
{
    private const string KEY_MASTER = "Master";
    private const string KEY_BGM = "BGM";
    private const string KEY_SFX = "SFX";

    private AudioMixer _audioMixer;
    private AudioSource _bgmSource;
    private AudioSource _sequentialSFXSource;
    private const int _sfxSourceCount = 4;
    private readonly Queue<AudioSource> _sfxSources = new Queue<AudioSource>();
    private readonly Queue<AudioClip> _sfxQueue = new Queue<AudioClip>();

    private Coroutine _bgmFadeCo;
    private IEnumerator FadeSourceVolume(AudioSource src, float target, float duration, System.Action onDone)
    {
        float start = src.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        src.volume = target;
        onDone?.Invoke();
    }
    protected override void Init()
    {
        DontDestroyOnLoad(gameObject);

        // 오디오 믹서 로드 및 저장된 볼륨 복원
        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");
        float master = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        float bgm = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        SetMasterVolume(master, save: false);
        SetBGMVolume(bgm, save: false);
        SetSFXVolume(sfx, save: false);

        // BGM Source
        _bgmSource = CreateAudioSource("BGM");
        _bgmSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("BGM")[0];

        // Sequential SFX Source
        _sequentialSFXSource = CreateAudioSource("SequentialSFX");
        _sequentialSFXSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];

        // Simultaneous SFX Sources
        for (int i = 0; i < _sfxSourceCount; i++)
        {
            var sfxSrc = CreateAudioSource($"SFX_{i}");
            sfxSrc.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];
            _sfxSources.Enqueue(sfxSrc);
        }
    }

    private AudioSource CreateAudioSource(string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform);
        return obj.AddComponent<AudioSource>();
    }

    #region BGM
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }
    public void PauseBGM() => _bgmSource.Pause();
    public void StopBGM()
    {
        if (_bgmSource != null)
             _bgmSource.Stop();
    }

    public void SetBGMSourceVolume(float volume01, float fadeTime = 0f)
    {
        volume01 = Mathf.Clamp01(volume01);

        if (_bgmFadeCo != null) StopCoroutine(_bgmFadeCo);

        if (fadeTime <= 0f)
        {
            _bgmSource.volume = volume01;
            return;
        }

        _bgmFadeCo = StartCoroutine(FadeSourceVolume(_bgmSource, volume01, fadeTime, () => _bgmFadeCo = null));
    }

    #endregion

    #region SFX (동시 재생)
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfxSources.Count == 0) return;
        var src = _sfxSources.Dequeue();
        src.volume = volume;
        src.PlayOneShot(clip);
        _sfxSources.Enqueue(src);
    }
    public void StopAllSFX() 
    {
        foreach (var src in _sfxSources)
        {
            if (src != null)
                src.Stop(); 
        }
    }
    #endregion

    #region Sequential SFX
    public void EnqueueSFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        _sfxQueue.Enqueue(clip);
        if (!_sequentialSFXSource.isPlaying)
            StartCoroutine(PlayQueuedSFX(volume));
    }

    private IEnumerator PlayQueuedSFX(float volume)
    {
        while (_sfxQueue.Count > 0)
        {
            var clip = _sfxQueue.Dequeue();
            _sequentialSFXSource.volume = volume;
            _sequentialSFXSource.clip = clip;
            _sequentialSFXSource.Play();
            yield return new WaitWhile(() => _sequentialSFXSource.isPlaying);
        }
    }
    #endregion

    #region Mixer Control
    public void SetMasterVolume(float linear, bool save = true)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        _audioMixer.SetFloat("Master", dB);
        if (save) { PlayerPrefs.SetFloat(KEY_MASTER, linear); PlayerPrefs.Save(); }
    }
    public void SetBGMVolume(float linear, bool save = true)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        _audioMixer.SetFloat("BGM", dB);
        if (save) { PlayerPrefs.SetFloat(KEY_BGM, linear); PlayerPrefs.Save(); }
    }
    public void SetSFXVolume(float linear, bool save = true)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        _audioMixer.SetFloat("SFX", dB);
        if (save) { PlayerPrefs.SetFloat(KEY_SFX, linear); PlayerPrefs.Save(); }
    }
    #endregion
}
