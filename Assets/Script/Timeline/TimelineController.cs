using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class TimelineController : MonoBehaviour
{
    public PlayableDirector Director;
    public string SceneString;

    void Start()
    {
        Director.Play();
    }

    public void ChangeScene()
    {
        SceneManager.LoadScene(SceneString);
    }
}
