using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineController : MonoBehaviour
{
    public PlayableDirector Director;
    public string SceneString;
    void Start()
    {
        Director.Play();
        StartCoroutine(StopTimelineAfterPlay());
    }

    IEnumerator StopTimelineAfterPlay()
    {
        yield return new WaitForSeconds((float)Director.duration);
        SceneManager.LoadScene(SceneString);
    }
}
