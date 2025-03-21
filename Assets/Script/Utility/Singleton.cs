using UnityEngine;

// T 타입의 Manager를 싱글톤으로 만들어주는 클래스
// T 타입의 Manager는 Init 함수를 구현해야 한다.
// T 타입의 Manager는 Singleton<T>를 상속받아야 한다.
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance = null;

    // T 타입의 Manager를 반환하는 프로퍼티
    public static T Instance
    {
        get
        {
            if (_instance is null)
                _instance = InitManager<T>();
            return _instance;
        }
    }

    // T 타입의 Manager를 생성하고 반환하는 함수
    private static U InitManager<U>() where U : MonoBehaviour
    {
        GameObject go = null;
        U manager = FindObjectOfType<U>();
        if (manager is null)
        {
            go = new GameObject(typeof(U).Name);
            manager = go.AddComponent<U>();
        }
        else
            go = manager.gameObject;

        DontDestroyOnLoad(go);
        return manager;
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this as T;
        else if (_instance != this)
            Destroy(gameObject);
        Init();
    }

    // 초기화 함수
    protected abstract void Init();
}