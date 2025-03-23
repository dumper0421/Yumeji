using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : Singleton<SoundManager>
{
    protected override void Init()
    {
        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");

        // BGM AudioSource 생성
        _bgmSource = CreateAudioSource("BGM");
        _bgmSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("BGM")[0];

        // Sequential SFX용 AudioSource 생성 (순차 재생 전용)
        _sequentialSFXSource = CreateAudioSource("SequentialSFX");
        _sequentialSFXSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];

        // 동시 재생 가능한 SFX용 AudioSource 생성
        for (int i = 0; i < _sfxSourceCount; i++)
        {
            AudioSource sfxSource = CreateAudioSource($"SFX_{i}");
            sfxSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];

            _sfxSources.Enqueue(sfxSource);
        }
    }

    private AudioSource CreateAudioSource(string sourceName)
    {
        GameObject audioObject = new GameObject(sourceName);
        audioObject.transform.SetParent(transform);
        return audioObject.AddComponent<AudioSource>();
    }

    #region BGM

    private AudioSource _bgmSource;

    /// <summary>
    /// BGM을 재생하는 함수
    /// </summary>
    /// <param name="clip">재생할 AudioClip</param>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    /// <summary>
    /// BGM을 일시정지하는 함수
    /// </summary>
    public void PauseBGM()
    {
        _bgmSource.Pause();
    }

    /// <summary>
    /// BGM을 정지하는 함수
    /// </summary>
    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    #endregion

    #region SFX (동시 재생)

    // SFX AudioSource 개수 (= 동시재생 가능한 SFX 개수)
    private const int _sfxSourceCount = 4;
    // 재생 가능한 SFX AudioSource를 관리하는 Queue
    private readonly Queue<AudioSource> _sfxSources = new Queue<AudioSource>();

    /// <summary>
    /// SFX를 재생하는 함수 (동시 재생)
    /// </summary>
    /// <param name="clip">재생할 clip</param>
    /// <param name="volume">볼륨 설정 (기본 1)</param>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource sfxSource = _sfxSources.Dequeue();

        sfxSource.volume = volume;
        sfxSource.PlayOneShot(clip);

        _sfxSources.Enqueue(sfxSource);
    }

    /// <summary>
    /// 특정 SFX를 정지하는 함수
    /// </summary>
    /// <param name="clip">정지할 clip</param>
    public void StopSFX(AudioClip clip)
    {
        foreach (AudioSource sfxSource in _sfxSources)
        {
            if (sfxSource.clip == clip)
            {
                sfxSource.Stop();
            }
        }
    }

    /// <summary>
    /// 모든 SFX를 정지하는 함수
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource sfxSource in _sfxSources)
        {
            sfxSource.Stop();
        }
    }

    #endregion

    #region Sequential SFX (순차 재생)

    // 순차 재생용 AudioSource (동시에 여러 SFX 재생과 별개)
    private AudioSource _sequentialSFXSource;
    // 순차 재생할 AudioClip을 관리하는 Queue
    private readonly Queue<AudioClip> _sfxQueue = new Queue<AudioClip>();

    /// <summary>
    /// SFX를 순차 재생하기 위해 큐에 추가하는 함수  
    /// 재생 중이 아니라면 자동으로 재생 시작
    /// </summary>
    /// <param name="clip">재생할 AudioClip</param>
    /// <param name="volume">볼륨 (기본값 1.0f)</param>
    public void EnqueueSFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null)
        {
            return;
        }

        _sfxQueue.Enqueue(clip);

        // 재생 중이 아니면 순차 재생 시작
        if (!_sequentialSFXSource.isPlaying)
        {
            StartCoroutine(PlayQueuedSFX(volume));
        }
    }

    /// <summary>
    /// 큐에 저장된 SFX를 순차적으로 재생하는 코루틴  
    /// 현재 재생이 끝날 때까지 대기 후 다음 클립 재생
    /// </summary>
    /// <param name="volume">볼륨 설정</param>
    /// <returns></returns>
    private IEnumerator PlayQueuedSFX(float volume)
    {
        while (_sfxQueue.Count > 0)
        {
            AudioClip nextClip = _sfxQueue.Dequeue();
            _sequentialSFXSource.volume = volume;
            _sequentialSFXSource.clip = nextClip;
            _sequentialSFXSource.Play();

            // 현재 클립 재생이 끝날 때까지 대기
            yield return new WaitWhile(() => _sequentialSFXSource.isPlaying);
        }
    }

    #endregion

    #region Mixer

    private AudioMixer _audioMixer;

    /// <summary>
    /// Master 볼륨을 설정하는 함수
    /// </summary>
    /// <param name="volume">설정할 volume</param>
    public void SetMasterVolume(float volume)
    {
        _audioMixer.SetFloat("MasterVolume", volume);
    }

    /// <summary>
    /// BGM 볼륨을 설정하는 함수
    /// </summary>
    /// <param name="volume">설정할 volume</param>
    public void SetBGMVolume(float volume)
    {
        _audioMixer.SetFloat("BGMVolume", volume);
    }

    /// <summary>
    /// SFX 볼륨을 설정하는 함수
    /// </summary>
    /// <param name="volume">설정할 volume</param>
    public void SetSFXVolume(float volume)
    {
        _audioMixer.SetFloat("SFXVolume", volume);
    }

    #endregion
}
