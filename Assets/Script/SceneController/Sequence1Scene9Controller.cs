using UnityEngine;

public class Sequence1Scene9Controller : SceneController
{
    [Header("Button Press 시 재생할 BGM")]
    [SerializeField] private AudioClip buttonBgm;   // 버튼 누르면  바꿀 BGM

    protected override void OnStopIntervalReached()
    {
        
    }


    public void ChangeBGMToButtonClip()
    {
        if (buttonBgm == null) return;

        
        bgm = buttonBgm;
        // SoundManager 로 즉시 재생
        SoundManager.Instance.PlayBGM(bgm);
    }
}
