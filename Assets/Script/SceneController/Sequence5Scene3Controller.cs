using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence5Scene3Controller : SceneController
{
    [SerializeField] AnalogTVNoiseController _noiseControlloer;

    private void Start()
    {
        _noiseControlloer.SetPreset(AnalogTVNoiseController.TVNoisePreset.Stage1);
    }

    protected override void OnStopIntervalReached()
    {

    }

    
}
