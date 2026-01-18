using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void OnNewGameButtonClicked()
    {
        SoundManager.Instance.StopAllSFX();
        SoundManager.Instance.StopBGM();
        SceneManager.LoadScene("Scenes/2026QA/OpeningScene");
    }

    public void OnTitleButtonClicked()
    {
        SoundManager.Instance.StopAllSFX();
        SoundManager.Instance.StopBGM();
        SceneManager.LoadScene("Scenes/2026QA/TitleScene");
    } 

    public void ChangeScene(string sceneName)
    {
        SoundManager.Instance.StopAllSFX();
        SoundManager.Instance.StopBGM();
        SceneManager.LoadScene(sceneName);
    }
}
