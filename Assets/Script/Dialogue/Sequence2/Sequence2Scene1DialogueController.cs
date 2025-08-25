using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum S2S1State
{
    None
}

public class Sequence2Scene1DialogueController : DialogueController<S1S5State>
{

    protected override void HandleDialogueEnd(string dialogueId)
    {
       

      
        TryProgress();
    }
    protected override void DialogueRunning(string dialogueId)
    {
    
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

