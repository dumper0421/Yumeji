#if YUMEJI_EXHIBITION
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 전시 웹빌드 QA용: 주소 끝에 ?qa=씬이름 을 붙이면 타이틀이 뜬 뒤 그 씬으로 바로 간다 (# 은 %23 으로).
/// 예) index.html?qa=Sequence5S%231
/// 파라미터가 없으면 아무것도 하지 않는다. 체험판(YUMEJI_EXHIBITION) 빌드에만 들어간다.
/// </summary>
public class ExhibitionQaJump : MonoBehaviour
{
    private string _scene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        string scene = ReadParam(Application.absoluteURL, "qa");
        if (string.IsNullOrEmpty(scene))
            return;

        var go = new GameObject("[ExhibitionQaJump]");
        DontDestroyOnLoad(go);
        go.AddComponent<ExhibitionQaJump>()._scene = scene;
    }

    private IEnumerator Start()
    {
        // 타이틀에서 매니저(사운드, 대화 등)가 먼저 만들어지도록 잠깐 기다린다
        yield return new WaitForSecondsRealtime(1.5f);

        if (!Application.CanStreamedLevelBeLoaded(_scene))
        {
            Debug.LogError($"[QA] 빌드에 없는 씬: {_scene}");
            yield break;
        }

        Debug.Log($"[QA] 씬 이동: {_scene}");
        SceneManager.LoadScene(_scene);
        Destroy(gameObject);
    }

    private static string ReadParam(string url, string key)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        int q = url.IndexOf('?');
        if (q < 0)
            return null;

        string query = url.Substring(q + 1);
        int hash = query.IndexOf('#');
        if (hash >= 0)
            query = query.Substring(0, hash);

        foreach (string pair in query.Split('&'))
        {
            int eq = pair.IndexOf('=');
            if (eq > 0 && pair.Substring(0, eq) == key)
                return Uri.UnescapeDataString(pair.Substring(eq + 1));
        }

        return null;
    }
}
#endif
