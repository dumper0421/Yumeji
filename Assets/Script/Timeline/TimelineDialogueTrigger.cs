using UnityEngine;
using UnityEngine.Playables;
using Yarn.Unity;

public class TimelineDialogueTrigger : MonoBehaviour
{
    public DialogueRunner DialogueRunner;
    public string StartNode = "Start";

    public void OnTimelineSignalReceived(PlayableDirector director)
    {
        if (!DialogueRunner.IsDialogueRunning)
        {
            DialogueRunner.StartDialogue(StartNode);
        }
    }
}
