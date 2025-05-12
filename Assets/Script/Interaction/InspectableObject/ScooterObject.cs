using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScooterObject : DialogueObject
{
    public Light2D light2D;
    public int MaxflashCnt = 10;

    protected override void OnInspect()
    {
        DialogueManager.StartDialogue(StartDialogue);
    }

    public IEnumerator Flash()
    {
        int flashCnt = 0;
        while (flashCnt < MaxflashCnt)
        {
            light2D.intensity += Time.deltaTime * 2f;

            if (light2D.intensity > 0.7f)
            {
                light2D.intensity = 0f;
                flashCnt++;
            }

            yield return null;
        }

        yield break;
    }
}
