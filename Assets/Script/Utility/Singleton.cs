using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError(typeof(T).Name + " 인스턴스가 씬에 존재하지 않습니다. 반드시 수동 배치하세요.");
            }
            return _instance;
        }
    }

    protected virtual bool IsGlobal => false;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            // 전역 매니저라면 씬 전환 시 삭제되지 않도록 설정
            if (IsGlobal)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnSingletonInit();
        }
        else if (_instance != this)
        {
            // 이미 인스턴스가 존재하면 현재 중복 인스턴스는 파괴
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 파생 클래스에서 초기화할 코드를 작성하세요.
    /// </summary>
    protected abstract void OnSingletonInit();
}
