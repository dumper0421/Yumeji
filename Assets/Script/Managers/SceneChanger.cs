using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void OnNewGameButtonClicked()
    {
        SceneManager.LoadScene("Scenes/Choiwoohyck/Sequence1S#3_1_CutScene1");
    }

    public void OnTitleButtonClicked()
    {
        SceneManager.LoadScene("Scenes/Choiwoohyck/TitleScene");
    } 

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
