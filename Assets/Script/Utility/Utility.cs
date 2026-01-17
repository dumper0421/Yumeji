using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public static class Utility
{
    public static void PlayDirector(PlayableDirector _director)
    {
        _director.time = 0;
        _director.RebuildGraph();
        _director.Play();
    }

}
