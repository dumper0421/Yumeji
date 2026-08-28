using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : InspectableObject
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private float activeDuration = 0.1f;

    protected override void OnInspect()
    {   
        StartCoroutine(ActivateTemporarily());
    }

    private IEnumerator ActivateTemporarily()
    {
        targetObject.SetActive(true);

        yield return new WaitForSeconds(activeDuration);

        targetObject.SetActive(false);
    }
}
