using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeTrigger : MonoBehaviour
{
    private bool hasReached_ = false;
    [SerializeField]
    private string sceneName_;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasReached_)
        {
            SoundManager.Instance.StopAllSFX();
            SoundManager.Instance.StopBGM();
            if (sceneName_ != "Scenes/Final/Sequence1#7")
            SceneManager.LoadScene(sceneName_);
            else
                SceneManager.LoadScene(7);

            hasReached_ = true;
        }
    }
}
