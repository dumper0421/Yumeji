/// <summary>
/// [7-2] 테스트 전역 스위치.
/// 테스트 패널(S7S2TestPanel)에서 켜고 끄며, 각 기믹 스크립트가 이 값을 참조한다.
/// 빌드에 남아도 기본값이 '평소 동작'이라 게임 진행에는 영향이 없다.
/// </summary>
public static class S7S2Test
{
    /// <summary>true면 마네킹에 닿아도 게임오버되지 않는다.</summary>
    public static bool Invincible = false;

    /// <summary>true면 대사가 재생 중이어도 마네킹이 계속 움직인다. (테스트용)</summary>
    public static bool IgnoreDialoguePause = false;

    /// <summary>씬을 새로 로드할 때마다 초기화한다.</summary>
    public static void Reset()
    {
        Invincible = false;
        IgnoreDialoguePause = false;
    }
}
