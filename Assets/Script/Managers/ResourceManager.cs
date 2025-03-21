using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    private Dictionary<string, object> _data;

    protected override void Init()
    {
        return;
    }
    /// <summary>
    ///   리소스를 로드합니다.
    /// </summary>
    /// <param name="path">리소스 경로</param>
    /// <typeparam name="T">모든 오브젝트</typeparam>
    /// <returns></returns>
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        if (_data == null)
        {
            _data = new Dictionary<string, object>();
        }

        if (_data.ContainsKey(path))
        {
            return _data[path] as T;
        }

        T data = Resources.Load<T>(path);
        _data.Add(path, data);
        return data;
    }

    /// <summary>
    ///     프리팹을 생성합니다.
    /// </summary>
    /// <param name="path">프리팹의 경로</param>
    /// <param name="parent">부모 오브젝트</param>
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("Failed to load prefab at path: " + path);
            return null;
        }

        return Instantiate(prefab, parent);
    }

    /// <summary>
    ///  오브젝트를 파괴합니다.
    /// </summary>
    /// <param name="gameObject">파괴할 오브젝트</param>
    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return;
        }
        Destroy(gameObject);
    }

}