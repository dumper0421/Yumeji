using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void OnNewGameButtonClicked()
    {
        SceneManager.LoadScene("1");
    }

    public void OnMainButtonClicked()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
