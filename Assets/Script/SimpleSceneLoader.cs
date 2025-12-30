using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

[RequireComponent(typeof(Button))]
public class SimpleSceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool asyncLoad = false;

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.RemoveListener(Load);
        _btn.onClick.AddListener(Load);  // ← 자동으로 OnClick 연결
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneJump] sceneName이 비어있다.");
            return;
        }

        bool exists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) { exists = true; break; }
        }
        if (!exists)
        {
            Debug.LogError($"[SceneJump] '{sceneName}'가 Build Settings에 없다.");
            return;
        }

        Debug.Log($"[SceneJump] '{sceneName}' 로드 시도 (async={asyncLoad})");
        if (asyncLoad) StartCoroutine(LoadAsync(sceneName));
        else SceneManager.LoadScene(sceneName);
    }

    private System.Collections.IEnumerator LoadAsync(string name)
    {
        var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
    }
}
