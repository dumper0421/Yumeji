using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [10-1] 영사실 씬을 한 번에 만들어 주는 메뉴 도구.
/// 유니티 상단 메뉴 → Tools → 유메지 [10-1 영사실] 에서 사용한다.
///
/// 베이스는 Sequence8S#2(매니저/대화창/하루/루나/케이가 이미 연결된 씬)를 복사하고,
/// 맵은 기획서대로 시퀀스 3의 소형 상영관(Sequence3S#4)을 크기·좌석·통로·출구 그대로 가져온다.
/// 3-4의 좌석 상호작용과 출구 잠금 대사는 이 구간에서 실행하지 않으므로 스크립트를 떼어낸다. (기획서 4-2)
///
/// 캐릭터 위치는 기획서 배치도/동선 이미지를 3-4 맵 좌표로 옮긴 값이다.
///   통로: x -7.5(왼쪽 칸), -6.5(오른쪽 칸) / 아래 복도: y 3.25 / 출구: (0.5, 0.25)
///
/// 아래 좌표는 모두 "하루 transform 기준"이다. 하루 스프라이트는 피봇이 가운데, 루나는 발밑이라
/// 같은 y에 세우면 루나가 한 칸 떠 보인다. 루나 지점은 두 스프라이트의 발 위치 차이를 재서 보정한다.
/// </summary>
public static class S10S1SceneTools
{
    private const string Tag = "[10-1 영사실]";

    private const string BaseScenePath = "Assets/Scenes/2026PlayX4/Sequence8S#2.unity";
    private const string TargetScenePath = "Assets/Scenes/2026PlayX4/Sequence10S#1.unity";
    private const string TargetSceneName = "Sequence10S#1";
    private const string AfterScenePath = "Assets/Scenes/2026PlayX4/Sequence9S#2.unity";

    private const string TheaterScenePath = "Assets/Scenes/2026PlayX4/Sequence3S#4.unity";
    private const string TheaterMapPath = "=====Map=====/backGround";

    private const string LoaderPath = "Assets/Script/Dialogue/DialogueLoader.asset";
    private const string FirePrefabPath = "Assets/Prefab/Object/Sequence1/S#7/obj_fireAtlas_v1_0.prefab";

    // 필름 롤 리소스가 오기 전까지 2-1의 필름 통 스프라이트로 대신한다
    private const string FilmSpritePath = "Assets/Sprites/Object/Sequence2/Scene1/obj_firmbox_v1.png";

    // 불타는 소리. 파일을 못 찾으면 1-7의 불소리로 대신한다.
    private const string BurningPlaceholderPath = "Assets/Sound/Sequence1/SFX/S#7 SFX/S#7 불소리.mp3";
    private static readonly string[] BurningNames = { "BGM_불타는 소리", "SFX_불타는소리", "SFX_불타는_소리", "SFX_Burning", "SFX_Fire", "불타는 소리" };

    // 영사기 작동음은 업로드 예정. 그 전까지 3-4에서 쓰던 영사기 루프로 대신한다.
    private const string ProjectorPlaceholderPath = "Assets/Sound/Sequence2/S#2/BGM_영사기_돌아가는_소리.mp3";
    private static readonly string[] ProjectorNames = { "SFX_영사기작동음", "SFX_영사기_작동음", "SFX_Projector", "영사기 작동음" };

    // ---------- 배치 (3-4 맵 좌표) ----------
    private static readonly Vector3 HaruStart = new Vector3(-6.5f, 4.25f, 0f);
    private static readonly Vector3 LunaStart = new Vector3(-7.5f, 6.25f, 0f);
    // 케이는 이동하지 않으므로 기획 배치도의 화면 위치에 맞춰 격자에서 살짝 위로 올린다
    private static readonly Vector3 KeiPos = new Vector3(-6.5f, 7.6f, 0f);
    private static readonly Vector3 LunaBesideHaru = new Vector3(-7.5f, 4.25f, 0f);
    private static readonly Vector3 LunaStepUp = new Vector3(-7.5f, 6.25f, 0f);
    private static readonly Vector3 HaruBeforeKei = new Vector3(-6.5f, 6.25f, 0f);

    private static readonly Vector3[] HaruLeavePath = { new Vector3(-6.5f, 3.25f, 0f), new Vector3(-2.5f, 3.25f, 0f) };
    // 출구(0.5, 0.25)를 지나 화면 밖까지 걸어 나간다
    private static readonly Vector3[] HaruExitPath =
    {
        new Vector3(0.5f, 3.25f, 0f),
        new Vector3(0.5f, 0.25f, 0f),
        new Vector3(0.5f, -1.75f, 0f),
    };

    // 카메라: 맵 윗부분 → 하루·루나·케이와 필름이 함께 보이는 구도 → (5-3) 출구까지 보이는 구도
    // 기획서 카메라 프레임: 맵 가로 전체, 위쪽 프레임에서 아래쪽 프레임(맵 아래 끝까지)으로 틸트 다운
    private static readonly Vector3 CamTop = new Vector3(-6.25f, 8.25f, 0f);
    private static readonly Vector3 CamTalk = new Vector3(-6.25f, 5f, 0f);
    private static readonly Vector3 CamExit = new Vector3(-5.5f, 4.75f, 0f);
    private const float CamOrthoSize = 5f;

    // 처음부터 타고 있는 가장자리 불. 기획 배치도처럼 좌석 위에 작은 불이 흩어져 있다. (극장 벽 안쪽만)
    //   좌석 열 x: 왼쪽 블록 -9.5 / -8.5, 오른쪽 블록 -5.5 ~ -2.5 / 좌석 줄 y: 5.13, 7.13, 9.13, 11.13
    private static readonly Vector3[] EdgeFires =
    {
        new Vector3(-9.5f, 11.13f, 0f), new Vector3(-2.5f, 11.13f, 0f),
        new Vector3(-9.5f, 9.13f, 0f), new Vector3(-2.5f, 9.13f, 0f),
        new Vector3(-9.5f, 7.13f, 0f), new Vector3(-2.5f, 7.13f, 0f),
        new Vector3(-9.5f, 5.13f, 0f), new Vector3(-2.5f, 5.13f, 0f),
        new Vector3(-9.6f, 3.6f, 0f), new Vector3(-2.4f, 3.6f, 0f),
        // 기획 배치도처럼 통로 쪽 안쪽 좌석에도 처음부터 몇 군데
        new Vector3(-8.5f, 9.13f, 0f), new Vector3(-3.5f, 7.13f, 0f),
        new Vector3(-4.5f, 11.13f, 0f), new Vector3(-8.5f, 5.13f, 0f),
    };

    // 대화 중 가장자리에서 통로 쪽으로 하나씩 번지는 불
    private static readonly Vector3[] SpreadFires =
    {
        new Vector3(-8.5f, 11.13f, 0f), new Vector3(-3.5f, 11.13f, 0f),
        new Vector3(-8.5f, 7.13f, 0f), new Vector3(-4.5f, 9.13f, 0f),
        new Vector3(-3.5f, 5.13f, 0f), new Vector3(-5.5f, 7.13f, 0f),
        new Vector3(-5.5f, 11.13f, 0f), new Vector3(-4.5f, 5.13f, 0f),
        new Vector3(-3.5f, 9.13f, 0f), new Vector3(-5.5f, 9.13f, 0f),
    };

    // 좌석 위 불 크기 (한 칸 정도) / 몸에 붙는 불 크기
    private const float SeatFireScale = 0.45f;
    private const float BodyFireScale = 0.5f;

    // 화면 전체 옅은 붉은 필터 (기획서 4-1)
    // 프로젝트가 Linear 색공간이라 UI 알파가 눈으로는 훨씬 진하게 보인다. 옅게 보이도록 낮게 잡는다.
    private static readonly Color RedFilter = new Color(1f, 0.25f, 0.12f, 0.07f);

    private class Ctx
    {
        public readonly List<string> Report = new List<string>();
        public Scene Scene;
        public Transform Haru;
    }

    // ================================================================ 메뉴

    [MenuItem("Tools/유메지 [10-1 영사실]/씬 생성 (Sequence10S#1)", false, 1)]
    public static void BuildSceneMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
        {
            bool ok = EditorUtility.DisplayDialog(
                "10-1 영사실",
                $"{TargetScenePath} 가 이미 있습니다.\n지우고 처음부터 다시 만들까요? (씬에서 직접 옮긴 위치는 사라집니다)",
                "다시 만들기",
                "취소"
            );

            if (!ok)
                return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        BuildScene();
    }

    [MenuItem("Tools/유메지 [10-1 영사실]/리소스 다시 연결 (불타는 소리·영사기)", false, 10)]
    public static void RelinkMenu()
    {
        var controller = Object.FindObjectOfType<Sequence10Scene1DialogueController>(true);

        if (controller == null)
        {
            Debug.LogWarning($"{Tag} 열린 씬에 10-1 컨트롤러가 없다. {TargetSceneName} 씬을 열고 실행할 것.");
            return;
        }

        var ctx = new Ctx();
        LinkSounds(new SerializedObject(controller), ctx, force: true);

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log($"{Tag} 리소스 연결\n - " + string.Join("\n - ", ctx.Report));
    }

    /// <summary>배치모드용: Unity.exe -batchmode -executeMethod S10S1SceneTools.BuildFromCommandLine</summary>
    public static void BuildFromCommandLine()
    {
        bool ok = false;

        try
        {
            ok = BuildScene();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        EditorApplication.Exit(ok ? 0 : 1);
    }

    // ================================================================ 생성

    private static bool BuildScene()
    {
        var ctx = new Ctx();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BaseScenePath) == null)
        {
            Debug.LogError($"{Tag} 베이스 씬이 없다: {BaseScenePath}");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(BaseScenePath, TargetScenePath))
        {
            Debug.LogError($"{Tag} 베이스 씬 복사 실패");
            return false;
        }

        ctx.Scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        var haruMove = Object.FindObjectOfType<PlayerMove_Test_Lerp>(true);
        ctx.Haru = haruMove != null ? haruMove.transform : null;

        if (ctx.Haru == null)
        {
            Debug.LogError($"{Tag} 베이스 씬에서 하루를 못 찾았다.");
            return false;
        }

        StripEightTwo(ctx);

        // ---------- 맵: 3-4 소형 상영관 ----------
        Transform mapRoot = EnsureRoot("=====Map=====", ctx).transform;
        if (!ImportTheater(ctx, mapRoot))
        {
            Debug.LogError($"{Tag} 맵 복사 실패\n - " + string.Join("\n - ", ctx.Report));
            return false;
        }

        // ---------- 연출 그룹 ----------
        Transform root = EnsureRoot("=====S10S1=====", ctx).transform;
        Transform points = EnsureChild(root, "Points");

        Transform luna = FindDeepInScene("Luna");

        // 루나 지점은 발이 하루와 같은 줄에 오도록 피봇 차이만큼 옮긴다
        Vector3 lunaFix = new Vector3(0f, LunaFootCorrection(ctx, luna), 0f);

        Transform haruStart = MakePoint(points, "HaruStart", HaruStart);
        Transform lunaStart = MakePoint(points, "LunaStart", LunaStart + lunaFix);
        Transform lunaBeside = MakePoint(points, "LunaBesideHaru", LunaBesideHaru + lunaFix);
        Transform lunaStepUp = MakePoint(points, "LunaStepUp", LunaStepUp + lunaFix);
        Transform haruBeforeKei = MakePoint(points, "HaruBeforeKei", HaruBeforeKei);

        Transform leaveGroup = EnsureChild(points, "HaruLeavePath");
        Transform[] leavePath = HaruLeavePath.Select((p, i) => MakePoint(leaveGroup, $"Leave_{i + 1}", p)).ToArray();

        Transform exitGroup = EnsureChild(points, "HaruExitPath");
        Transform[] exitPath = HaruExitPath.Select((p, i) => MakePoint(exitGroup, $"Exit_{i + 1}", p)).ToArray();

        Transform camGroup = EnsureChild(points, "Camera");
        Transform camTop = MakePoint(camGroup, "Cam_Top", CamTop);
        Transform camTalk = MakePoint(camGroup, "Cam_Talk", CamTalk);
        Transform camExit = MakePoint(camGroup, "Cam_Exit", CamExit);

        // ---------- 인물 ----------
        ctx.Haru.position = new Vector3(HaruStart.x, HaruStart.y, ctx.Haru.position.z);
        SetVector2(ctx.Haru.GetComponent<PlayerMove_Test_Lerp>(), "startFacing", Vector2.up);

        if (luna != null)
        {
            // 8-2 베이스에서는 루나가 퇴장한 뒤라 꺼져 있다
            luna.gameObject.SetActive(true);
            luna.position = new Vector3(lunaStart.position.x, lunaStart.position.y, luna.position.z);
        }
        else
        {
            ctx.Report.Add("⚠ 루나를 못 찾았다");
        }

        Transform kei = FindDeepInScene("Kei");
        if (kei != null)
            PlaceKei(ctx, kei);
        else
            ctx.Report.Add("⚠ 케이를 못 찾았다");

        Transform film = MakeFilm(ctx, root, kei);

        // ---------- 불 ----------
        Transform fireRoot = EnsureChild(root, "Fires");
        foreach (Vector3 p in EdgeFires)
            MakeFire(ctx, EnsureChild(fireRoot, "Edge"), "Fire", p, SeatFireScale, -Mathf.RoundToInt(p.y) + 2, true);

        var spread = new List<GameObject>();
        foreach (Vector3 p in SpreadFires)
        {
            GameObject f = MakeFire(ctx, EnsureChild(fireRoot, "Spread"), "Fire", p, SeatFireScale, -Mathf.RoundToInt(p.y) + 2, false);
            if (f != null)
                spread.Add(f);
        }

        GameObject haruFire = MakeFire(ctx, fireRoot, "HaruFire", HaruStart, BodyFireScale, 100, false);
        GameObject lunaFire = MakeFire(ctx, fireRoot, "LunaFire", LunaStart, BodyFireScale, 100, false);

        // ---------- 화면 ----------
        MakeOverlayCanvas(ctx, "S10S1 Red Filter Canvas", -1, "RedFilter", RedFilter, true);
        Image white = MakeOverlayCanvas(ctx, "S10S1 White Canvas", 100, "WhiteOut", new Color(1f, 1f, 1f, 0f), false);

        // ---------- 카메라 ----------
        CinemachineVirtualCamera vcam = MakeCamera(root, CamTop);

        // ---------- 컨트롤러 ----------
        GameObject controllerObj = EnsureChild(root, "S10S1Controller").gameObject;
        var controller = controllerObj.AddComponent<Sequence10Scene1DialogueController>();
        var so = new SerializedObject(controller);

        SetObj(so, "dialogueManager", Object.FindObjectOfType<DialogueManager>(true), "대화 매니저", ctx);
        SetObj(so, "dialogueLoader", LoadAsset<DialogueLoader>(LoaderPath, ctx), "대화 로더", ctx);
        SetStr(so, "sceneId", "Sequence10Scene1");
        SetStr(so, "puzzleId", "S10S1");

        SetObj(so, "_playerMove", ctx.Haru.GetComponent<PlayerMove_Test_Lerp>(), "하루", ctx);
        SetObj(so, "_luna", luna, "루나", ctx);
        SetObj(so, "_lunaAnimator", luna != null ? luna.GetComponentInChildren<Animator>(true) : null, "루나 애니메이터", ctx);
        SetObj(so, "_film", film, "필름", ctx);

        SetObj(so, "_haruStart", haruStart, null, ctx);
        SetObj(so, "_lunaStart", lunaStart, null, ctx);
        SetObj(so, "_lunaBesideHaru", lunaBeside, null, ctx);
        SetObj(so, "_lunaStepUp", lunaStepUp, null, ctx);
        SetObj(so, "_haruBeforeKei", haruBeforeKei, null, ctx);
        SetArray(so, "_haruLeavePath", leavePath.Cast<Object>().ToArray());
        SetArray(so, "_haruExitPath", exitPath.Cast<Object>().ToArray());

        SetObj(so, "_vcam", vcam, "카메라", ctx);
        SetObj(so, "_camTop", camTop, null, ctx);
        SetObj(so, "_camTalk", camTalk, null, ctx);
        SetObj(so, "_camExit", camExit, null, ctx);

        SetArray(so, "_spreadFires", spread.Cast<Object>().ToArray());
        SetObj(so, "_haruFire", haruFire, "하루 몸의 불", ctx);
        SetObj(so, "_lunaFire", lunaFire, "루나 몸의 불", ctx);
        SetObj(so, "_whiteOverlay", white, null, ctx);

        LinkSounds(so, ctx, force: false);

        so.ApplyModifiedPropertiesWithoutUndo();

        ResetGlobalLight(ctx);
        AddToBuildSettings(ctx);

        EditorSceneManager.MarkSceneDirty(ctx.Scene);
        EditorSceneManager.SaveScene(ctx.Scene);
        Selection.activeGameObject = controllerObj;

        Debug.Log($"{Tag} 씬 생성 완료: {TargetScenePath}\n - " + string.Join("\n - ", ctx.Report));
        return true;
    }

    // ================================================================ 8-2 정리

    private static void StripEightTwo(Ctx ctx)
    {
        foreach (var c in Object.FindObjectsOfType<Sequence8Scene2DialogueController>(true))
            Object.DestroyImmediate(c);

        foreach (var c in Object.FindObjectsOfType<S8S2CurtainTrigger>(true))
            Object.DestroyImmediate(c);

        foreach (GameObject go in ctx.Scene.GetRootGameObjects())
        {
            string n = go.name.Trim();

            if (
                n == "=====S8S2====="
                || n == "_gardenVolume"
                || n == "SEQ8_Garden_Tilemap_8-1"
                || n == "Kei Camera"
                || n == "Virtual Camera"
            )
            {
                ctx.Report.Add($"8-2 전용 '{n}' 제거");
                Object.DestroyImmediate(go);
            }
        }

        GameObject map = FindRoot("=====Map=====", ctx.Scene);
        if (map != null)
        {
            for (int i = map.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(map.transform.GetChild(i).gameObject);
        }

        Transform illust = FindDeepInScene("S8S2 Illustration Canvas");
        if (illust != null)
            Object.DestroyImmediate(illust.gameObject);
    }

    private static void ResetGlobalLight(Ctx ctx)
    {
        foreach (Light2D light in Object.FindObjectsOfType<Light2D>(true))
        {
            if (light.lightType != Light2D.LightType.Global || light.gameObject.scene != ctx.Scene)
                continue;

            light.intensity = 1f;
            light.color = Color.white;
        }
    }

    // ================================================================ 맵 복사

    private static bool ImportTheater(Ctx ctx, Transform mapRoot)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TheaterScenePath) == null)
        {
            ctx.Report.Add($"⚠ 원본 씬 없음: {TheaterScenePath}");
            return false;
        }

        Scene src = EditorSceneManager.OpenScene(TheaterScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(ctx.Scene);

        Transform srcMap = FindByPath(src, TheaterMapPath);
        if (srcMap == null)
        {
            ctx.Report.Add($"⚠ {TheaterScenePath} 에서 '{TheaterMapPath}' 를 못 찾음");
            EditorSceneManager.CloseScene(src, true);
            return false;
        }

        GameObject copy = Object.Instantiate(srcMap.gameObject, srcMap.position, srcMap.rotation);
        copy.name = "SmallTheater";
        copy.transform.localScale = srcMap.lossyScale;
        copy.transform.SetParent(mapRoot, true);

        EditorSceneManager.CloseScene(src, true);

        CleanImported(copy.transform, ctx);
        ctx.Report.Add("맵 ← Sequence3S#4 소형 상영관 (좌석·통로·출구·벽 그대로)");
        return true;
    }

    /// <summary>
    /// 3-4 전용 동작(좌석 상호작용, 출구 잠금 대사, 레이 대화 트리거)을 떼어낸다.
    /// 스프라이트 정렬용 스크립트, 렌더러, 벽 콜라이더는 남긴다.
    /// </summary>
    private static void CleanImported(Transform root, Ctx ctx)
    {
        int removed = 0;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        var keep = new HashSet<string> { "LayerSetting", "LayerSetting_child", "SimpleLayerSorting", "CandeFlicker" };

        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
                continue;

            System.Type type = mb.GetType();
            if (type.Assembly.GetName().Name != "Assembly-CSharp" || keep.Contains(type.Name))
                continue;

            ctx.Report.Add($"  └ 3-4 전용 '{type.Name}' 제거 ({mb.name})");
            Object.DestroyImmediate(mb);
            removed++;
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t != null && t.name.Trim() == "Coversation Wall")
            {
                Object.DestroyImmediate(t.gameObject);
                ctx.Report.Add("  └ 레이 대화 트리거 'Coversation Wall' 제거");
            }
        }

        foreach (Rigidbody2D rb in root.GetComponentsInChildren<Rigidbody2D>(true))
            Object.DestroyImmediate(rb);
    }

    /// <summary>
    /// 같은 transform y에서 하루와 루나의 발(스프라이트 아래 끝) 높이 차이.
    /// 하루는 피봇이 가운데라 발이 1칸 아래, 루나는 피봇이 발밑이라 transform 위치가 곧 발이다.
    /// </summary>
    private static float LunaFootCorrection(Ctx ctx, Transform luna)
    {
        var haruSr = ctx.Haru.GetComponentInChildren<SpriteRenderer>(true);
        var lunaSr = luna != null ? luna.GetComponentInChildren<SpriteRenderer>(true) : null;

        if (haruSr == null || lunaSr == null || haruSr.sprite == null || lunaSr.sprite == null)
        {
            ctx.Report.Add("⚠ 하루/루나 스프라이트를 못 찾아 피봇 보정을 건너뜀");
            return 0f;
        }

        float haruFeet = FeetOffset(haruSr, ctx.Haru);
        float lunaFeet = FeetOffset(lunaSr, luna);
        float fix = Mathf.Round((haruFeet - lunaFeet) * 100f) / 100f;

        ctx.Report.Add($"루나 피봇 보정 {fix:+0.##;-0.##;0} (발 위치: 하루 {haruFeet:0.##}, 루나 {lunaFeet:0.##})");
        return fix;
    }

    private static float FeetOffset(SpriteRenderer sr, Transform root)
    {
        Sprite s = sr.sprite;
        return -s.pivot.y / s.pixelsPerUnit * sr.transform.lossyScale.y + (sr.transform.position.y - root.position.y);
    }

    // ================================================================ 오브젝트 생성

    private const string KeiStandDownClipPath = "Assets/Animation/Character/Kei/Kei_Stand_Down.anim";

    /// <summary>
    /// 쓰러진 케이. 전용 리소스가 오기 전까지는 기획 배치도와 같이 아래를 보고 선 모습으로 둔다
    /// (배치도도 임시 리소스로 그렸다). 필름은 가슴 앞에 든 것처럼 겹쳐 놓는다.
    /// </summary>
    private static void PlaceKei(Ctx ctx, Transform kei)
    {
        kei.position = new Vector3(KeiPos.x, KeiPos.y, kei.position.z);
        kei.rotation = Quaternion.identity;
        kei.gameObject.SetActive(true);

        var anim = kei.GetComponentInChildren<Animator>(true);
        if (anim != null)
            anim.enabled = false;

        var sr = kei.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            sr.color = Color.white;
            Sprite down = FirstSprite(KeiStandDownClipPath);
            if (down != null)
                sr.sprite = down;
            else
                ctx.Report.Add("⚠ 케이 아래 보는 자세를 못 찾았다");
        }
    }

    private static Sprite FirstSprite(string clipPath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
            return null;

        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (b.propertyName != "m_Sprite")
                continue;

            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, b);
            if (keys != null && keys.Length > 0)
                return keys[0].value as Sprite;
        }

        return null;
    }

    private static Transform MakeFilm(Ctx ctx, Transform parent, Transform kei)
    {
        Sprite sprite = LoadAsset<Sprite>(FilmSpritePath, ctx);

        var go = new GameObject("Film");
        go.transform.SetParent(parent, false);
        // 케이가 가슴 앞에 든 위치
        go.transform.position = KeiPos + new Vector3(0f, -0.3f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // 케이 위에 놓인다
        var keiSr = kei != null ? kei.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (keiSr != null)
        {
            sr.sortingLayerID = keiSr.sortingLayerID;
            sr.sortingOrder = keiSr.sortingOrder + 1;
        }

        if (sprite != null)
        {
            float s = 0.55f / Mathf.Max(sprite.bounds.size.x, 0.01f);
            go.transform.localScale = new Vector3(s, s, 1f);
        }

        return go.transform;
    }

    private static GameObject MakeFire(Ctx ctx, Transform parent, string name, Vector3 pos, float scale, int order, bool active)
    {
        var prefab = LoadAsset<GameObject>(FirePrefabPath, ctx);
        if (prefab == null)
            return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.Scene);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        // 프리팹의 LayerSetting은 y 기준으로 순서를 덮어쓴다. 좌석/인물 위에 그리도록 직접 정한다.
        foreach (LayerSetting ls in go.GetComponentsInChildren<LayerSetting>(true))
            ls.enabled = false;

        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = order;
        }

        go.SetActive(active);
        return go;
    }

    private static Image MakeOverlayCanvas(Ctx ctx, string canvasName, int order, string imageName, Color color, bool active)
    {
        Transform canvasRoot = EnsureRoot("=====Canvas=====", ctx).transform;

        var go = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.layer = 5;
        go.transform.SetParent(canvasRoot, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;

        var imgGo = new GameObject(imageName, typeof(RectTransform), typeof(Image));
        imgGo.layer = 5;
        imgGo.transform.SetParent(go.transform, false);

        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = imgGo.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        imgGo.SetActive(active);

        return img;
    }

    private static CinemachineVirtualCamera MakeCamera(Transform parent, Vector3 pos)
    {
        var go = new GameObject("S10S1 Virtual Camera");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, -10f);

        // Body 없이 transform 위치가 곧 화면 중심. 컨트롤러가 직접 옮긴다 (틸트 다운 / 출구 구도).
        var vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.OrthographicSize = CamOrthoSize;
        vcam.Priority = 10;
        return vcam;
    }

    private static Transform MakePoint(Transform parent, string name, Vector3 pos)
    {
        Transform t = EnsureChild(parent, name);
        t.position = new Vector3(pos.x, pos.y, 0f);
        return t;
    }

    // ================================================================ 리소스

    private static void LinkSounds(SerializedObject so, Ctx ctx, bool force)
    {
        AudioClip burning = FindByNames<AudioClip>(BurningNames);
        AudioClip projector = FindByNames<AudioClip>(ProjectorNames);

        SetSound(so, "_burningLoop", burning, BurningPlaceholderPath, "불타는 소리", ctx, force);
        SetSound(so, "_projectorLoop", projector, ProjectorPlaceholderPath, "영사기 작동음", ctx, force);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSound(SerializedObject so, string field, AudioClip found, string placeholderPath, string label, Ctx ctx, bool force)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
            return;

        if (found != null)
        {
            p.objectReferenceValue = found;
            ctx.Report.Add($"{label} 연결 ({AssetDatabase.GetAssetPath(found)})");
            return;
        }

        if (p.objectReferenceValue != null && !force)
            return;

        p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(placeholderPath);
        ctx.Report.Add($"⏳ {label} 파일이 아직 없어 임시 클립 사용: {placeholderPath}");
    }

    private static T FindByNames<T>(string[] names)
        where T : Object
    {
        foreach (string n in names)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{n} t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == n)
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }

        return null;
    }

    // ================================================================ 탐색 / 직렬화 헬퍼

    private static Transform FindByPath(Scene scene, string path)
    {
        string[] parts = path.Split('/');

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name.Trim() != parts[0].Trim())
                continue;

            Transform cur = go.transform;
            for (int i = 1; i < parts.Length && cur != null; i++)
            {
                Transform next = null;
                foreach (Transform child in cur)
                {
                    if (child.name.Trim() == parts[i].Trim())
                    {
                        next = child;
                        break;
                    }
                }

                cur = next;
            }

            if (cur != null)
                return cur;
        }

        return null;
    }

    private static Transform FindDeepInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
        }

        return null;
    }

    private static GameObject FindRoot(string name, Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == name)
                return go;
        }

        return null;
    }

    private static GameObject EnsureRoot(string name, Ctx ctx)
    {
        GameObject found = FindRoot(name, ctx.Scene);
        if (found != null)
            return found;

        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, ctx.Scene);
        return go;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null)
            return found;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void AddToBuildSettings(Ctx ctx)
    {
        var scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.Any(s => s.path == TargetScenePath))
            return;

        int after = scenes.FindIndex(s => s.path == AfterScenePath);
        if (after < 0)
            after = scenes.FindIndex(s => s.path == BaseScenePath);

        var entry = new EditorBuildSettingsScene(TargetScenePath, true);

        if (after >= 0)
            scenes.Insert(after + 1, entry);
        else
            scenes.Add(entry);

        EditorBuildSettings.scenes = scenes.ToArray();
        ctx.Report.Add("빌드 설정에 Sequence10S#1 추가 (9-2 바로 뒤)");
    }

    private static T LoadAsset<T>(string path, Ctx ctx)
        where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            ctx.Report.Add($"⚠ 애셋을 못 찾았다: {path}");

        return asset;
    }

    private static void SetVector2(Object target, string field, Vector2 value)
    {
        if (target == null)
            return;

        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
            return;

        p.vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObj(SerializedObject so, string field, Object value, string label, Ctx ctx)
    {
        SerializedProperty p = so.FindProperty(field);

        if (p == null)
        {
            Debug.LogWarning($"{Tag} 컨트롤러에 '{field}' 필드가 없다. 스크립트가 바뀌었는지 확인할 것.");
            return;
        }

        p.objectReferenceValue = value;

        if (label != null && value == null)
            ctx.Report.Add($"⚠ {label} 를 못 찾아 비워둠");
    }

    private static void SetStr(SerializedObject so, string field, string value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null)
            p.stringValue = value;
    }

    private static void SetArray(SerializedObject so, string field, Object[] values)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
            return;

        p.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
