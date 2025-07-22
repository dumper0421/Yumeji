using UnityEngine;
using System.Collections;

public class Sequence1Scene9Controller : SceneController
{
    [Header("Button Press 시 재생할 BGM")]
    [SerializeField] private AudioClip buttonBgm;

    private void Awake()
    {
        if (buttonBgm != null)
            buttonBgm.LoadAudioData();
    }

    protected override void OnStopIntervalReached()
    {
    }

    public void ChangeBGMToButtonClip()
    {
        if (buttonBgm == null)
            return;

        // PlayBGM 호출을 다음 프레임으로 미룸
        StartCoroutine(PlayBGMNextFrame());
    }

    private IEnumerator PlayBGMNextFrame()
    {
        // 물리 업데이트가 끝난 다음 프레임까지 대기
        yield return null;

        bgm = buttonBgm;
        SoundManager.Instance.PlayBGM(bgm);
    }
}
