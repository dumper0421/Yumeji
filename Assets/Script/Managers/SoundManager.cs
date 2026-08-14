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
    private AudioSource _loopSFXSource;
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

        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");

        float master = PlayerPrefs.HasKey(KEY_MASTER) ? PlayerPrefs.GetFloat(KEY_MASTER) : 0.5f;
        float bgm = PlayerPrefs.HasKey(KEY_BGM) ? PlayerPrefs.GetFloat(KEY_BGM) : 0.5f;
        float sfx = PlayerPrefs.HasKey(KEY_SFX) ? PlayerPrefs.GetFloat(KEY_SFX) : 0.5f;

        SetMasterVolume(master, save: false);
        SetBGMVolume(bgm, save: false);
        SetSFXVolume(sfx, save: false);

         if (!PlayerPrefs.HasKey(KEY_MASTER)) PlayerPrefs.SetFloat(KEY_MASTER, master);
        if (!PlayerPrefs.HasKey(KEY_BGM)) PlayerPrefs.SetFloat(KEY_BGM, bgm);
        if (!PlayerPrefs.HasKey(KEY_SFX)) PlayerPrefs.SetFloat(KEY_SFX, sfx);

        // BGM Source
        _bgmSource = CreateAudioSource("BGM");
        _bgmSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("BGM")[0];

        // Sequential SFX Source
        _sequentialSFXSource = CreateAudioSource("SequentialSFX");
        _sequentialSFXSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];

        // Loop SFX Source (영사기 작동음처럼 계속 깔리는 지속음)
        _loopSFXSource = CreateAudioSource("LoopSFX");
        _loopSFXSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];
        _loopSFXSource.loop = true;
        _loopSFXSource.playOnAwake = false;

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
        _bgmWasPaused = false;
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
        _sfxQueue.Clear();

        if (_sequentialSFXSource != null)
            _sequentialSFXSource.Stop();

        StopLoopSFX();

        foreach (var src in _sfxSources)
        {
            if (src != null)
                src.Stop();
        }
    }
    #endregion

    #region Loop SFX (지속 환경음)
    private Coroutine _loopSFXFadeCo;

    public bool IsLoopSFXPlaying => _loopSFXSource != null && _loopSFXSource.isPlaying;

    /// <summary>
    /// 영사기 작동음처럼 씬 내내 깔리는 지속음을 재생한다.
    /// PlaySFX는 PlayOneShot이라 루프도 개별 정지도 안 되고,
    /// PlayBGM은 BGM 채널 단일 소스라 배경음악을 밀어내기 때문에 전용 소스를 쓴다.
    /// 이미 같은 클립이 돌고 있으면 처음부터 다시 틀지 않고 볼륨만 맞춘다.
    /// </summary>
    public void PlayLoopSFX(AudioClip clip, float volume = 1f, float fadeInTime = 0f)
    {
        if (clip == null || _loopSFXSource == null) return;

        if (_loopSFXFadeCo != null)
        {
            StopCoroutine(_loopSFXFadeCo);
            _loopSFXFadeCo = null;
        }

        if (_loopSFXSource.clip == clip && _loopSFXSource.isPlaying)
        {
            _loopSFXSource.volume = volume;
            return;
        }

        _loopSFXSource.clip = clip;
        _loopSFXSource.loop = true;
        _loopSFXSource.volume = fadeInTime > 0f ? 0f : volume;
        _loopSFXSource.Play();

        if (fadeInTime > 0f)
            _loopSFXFadeCo = StartCoroutine(
                FadeSourceVolume(_loopSFXSource, volume, fadeInTime, () => _loopSFXFadeCo = null));
    }

    public void StopLoopSFX(float fadeOutTime = 0f)
    {
        if (_loopSFXSource == null) return;

        if (_loopSFXFadeCo != null)
        {
            StopCoroutine(_loopSFXFadeCo);
            _loopSFXFadeCo = null;
        }

        if (fadeOutTime <= 0f || !_loopSFXSource.isPlaying)
        {
            _loopSFXSource.Stop();
            _loopSFXSource.clip = null;
            return;
        }

        _loopSFXFadeCo = StartCoroutine(FadeSourceVolume(_loopSFXSource, 0f, fadeOutTime, () =>
        {
            _loopSFXSource.Stop();
            _loopSFXSource.clip = null;
            _loopSFXFadeCo = null;
        }));
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

    #region Utility

    private readonly List<AudioSource> _pausedSfxSources = new List<AudioSource>();
    private bool _bgmWasPaused = false;
    private bool _sequentialWasPaused = false;
    private bool _loopWasPaused = false;

    public void PauseAllAudio()
    {
        // BGM
        _bgmWasPaused = false;
        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.Pause();
            _bgmWasPaused = true;
        }

        // Sequential SFX
        _sequentialWasPaused = false;
        if (_sequentialSFXSource != null && _sequentialSFXSource.isPlaying)
        {
            _sequentialSFXSource.Pause();
            _sequentialWasPaused = true;
        }

        // Loop SFX
        _loopWasPaused = false;
        if (_loopSFXSource != null && _loopSFXSource.isPlaying)
        {
            _loopSFXSource.Pause();
            _loopWasPaused = true;
        }

        // Simultaneous SFX (풀에 있는 소스들 중 재생중인 것들만 Pause)
        _pausedSfxSources.Clear();
        foreach (var src in _sfxSources)
        {
            if (src != null && src.isPlaying)
            {
                src.Pause();
                _pausedSfxSources.Add(src);
            }
        }
    }

 
    public void ResumeAllAudio()
    {
        // BGM
        if (_bgmWasPaused && _bgmSource != null)
            _bgmSource.UnPause();

        // Sequential SFX
        if (_sequentialWasPaused && _sequentialSFXSource != null)
            _sequentialSFXSource.UnPause();

        // Loop SFX
        if (_loopWasPaused && _loopSFXSource != null)
            _loopSFXSource.UnPause();

        // Simultaneous SFX
        for (int i = 0; i < _pausedSfxSources.Count; i++)
        {
            var src = _pausedSfxSources[i];
            if (src != null)
                src.UnPause();
        }
        _pausedSfxSources.Clear();

        _bgmWasPaused = false;
        _sequentialWasPaused = false;
        _loopWasPaused = false;
    }

    #endregion

}
