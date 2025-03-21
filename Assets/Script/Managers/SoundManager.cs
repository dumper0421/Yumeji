using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : Singleton<SoundManager>
{
    
    protected override void Init()
    {
        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");

        _bgmSource = CreateAudioSource("BGM");
        _bgmSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("BGM")[0];

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
    /// <param name="clip"> 재생할 AudioClip </param>
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

    #region SFX

    // SFX AudioSource 개수 (= 동시재생 가능한 SFX 개수)
    private const int _sfxSourceCount = 4;
    // 재생 가능한 SFX AudioSource를 관리하는 Queue
    private readonly Queue<AudioSource> _sfxSources = new Queue<AudioSource>();

    /// <summary>
    /// SFX를 재생하는 함수
    /// </summary>
    /// <param name="clip"> 재생할 clip </param>
    /// <param name="volume"> 볼륨 설정 (기본 1) </param>
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
    /// <param name="clip"> 정지할 clip </param>
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

    #region Mixer

    private AudioMixer _audioMixer;

    /// <summary>
    /// Master 볼륨을 설정하는 함수
    /// </summary>
    /// <param name="volume"> 설정할 volume </param>
    public void SetMasterVolume(float volume)
    {
        _audioMixer.SetFloat("MasterVolume", volume);
    }

    /// <summary>
    /// BGM 볼륨을 설정하는 함수
    /// </summary>
    /// <param name="volume"> 설정할 volume </param>
    public void SetBGMVolume(float volume)
    {
        _audioMixer.SetFloat("BGMVolume", volume);
    }

    /// <summary>
    /// SFX 볼륨을 설정하는 함수
    /// </summary>
    /// <param name="volume"> 설정할 volume </param>
    public void SetSFXVolume(float volume)
    {
        _audioMixer.SetFloat("SFXVolume", volume);
    }

    #endregion
}