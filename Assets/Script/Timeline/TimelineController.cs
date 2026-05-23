using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class TimelineController : MonoBehaviour
{
    public PlayableDirector[] Directors;
    public string SceneString;

    public static int _playingCount = 0;
    public static bool IsPlaying => _playingCount > 0;

    void Start()
    {
        foreach (var director in Directors)
        {
            director.played += OnDirectorPlayed;
            director.stopped += OnDirectorStopped;
            director.Play();
        }
    }

    private void OnDirectorPlayed(PlayableDirector d)
    {
        _playingCount++;
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        _playingCount = Mathf.Max(0, _playingCount - 1);
    }

    private void OnDestroy()
    {
        foreach (var director in Directors)
        {
            if (director == null)
                continue;
            director.played -= OnDirectorPlayed;
            director.stopped -= OnDirectorStopped;
            if (director.state == PlayState.Playing)
                _playingCount = Mathf.Max(0, _playingCount - 1);
        }
    }

    public void ChangeScene()
    {
        SceneManager.LoadScene(SceneString);
    }
}
