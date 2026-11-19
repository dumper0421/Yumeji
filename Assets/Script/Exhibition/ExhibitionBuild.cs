/// <summary>
/// 전시용 웹빌드(체험판) 설정. 빌드 스크립트가 YUMEJI_EXHIBITION 심볼을 붙여 빌드할 때만 켜진다.
/// 체험판은 시퀀스 7까지이고, 그 뒤로 넘어가려 하면 안내 화면(DemoEndScene)으로 보낸다.
/// </summary>
public static class ExhibitionBuild
{
    public const string EndSceneName = "DemoEndScene";

#if YUMEJI_EXHIBITION
    public const bool Enabled = true;
#else
    public const bool Enabled = false;
#endif

    /// <summary>체험판에 없는 씬(시퀀스 8 이후)으로 가려 하면 안내 화면 이름을 돌려준다. 정식 빌드에서는 그대로.</summary>
    public static string Redirect(string sceneName)
    {
        if (!Enabled || string.IsNullOrEmpty(sceneName))
            return sceneName;

        string name = sceneName.Replace('\\', '/');
        int slash = name.LastIndexOf('/');
        if (slash >= 0)
            name = name.Substring(slash + 1);

        bool beyondDemo = name.StartsWith("Sequence8") || name.StartsWith("Sequence9") || name.StartsWith("Sequence10");
        return beyondDemo ? EndSceneName : sceneName;
    }
}
