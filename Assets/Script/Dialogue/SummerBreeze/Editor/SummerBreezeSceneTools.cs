using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// SummerBreeze 컷씬 세팅을 씬에 한 번에 만들어 넣는 메뉴 도구.
/// 유니티 상단 메뉴 → Tools → 유메지 [여름 바람] 에서 사용한다.
///
/// 좌표는 절대 좌표로 박지 않는다. 씬에 깔린 맵의 실제 경계를 재서
/// 기획 문서의 34x36 구역도 좌표(왼쪽 위가 0,0인 타일 칸)를 월드로 환산한다.
/// 그래야 맵을 어디에 어떤 크기로 두든 방파제·정류장 위에 정확히 얹힌다.
///
/// 같은 이름이 이미 있으면 다시 만들지 않고 재사용하되 위치는 매번 다시 맞추므로,
/// 맵을 옮기거나 하루를 옮긴 뒤에 다시 눌러도 된다.
/// </summary>
public static class SummerBreezeSceneTools
{
    private const string SceneName = "SummerBreeze";
    private const string DialogueSceneId = "SummerBreeze";

    // 기획 문서의 구역도 크기
    private const float MapCols = 34f;
    private const float MapRows = 36f;

    private const string LoaderPath = "Assets/Script/Dialogue/DialogueLoader.asset";
    private const string LunaPrefabPath = "Assets/Prefab/Character/Luna.prefab";
    private const string RainPrefabPath = "Assets/Epic Toon FX/Prefabs 2D/Weather/Rain2D.prefab";

    private const string RainBlendMaterialPath = "Assets/Shader/SummerWind_RainyBlend.mat";

    private const string EveningBlendMaterialPath = "Assets/Shader/SummerWind_EveningBlend.mat";

    private const string FireworkPrefabA =
        "Assets/Epic Toon FX/Prefabs/Environment/Firework/FireworkBlueCluster.prefab";

    private const string FireworkPrefabB =
        "Assets/Epic Toon FX/Prefabs/Environment/Firework/FireworkRedCluster.prefab";

    private const string BgmBeachPath = "Assets/Sound/SummerBreeze/BGM/BGM_해변 소리.mp3";

    private const string BgmRainPath = "Assets/Sound/SummerBreeze/BGM/BGM_비 그리고 해변 소리.mp3";

    private const string SfxBusPath = "Assets/Sound/SummerBreeze/SFX/SFX_버스 발차.mp3";
    private const string SfxFireworksPath = "Assets/Sound/SummerBreeze/SFX/SFX_불꽃놀이.mp3";
    private const string SfxShutterPath = "Assets/Sound/Sequence1/SFX/촬영 SFX.mp3";

    /// <summary>
    /// 구역도 기준 타일 좌표. col은 왼쪽부터, row는 위(바다)부터 센다.
    ///
    /// 구역도에서 읽은 값:
    ///   바다 A   row 0~11
    ///   해변 B   row 12~28
    ///   인도     row 29
    ///   도로 D   row 30~35
    ///   방파제 C 머리 col 21~27 / row 6~8,  기둥 col 23~25 / row 9~19
    ///   정류장   col 12~17 / row 26~29
    /// </summary>
    private readonly struct Tile
    {
        public readonly string Name;
        public readonly float Col;
        public readonly float Row;

        public Tile(string name, float col, float row)
        {
            Name = name;
            Col = col;
            Row = row;
        }
    }

    // 방파제 기둥 한가운데 세로줄과 맨 끝(머리) 줄
    private const float PierCol = 24f;
    private const float PierEndRow = 7f;

    // 정류장 안쪽
    private const float StopCol = 15f;
    private const float StopRow = 28f;

    private static readonly Tile[] CameraTiles =
    {
        new Tile("Cam_S1_Start", StopCol, StopRow), // 정류장에서 팬 시작
        new Tile("Cam_S1_Beach", 12f, 20f), // 하루 위치로 덮어씀
        new Tile("Cam_S2_Start", 22f, 11f),
        new Tile("Cam_S2_End", 23f, 8f),
        new Tile("Cam_S3", StopCol, StopRow - 3f),
        new Tile("Cam_S4_BusStop", StopCol, StopRow - 2f),
        new Tile("Cam_S4_Pier", PierCol, PierEndRow + 2f),
    };

    private static readonly Tile[] WaypointTiles =
    {
        // S#1은 하루의 현재 위치 기준이라 아래 SnapSceneOne에서 다시 잡는다
        new Tile("S1_Haru_Stand", 12f, 20f),
        new Tile("S1_Luna_Enter", 6f, 19f),
        new Tile("S1_Luna_Shutter", 12f, 19f),
        new Tile("S1_Luna_Exit_1", 18f, 19f),
        new Tile("S1_Luna_Exit_2", 26f, 19f),
        // S#2 방파제. 맨 끝(머리)이 row 7, 기둥을 따라 내려올수록 row가 커진다
        new Tile("S2_Haru_Stand", PierCol, PierEndRow + 3f),
        new Tile("S2_Luna_Enter", PierCol, PierEndRow + 9f),
        new Tile("S2_Luna_Stop", PierCol, PierEndRow + 5f),
        new Tile("S2_Luna_End", PierCol + 1f, PierEndRow),
        new Tile("S2_Haru_End", PierCol, PierEndRow + 2f),
        new Tile("S2_Luna_FrameIn", PierCol, PierEndRow),
        // S#3 빗속 정류장
        new Tile("S3_Luna_Stand", StopCol, StopRow),
        new Tile("S3_Haru_Enter", StopCol - 4f, StopRow),
        new Tile("S3_Haru_Stop", StopCol - 1f, StopRow),
        // S#4 이별 → 방파제
        new Tile("S4_Luna_Stand", StopCol, StopRow),
        new Tile("S4_Haru_Enter", StopCol - 4f, StopRow),
        new Tile("S4_Haru_Stop", StopCol - 1f, StopRow),
        new Tile("S4_Haru_Seat", PierCol, PierEndRow),
        new Tile("S4_EmptySeat", PierCol + 1f, PierEndRow),
    };

    // 맵 경계와 화면 반폭. 메뉴를 실행할 때마다 씬에서 다시 잰다.
    private static Bounds _map;
    private static bool _hasMap;
    private static float _halfW;
    private static float _halfH;

    // ================================================================ 메뉴

    [MenuItem("Tools/유메지 [여름 바람]/컷씬 세팅 생성 (전체 자동)", false, 1)]
    public static void BuildCutsceneSetup()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.name != SceneName)
        {
            EditorUtility.DisplayDialog(
                "여름 바람 컷씬",
                $"활성 씬이 '{scene.name}'입니다.\n{SceneName} 씬을 연 다음 다시 실행해주세요.",
                "확인"
            );
            return;
        }

        var report = new List<string>();

        var haru = Object.FindObjectOfType<PlayerMove_Test_Lerp>();
        var dialogueManager = Object.FindObjectOfType<DialogueManager>();
        var vcam = Object.FindObjectOfType<CinemachineVirtualCamera>();

        MeasureMap(vcam, report);

        // ---------- 그룹 ----------
        GameObject cutsceneRoot = EnsureRoot("=====Cutscene=====", report);
        GameObject mapRoot = FindRoot("=====Map=====") ?? EnsureRoot("=====Map=====", report);
        GameObject characterRoot =
            FindRoot("=====Character=====") ?? EnsureRoot("=====Character=====", report);

        // ---------- 카메라 ----------
        GameObject rig = EnsureChild(cutsceneRoot.transform, "CameraRig", report);
        GameObject camGroup = EnsureChild(cutsceneRoot.transform, "CameraPoints", report);

        var camMap = new Dictionary<string, Transform>();
        foreach (Tile t in CameraTiles)
        {
            Transform tr = EnsureChild(camGroup.transform, t.Name, report).transform;
            tr.position = ClampToView(TileToWorld(t.Col, t.Row));
            camMap[t.Name] = tr;
        }

        // ---------- 웨이포인트 ----------
        GameObject wpGroup = EnsureChild(cutsceneRoot.transform, "Waypoints", report);

        var wpMap = new Dictionary<string, Transform>();
        foreach (Tile t in WaypointTiles)
        {
            Transform tr = EnsureChild(wpGroup.transform, t.Name, report).transform;
            tr.position = TileToWorld(t.Col, t.Row);
            wpMap[t.Name] = tr;
        }

        // S#1은 하루가 서 있는 자리 그대로 시작하므로 그 주변으로 다시 잡는다
        SnapSceneOne(cutsceneRoot.transform, haru != null ? haru.transform : null, report);

        rig.transform.position = camMap["Cam_S1_Start"].position;

        // ---------- 효과 / 인물 ----------
        Image moodOverlay = EnsureMoodOverlay(cutsceneRoot.transform, report);
        GameObject rain = EnsureRain(rig.transform, report);
        GameObject fireworks = EnsureFireworks(mapRoot.transform, report);
        GameObject luna = EnsureLuna(
            characterRoot.transform,
            wpMap["S1_Luna_Enter"].position,
            report
        );

        // ---------- 컨트롤러 ----------
        GameObject controllerObj = EnsureChild(cutsceneRoot.transform, "SummerBreezeCutscene", report);

        var controller = controllerObj.GetComponent<SummerBreezeCutsceneController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<SummerBreezeCutsceneController>(controllerObj);
            report.Add("컷씬 컨트롤러 추가");
        }

        RemoveDuplicateControllers(controller, report);

        var so = new SerializedObject(controller);

        SetObj(so, "dialogueManager", dialogueManager, "대화 매니저", report);
        SetObj(so, "dialogueLoader", LoadAsset<DialogueLoader>(LoaderPath), "대화 로더", report);
        SetStr(so, "sceneId", DialogueSceneId);
        SetStr(so, "puzzleId", "SummerBreezeCutscene");

        SetObj(so, "_haru", haru, "하루", report);
        SetObj(so, "_luna", luna != null ? luna.transform : null, "루나", report);
        SetObj(
            so,
            "_lunaAnimator",
            luna != null ? luna.GetComponentInChildren<Animator>(true) : null,
            "루나 애니메이터",
            report
        );

        SetObj(so, "_moodOverlay", moodOverlay, "색 오버레이", report);
        SetObj(so, "_rain", rain, "비 효과", report);
        SetObj(
            so,
            "_rainBlendMaterial",
            LoadAsset<Material>(RainBlendMaterialPath),
            "비 화면 합성 머티리얼",
            report
        );
        SetObj(
            so,
            "_eveningBlendMaterial",
            LoadAsset<Material>(EveningBlendMaterialPath),
            "저녁 화면 합성 머티리얼",
            report
        );        SetObj(so, "_fireworks", fireworks, "불꽃", report);

        SetObj(so, "_vcam", vcam, "버추얼 카메라", report);
        SetObj(so, "_cameraRig", rig.transform, "카메라 리그", report);
        SetObj(so, "_camS1Start", camMap["Cam_S1_Start"], null, report);
        SetObj(so, "_camS1Beach", camMap["Cam_S1_Beach"], null, report);
        SetObj(so, "_camS2Start", camMap["Cam_S2_Start"], null, report);
        SetObj(so, "_camS2End", camMap["Cam_S2_End"], null, report);
        SetObj(so, "_camS3", camMap["Cam_S3"], null, report);
        SetObj(so, "_camS4BusStop", camMap["Cam_S4_BusStop"], null, report);
        SetObj(so, "_camS4Pier", camMap["Cam_S4_Pier"], null, report);

        SetObj(so, "_s1HaruStand", wpMap["S1_Haru_Stand"], null, report);
        SetObj(so, "_s1LunaEnter", wpMap["S1_Luna_Enter"], null, report);
        SetObj(so, "_s1LunaShutter", wpMap["S1_Luna_Shutter"], null, report);
        SetArray(
            so,
            "_s1LunaExitPath",
            new Object[] { wpMap["S1_Luna_Exit_1"], wpMap["S1_Luna_Exit_2"] }
        );

        SetObj(so, "_s2HaruStand", wpMap["S2_Haru_Stand"], null, report);
        SetObj(so, "_s2LunaEnter", wpMap["S2_Luna_Enter"], null, report);
        SetObj(so, "_s2LunaStop", wpMap["S2_Luna_Stop"], null, report);
        SetObj(so, "_s2LunaEnd", wpMap["S2_Luna_End"], null, report);
        SetObj(so, "_s2HaruEnd", wpMap["S2_Haru_End"], null, report);
        SetObj(so, "_s2LunaFrameIn", wpMap["S2_Luna_FrameIn"], null, report);

        SetObj(so, "_s3HaruEnter", wpMap["S3_Haru_Enter"], null, report);
        SetObj(so, "_s3HaruStop", wpMap["S3_Haru_Stop"], null, report);
        SetObj(so, "_s3LunaStand", wpMap["S3_Luna_Stand"], null, report);

        SetObj(so, "_s4HaruEnter", wpMap["S4_Haru_Enter"], null, report);
        SetObj(so, "_s4HaruStop", wpMap["S4_Haru_Stop"], null, report);
        SetObj(so, "_s4LunaStand", wpMap["S4_Luna_Stand"], null, report);
        SetObj(so, "_s4HaruSeat", wpMap["S4_Haru_Seat"], null, report);
        SetObj(so, "_s4EmptySeat", wpMap["S4_EmptySeat"], null, report);

        SetObj(so, "_bgmBeach", LoadAsset<AudioClip>(BgmBeachPath), "BGM 해변", report);
        SetObj(so, "_bgmRainBeach", LoadAsset<AudioClip>(BgmRainPath), "BGM 비+해변", report);
        SetObj(so, "_sfxShutter", LoadAsset<AudioClip>(SfxShutterPath), "셔터음", report);
        SetObj(so, "_sfxBusDeparture", LoadAsset<AudioClip>(SfxBusPath), "버스 발차", report);
        SetObj(so, "_sfxFireworks", LoadAsset<AudioClip>(SfxFireworksPath), "불꽃놀이", report);

        so.ApplyModifiedPropertiesWithoutUndo();

        // 컷씬은 고정 구도라 카메라가 하루를 따라다니면 안 된다
        if (vcam != null)
        {
            Undo.RecordObject(vcam, "카메라 리그 연결");
            vcam.Follow = rig.transform;
            vcam.LookAt = null;
            EditorUtility.SetDirty(vcam);
            report.Add("버추얼 카메라 Follow → CameraRig");
        }

        // 촬영 모션의 플래시 VFX는 대낮 해변에서 튀므로 이 씬에서만 비운다
        if (haru != null && ClearFlashVfx(haru))
            report.Add("하루의 플래시 VFX 비움");

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = controllerObj;

        Debug.Log("[여름 바람] 컷씬 세팅 완료\n - " + string.Join("\n - ", report));
    }

    /// <summary>맵이나 하루를 옮긴 뒤 지점들만 다시 맞출 때 쓴다.</summary>
    [MenuItem("Tools/유메지 [여름 바람]/맵 기준으로 위치 다시 맞추기", false, 10)]
    public static void RepositionAll()
    {
        GameObject root = FindRoot("=====Cutscene=====");

        if (root == null)
        {
            Debug.LogWarning("[여름 바람] 컷씬 그룹이 없다. '컷씬 세팅 생성'을 먼저 실행할 것.");
            return;
        }

        var report = new List<string>();
        var vcam = Object.FindObjectOfType<CinemachineVirtualCamera>();
        var haru = Object.FindObjectOfType<PlayerMove_Test_Lerp>();

        MeasureMap(vcam, report);

        foreach (Tile t in CameraTiles)
            MovePoint(root, t.Name, ClampToView(TileToWorld(t.Col, t.Row)));

        foreach (Tile t in WaypointTiles)
            MovePoint(root, t.Name, TileToWorld(t.Col, t.Row));

        SnapSceneOne(root.transform, haru != null ? haru.transform : null, report);

        Transform rig = FindDeep(root.transform, "CameraRig");
        Transform camStart = FindDeep(root.transform, "Cam_S1_Start");
        if (rig != null && camStart != null)
            rig.position = camStart.position;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[여름 바람] 위치 재배치 완료\n - " + string.Join("\n - ", report));
    }

    [MenuItem("Tools/유메지 [여름 바람]/연결 상태 검사", false, 20)]
    public static void Validate()
    {
        var all = Object.FindObjectsOfType<SummerBreezeCutsceneController>(true);

        if (all.Length == 0)
        {
            Debug.LogWarning("[여름 바람] 씬에 컷씬 컨트롤러가 없다. '컷씬 세팅 생성'을 먼저 실행할 것.");
            return;
        }

        if (all.Length > 1)
        {
            var paths = new List<string>();
            foreach (SummerBreezeCutsceneController c in all)
                paths.Add(HierarchyPath(c.transform));

            Debug.LogError(
                "[여름 바람] 컷씬 컨트롤러가 " + all.Length + "개다. 대사가 두 번 나오는 원인이다.\n - "
                    + string.Join("\n - ", paths)
                    + "\n'컷씬 세팅 생성'을 다시 실행하면 자동으로 정리된다."
            );
        }

        SummerBreezeCutsceneController controller = all[0];

        var so = new SerializedObject(controller);
        var empty = new List<string>();

        SerializedProperty p = so.GetIterator();
        bool enter = true;

        while (p.NextVisible(enter))
        {
            enter = false;

            if (
                p.propertyType == SerializedPropertyType.ObjectReference
                && p.objectReferenceValue == null
            )
                empty.Add(p.displayName);
            else if (
                p.isArray
                && p.propertyType != SerializedPropertyType.String
                && p.arraySize == 0
            )
                empty.Add($"{p.displayName} (빈 배열)");
        }

        if (empty.Count == 0)
            Debug.Log("[여름 바람] 비어 있는 참조 없음.");
        else
            Debug.LogWarning("[여름 바람] 아직 비어 있는 항목:\n - " + string.Join("\n - ", empty));
    }

    // ================================================================ 맵 측정 / 좌표 환산

    /// <summary>
    /// 씬에 깔린 맵의 실제 경계를 잰다.
    /// 인물·컷씬 오브젝트·파티클은 빼고, 남은 렌더러들의 합집합을 맵으로 본다.
    /// </summary>
    private static void MeasureMap(CinemachineVirtualCamera vcam, List<string> report)
    {
        _hasMap = false;

        var bounds = new Bounds();
        bool first = true;

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Contains("Character") || root.name.Contains("Cutscene"))
                continue;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer)
                    continue;

                // UI(캔버스 아래)와 캐릭터 스프라이트는 맵이 아니다
                if (r.GetComponentInParent<Canvas>() != null)
                    continue;

                if (r.GetComponent<PlayerMove_Test_Lerp>() != null)
                    continue;

                if (first)
                {
                    bounds = r.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }

        // 카메라 화면 반폭. 구도점이 맵 밖을 비추지 않도록 안쪽으로 밀 때 쓴다.
        float ortho = vcam != null ? vcam.m_Lens.OrthographicSize : 5f;
        float aspect =
            Camera.main != null && Camera.main.aspect > 0.01f ? Camera.main.aspect : 16f / 9f;

        _halfH = ortho;
        _halfW = ortho * aspect;

        if (first)
        {
            report.Add("⚠ 맵 렌더러를 못 찾아 좌표를 환산할 수 없다. 맵을 배치한 뒤 '맵 기준으로 위치 다시 맞추기'를 실행할 것");
            return;
        }

        _map = bounds;
        _hasMap = true;

        report.Add(
            $"맵 경계 ({bounds.min.x:0.##}, {bounds.min.y:0.##}) ~ ({bounds.max.x:0.##}, {bounds.max.y:0.##}), "
                + $"타일 크기 {bounds.size.x / MapCols:0.###} x {bounds.size.y / MapRows:0.###}"
        );
    }

    /// <summary>구역도의 타일 칸(col: 왼쪽부터, row: 위부터)을 월드 좌표로 바꾼다.</summary>
    private static Vector3 TileToWorld(float col, float row)
    {
        if (!_hasMap)
            return new Vector3(col, MapRows - row, 0f);

        // 칸 한가운데를 가리키도록 0.5 보정
        float u = (col + 0.5f) / MapCols;
        float v = 1f - (row + 0.5f) / MapRows;

        return new Vector3(
            Mathf.Lerp(_map.min.x, _map.max.x, u),
            Mathf.Lerp(_map.min.y, _map.max.y, v),
            0f
        );
    }

    private static float TileWidth => _hasMap ? _map.size.x / MapCols : 1f;
    private static float TileHeight => _hasMap ? _map.size.y / MapRows : 1f;

    /// <summary>구도점이 맵 밖을 비추지 않도록 화면 반폭만큼 안쪽으로 민다.</summary>
    private static Vector3 ClampToView(Vector3 p)
    {
        if (!_hasMap)
            return p;

        float minX = _map.min.x + _halfW;
        float maxX = _map.max.x - _halfW;
        float minY = _map.min.y + _halfH;
        float maxY = _map.max.y - _halfH;

        p.x = minX <= maxX ? Mathf.Clamp(p.x, minX, maxX) : _map.center.x;
        p.y = minY <= maxY ? Mathf.Clamp(p.y, minY, maxY) : _map.center.y;
        p.z = 0f;

        return p;
    }

    /// <summary>
    /// S#1 지점들을 하루가 서 있는 자리 기준으로 다시 배치한다.
    /// 루나의 등장/퇴장은 타일 수가 아니라 화면 반폭 기준이라,
    /// 맵이나 카메라 크기가 어떻든 정확히 화면 밖에서 들어와 화면 밖으로 나간다.
    /// </summary>
    private static void SnapSceneOne(Transform root, Transform haru, List<string> report)
    {
        if (haru == null)
        {
            report.Add("⚠ 하루를 못 찾아 S#1 위치는 맵 기본값 그대로 뒀다");
            return;
        }

        Vector3 o = haru.position;
        float lane = TileHeight; // 하루보다 한 칸 위(바다 쪽) 줄
        float off = _halfW + TileWidth * 2f; // 화면 밖

        MovePoint(root, "S1_Haru_Stand", o);
        MovePoint(root, "S1_Luna_Shutter", new Vector3(o.x, o.y + lane, 0f));
        MovePoint(root, "S1_Luna_Enter", new Vector3(o.x - off, o.y + lane, 0f));
        MovePoint(root, "S1_Luna_Exit_1", new Vector3(o.x + _halfW * 0.5f, o.y + lane, 0f));
        MovePoint(root, "S1_Luna_Exit_2", new Vector3(o.x + off, o.y + lane, 0f));
        MovePoint(root, "Cam_S1_Beach", ClampToView(o));

        report.Add($"S#1을 하루 현재 위치 ({o.x:0.##}, {o.y:0.##}) 기준으로 배치");
    }

    private static void MovePoint(GameObject root, string name, Vector3 pos) =>
        MovePoint(root.transform, name, pos);

    private static void MovePoint(Transform root, string name, Vector3 pos)
    {
        Transform t = FindDeep(root, name);
        if (t == null)
            return;

        Undo.RecordObject(t, "여름 바람 위치 배치");
        t.position = new Vector3(pos.x, pos.y, t.position.z);
        EditorUtility.SetDirty(t);
    }

    // ================================================================ 생성 헬퍼

    private static GameObject FindRoot(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == name)
                return go;
        }

        return null;
    }

    private static GameObject EnsureRoot(string name, List<string> report)
    {
        GameObject found = FindRoot(name);
        if (found != null)
            return found;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "여름 바람 컷씬 세팅");
        report.Add($"'{name}' 그룹 생성");
        return go;
    }

    private static GameObject EnsureChild(Transform parent, string name, List<string> report)
    {
        Transform found = parent.Find(name);
        if (found != null)
            return found.gameObject;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "여름 바람 컷씬 세팅");
        go.transform.SetParent(parent, false);
        report.Add($"'{name}' 생성");
        return go;
    }

    /// <summary>
    /// 컷씬 감독이 둘 이상이면 각자 코루틴을 돌려서 같은 대사 블록을 두 번 시작한다.
    /// 정식 오브젝트에 붙은 것만 남기고 나머지는 컴포넌트만 떼어낸다.
    /// (GameObject 자체는 사용자가 만든 것일 수 있으니 지우지 않는다)
    /// </summary>
    private static void RemoveDuplicateControllers(
        SummerBreezeCutsceneController keep,
        List<string> report
    )
    {
        foreach (SummerBreezeCutsceneController c in
            Object.FindObjectsOfType<SummerBreezeCutsceneController>(true))
        {
            if (c == keep)
                continue;

            report.Add($"⚠ 중복 컨트롤러 제거: '{HierarchyPath(c.transform)}' (대사 두 번 나오던 원인)");
            Undo.DestroyObjectImmediate(c);
        }
    }

    private static string HierarchyPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return t;
        }

        return null;
    }

    /// <summary>
    /// 월드 위, 대사창 아래에 깔리는 색 오버레이.
    /// 대사 캔버스가 sortingOrder 0이라 -1로 두면 맵만 덮고 UI는 건드리지 않는다.
    /// </summary>
    private static Image EnsureMoodOverlay(Transform parent, List<string> report)
    {
        Transform found = parent.Find("MoodOverlayCanvas");
        GameObject canvasObj;

        if (found != null)
        {
            canvasObj = found.gameObject;
        }
        else
        {
            canvasObj = new GameObject("MoodOverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasObj, "여름 바람 컷씬 세팅");
            canvasObj.transform.SetParent(parent, false);
            report.Add("'MoodOverlayCanvas' 생성");
        }

        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;

        Transform imgTr = canvasObj.transform.Find("MoodOverlay");
        GameObject imgObj;

        if (imgTr != null)
        {
            imgObj = imgTr.gameObject;
        }
        else
        {
            imgObj = new GameObject("MoodOverlay", typeof(Image));
            Undo.RegisterCreatedObjectUndo(imgObj, "여름 바람 컷씬 세팅");
            imgObj.transform.SetParent(canvasObj.transform, false);
            report.Add("'MoodOverlay' 생성");
        }

        var rect = imgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = imgObj.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0f);

        return image;
    }

    private static GameObject EnsureRain(Transform parent, List<string> report)
    {
        Transform found = parent.Find("Rain");
        GameObject rain;

        if (found != null)
        {
            rain = found.gameObject;
        }
        else
        {
            var prefab = LoadAsset<GameObject>(RainPrefabPath);

            if (prefab != null)
            {
                rain = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                report.Add("비 파티클(Rain2D) 생성");
            }
            else
            {
                rain = new GameObject("Rain");
                report.Add("⚠ Rain2D 프리팹을 못 찾아 빈 오브젝트만 만들었다");
            }

            Undo.RegisterCreatedObjectUndo(rain, "여름 바람 컷씬 세팅");
            rain.name = "Rain";
            rain.transform.SetParent(parent, false);
        }

        // 카메라 리그의 자식이라 화면을 따라다닌다. 화면 위쪽에서 뿌리도록 올려둔다.
        rain.transform.localPosition = new Vector3(0f, _halfH, 0f);
        rain.SetActive(false);

        return rain;
    }

    private static GameObject EnsureFireworks(Transform parent, List<string> report)
    {
        Transform found = parent.Find("Fireworks");
        GameObject group;

        if (found != null)
        {
            group = found.gameObject;
        }
        else
        {
            group = new GameObject("Fireworks");
            Undo.RegisterCreatedObjectUndo(group, "여름 바람 컷씬 세팅");
            group.transform.SetParent(parent, false);

            AddFirework(group.transform, FireworkPrefabA, new Vector3(-TileWidth * 4f, 0f, 0f));
            AddFirework(group.transform, FireworkPrefabB, new Vector3(TileWidth * 4f, TileHeight * 2f, 0f));

            report.Add("불꽃 생성 (반복 재생은 파티클의 Looping을 켤 것)");
        }

        // 마지막 방파제 구도 안, 바다 위쪽에서 터지도록
        group.transform.position = TileToWorld(PierCol - 3f, PierEndRow - 3f);
        group.SetActive(false);

        return group;
    }

    private static void AddFirework(Transform parent, string path, Vector3 localPos)
    {
        var prefab = LoadAsset<GameObject>(path);
        if (prefab == null)
            return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, "여름 바람 컷씬 세팅");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
    }

    private static GameObject EnsureLuna(Transform parent, Vector3 pos, List<string> report)
    {
        Transform found = parent.Find("Luna");

        if (found != null)
        {
            found.position = new Vector3(pos.x, pos.y, found.position.z);
            return found.gameObject;
        }

        var prefab = LoadAsset<GameObject>(LunaPrefabPath);
        if (prefab == null)
        {
            report.Add("⚠ 루나 프리팹을 못 찾았다");
            return null;
        }

        var luna = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(luna, "여름 바람 컷씬 세팅");
        luna.name = "Luna";
        luna.transform.SetParent(parent, false);
        luna.transform.position = pos;
        report.Add("루나 배치");

        return luna;
    }

    private static bool ClearFlashVfx(PlayerMove_Test_Lerp haru)
    {
        var so = new SerializedObject(haru);
        bool changed = false;

        foreach (string field in new[] { "flashVFXPrefab", "flashVFXPrefabUp" })
        {
            SerializedProperty p = so.FindProperty(field);

            if (p != null && p.objectReferenceValue != null)
            {
                p.objectReferenceValue = null;
                changed = true;
            }
        }

        if (changed)
            so.ApplyModifiedPropertiesWithoutUndo();

        return changed;
    }

    // ================================================================ 직렬화 헬퍼

    private static T LoadAsset<T>(string path)
        where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            Debug.LogWarning($"[여름 바람] 애셋을 못 찾았다: {path}");

        return asset;
    }

    private static void SetObj(
        SerializedObject so,
        string field,
        Object value,
        string label,
        List<string> report
    )
    {
        SerializedProperty p = so.FindProperty(field);

        if (p == null)
        {
            Debug.LogWarning($"[여름 바람] 컨트롤러에 '{field}' 필드가 없다. 스크립트가 바뀌었는지 확인할 것.");
            return;
        }

        p.objectReferenceValue = value;

        if (label == null)
            return;

        report.Add(value != null ? $"{label} 연결" : $"⚠ {label} 를 못 찾아 비워둠");
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
