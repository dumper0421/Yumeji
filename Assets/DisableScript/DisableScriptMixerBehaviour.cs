using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class DisableScriptMixerBehaviour : PlayableBehaviour
{
    // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        MonoBehaviour trackBinding = playerData as MonoBehaviour;

        if (!trackBinding)
            return;

        int inputCount = playable.GetInputCount ();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<DisableScriptBehaviour> inputPlayable = (ScriptPlayable<DisableScriptBehaviour>)playable.GetInput(i);
            DisableScriptBehaviour input = inputPlayable.GetBehaviour ();
            
            // Use the above variables to process each frame of this playable.
            
        }
    }
}
