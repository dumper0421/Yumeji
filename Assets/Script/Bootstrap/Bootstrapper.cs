using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // 씬 로드 전 영속 매니저를 의존성 순서대로 초기화합니다.
        // Instance 접근 시 씬에 없으면 GameObject를 자동 생성하고 DontDestroyOnLoad가 적용됩니다.
        _ = ResourceManager.Instance;
        _ = SoundManager.Instance;
        _ = SaveLoadManager.Instance;
    }
}
