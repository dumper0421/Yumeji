using UnityEngine;
using UnityEngine.SceneManagement;

public class QASceneButton : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    [SerializeField] private string targetSceneName;

    public void LoadTargetScene()
    {
        Debug.Log("targetscene");
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어있습니다.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}