using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum S1S3CutSceneState
{
    None
}
public class S1S3CutSceneDialogueController : DialogueController<S1S3CutSceneState>
{
    public string SceneString;
    protected override void HandleDialogueEnd(string dialogueId)
    {
        if(dialogueId == "PlayerStart")
        {
            SceneManager.LoadScene(SceneString);
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
       
    }

    protected override void OnPuzzleComplete()
    {
        
    }

    protected override void TryProgress()
    {
       
    }
}
