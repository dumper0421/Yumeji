using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class DisableScriptClip : PlayableAsset, ITimelineClipAsset
{
    public DisableScriptBehaviour template = new DisableScriptBehaviour ();
    public ExposedReference<MonoBehaviour> scriptToDisable;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DisableScriptBehaviour>.Create (graph, template);
        DisableScriptBehaviour clone = playable.GetBehaviour ();
        clone.scriptToDisable = scriptToDisable.Resolve (graph.GetResolver ());
        return playable;
    }
}
