using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class DisableScriptBehaviour : PlayableBehaviour
{
    public MonoBehaviour scriptToDisable;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (scriptToDisable != null)
        {
            scriptToDisable.enabled = true; // 실행 시 특정 스크립트 비활성화
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (scriptToDisable != null)
        {
            scriptToDisable.enabled = false; // 타임라인이 멈추면 다시 활성화
        }
    }
}
