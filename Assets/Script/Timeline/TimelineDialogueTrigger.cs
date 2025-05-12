using UnityEngine;
using UnityEngine.Playables;
using Yarn.Unity;

public class TimelineDialogueTrigger : MonoBehaviour
{
    public DialogueManager DialogueManager;
    public string StartNode = "Start";

    public void OnTimelineSignalReceived(PlayableDirector director)
    {
        DialogueManager.StartDialogue(StartNode);
    }
}
