using UnityEngine;
using UnityEngine.Playables;
using Yarn.Unity;

public class DialogueStopperSignal : MonoBehaviour
{
    public DialogueRunner DialogueRunner;

    public void OnTimelineSignalReceived(PlayableDirector director)
    {
        if (DialogueRunner != null && DialogueRunner.IsDialogueRunning)
        {
            DialogueRunner.Stop();
        }
    }
}