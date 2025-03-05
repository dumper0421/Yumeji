using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class DialogueKeyboardAdbance : MonoBehaviour
{
    public DialogueRunner dialogueRunner;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            dialogueRunner.OnViewRequestedInterrupt();
        }
    }
}
