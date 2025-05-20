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
            SceneManager.LoadScene(sceneName_);
            hasReached_ = true;
        }
    }
}
