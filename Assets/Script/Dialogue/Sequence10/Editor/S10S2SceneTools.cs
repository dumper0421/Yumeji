using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [10-2] 마지막 상영 (엔딩 1) 씬을 한 번에 만들어 주는 메뉴 도구.
/// 유니티 상단 메뉴 → Tools → 유메지 [10-2 마지막 상영] 에서 사용한다.
///
/// 베이스는 Sequence8S#2(매니저/대화창/하루/루나가 연결된 씬)를 복사하고 케이는 뺀다.
///   추상공간 ← Sequence1S#1의 텔레비전 방 (BG_LostRoom_v1 + 텔레비전)
///   해변     ← SummerWind의 해변 맵 (파도 애니메이션 버전이 오기 전까지 정지 맵)
///
/// 해변은 추상공간과 "같은 화면 구도"가 되도록 옮겨서 겹쳐 둔다.
/// 그래서 디졸브할 때 카메라와 인물은 움직이지 않고 해변의 알파만 올라간다. (기획서 4-2, 8)
/// </summary>
public static class S10S2SceneTools
{
    private const string Tag = "[10-2 마지막 상영]";

    private const string BaseScenePath = "Assets/Scenes/2026PlayX4/Sequence8S#2.unity";
    private const string TargetScenePath = "Assets/Scenes/2026PlayX4/Sequence10S#2.unity";
    private const string TargetSceneName = "Sequence10S#2";
    private const string AfterScenePath = "Assets/Scenes/2026PlayX4/Sequence10S#1.unity";

    private const string RoomScenePath = "Assets/Scenes/2026PlayX4/Sequence1S#1.unity";
    private const string BeachScenePath = "Assets/Scenes/2026PlayX4/SummerWind.unity";

    private const string LoaderPath = "Assets/Script/Dialogue/DialogueLoader.asset";

    // 쪼그려 앉은 뒷모습. 하루는 서 있는 자세와 발 높이가 같도록 피봇을 맞췄고, 루나는 발밑이 피봇이다.
    private const string HaruSitSpritePath = "Assets/Sprites/Character/Haru/Haru_SquatBack_v1.png";
    private const string LunaSitSpritePath = "Assets/Sprites/Character/Luna/Luna_SquatBack_v1.png";

    // 사운드는 모두 업로드 예정. 그 전까지 쓰는 임시 클립과, 들어오면 찾을 이름들.
    private const string TvPlaceholder = "Assets/Resources/SFX/SFX_TelevisionNoise.mp3";
    private const string WavesPlaceholder = "Assets/Sound/SummerBreeze/BGM/BGM_해변 소리.mp3";
    private const string ProjectorPlaceholder = "Assets/Sound/Sequence2/S#2/BGM_영사기_돌아가는_소리.mp3";
    private static readonly string[] TvNames = { "BGM_텔레비전", "BGM_TV", "BGM_Television", "텔레비전 BGM" };
    private static readonly string[] WavesNames = { "BGM_파도", "BGM_파도소리", "BGM_파도_소리", "BGM_Waves", "파도 소리" };
    private static readonly string[] ProjectorNames = { "BGM_영사기", "BGM_Projector", "영사기 BGM" };

    // 기획서 해변 이미지(= 카메라 한 화면)의 중심을 해변 맵 좌표로 옮긴 값.
    // 화면 위쪽 1/3에 파도 거품선, 그 아래 젖은 모래 띠, 인물은 마른 모래 위.
    private static readonly Vector3 BeachViewCenter = new Vector3(-4f, 4.5f, 0f);

    // 두 사람의 몸 중심 위치 (화면 중심 = 텔레비전 가로 중심 기준). 기획서 추상공간 이미지에서 잰 값.
    // 루나 왼쪽, 하루 오른쪽, 둘 다 텔레비전 바로 아래에서 위를 본다.
    private const float LunaOffsetX = -0.8f;
    private const float HaruOffsetX = 0.5f;
    private const float BodyBelowCenter = 0.62f;
    private const float LunaWalkTiles = 2f;      // 바다 쪽(거품선까지) 걸어가는 칸 수
    private const float CamOrthoSize = 5f;

    private class Ctx
    {
        public readonly List<string> Report = new List<string>();
        public Scene Scene;
        public Transform Haru;
        public TMP_FontAsset Font;
    }

    // ================================================================ 메뉴

    [MenuItem("Tools/유메지 [10-2 마지막 상영]/씬 생성 (Sequence10S#2)", false, 1)]
    public static void BuildSceneMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
        {
            bool ok = EditorUtility.DisplayDialog(
                "10-2 마지막 상영",
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

    [MenuItem("Tools/유메지 [10-2 마지막 상영]/리소스 다시 연결 (텔레비전·파도·영사기)", false, 10)]
    public static void RelinkMenu()
    {
        var controller = Object.FindObjectOfType<Sequence10Scene2DialogueController>(true);

        if (controller == null)
        {
            Debug.LogWarning($"{Tag} 열린 씬에 10-2 컨트롤러가 없다. {TargetSceneName} 씬을 열고 실행할 것.");
            return;
        }

        var ctx = new Ctx();
        LinkSounds(new SerializedObject(controller), ctx, force: true);

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log($"{Tag} 리소스 연결\n - " + string.Join("\n - ", ctx.Report));
    }

    /// <summary>배치모드용: Unity.exe -batchmode -executeMethod S10S2SceneTools.BuildFromCommandLine</summary>
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

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(BaseScenePath, TargetScenePath))
        {
            Debug.LogError($"{Tag} 베이스 씬 복사 실패: {BaseScenePath}");
            return false;
        }

        ctx.Scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        ctx.Font = FindFont();

        var haruMove = Object.FindObjectOfType<PlayerMove_Test_Lerp>(true);
        ctx.Haru = haruMove != null ? haruMove.transform : null;

        if (ctx.Haru == null)
        {
            Debug.LogError($"{Tag} 베이스 씬에서 하루를 못 찾았다.");
            return false;
        }

        StripEightTwo(ctx);

        Transform kei = FindDeepInScene("Kei");
        if (kei != null)
        {
            Object.DestroyImmediate(kei.gameObject);
            ctx.Report.Add("케이 제거 (10-2에는 등장하지 않음)");
        }

        // ---------- 맵 ----------
        Transform mapRoot = EnsureRoot("=====Map=====", ctx).transform;

        Vector3 camCenter;
        Transform room = ImportRoom(ctx, mapRoot, out camCenter);
        Transform beach = ImportBeach(ctx, mapRoot, camCenter);

        if (room == null || beach == null)
        {
            Debug.LogError($"{Tag} 맵 복사 실패\n - " + string.Join("\n - ", ctx.Report));
            return false;
        }

        // ---------- 인물 ----------
        Transform root = EnsureRoot("=====S10S2=====", ctx).transform;
        Transform points = EnsureChild(root, "Points");

        Sprite haruSit = LoadPose(HaruSitSpritePath, ctx);
        Sprite lunaSit = LoadPose(LunaSitSpritePath, ctx);

        Transform luna = FindDeepInScene("Luna");
        if (luna != null)
            luna.gameObject.SetActive(true); // 8-2 베이스에서는 꺼져 있다
        else
            ctx.Report.Add("⚠ 루나를 못 찾았다");

        // 몸 중심을 화면상 같은 높이에 맞춘다 (하루는 피봇 가운데, 루나는 발밑이라 transform으로 맞추면 어긋난다)
        float bodyY = camCenter.y - BodyBelowCenter;
        PlaceByBodyCenter(ctx, ctx.Haru, haruSit, camCenter.x + HaruOffsetX, bodyY, "하루");
        if (luna != null)
            PlaceByBodyCenter(ctx, luna, lunaSit, camCenter.x + LunaOffsetX, bodyY, "루나");

        Transform lunaSea = MakePoint(
            points,
            "LunaSeaPoint",
            (luna != null ? luna.position : camCenter) + new Vector3(0f, LunaWalkTiles, 0f)
        );

        // ---------- 일러스트 / 카메라 ----------
        MakeIllustrationCanvas(ctx, out Image illust, out TMP_Text illustLabel);
        CinemachineVirtualCamera vcam = MakeCamera(root, camCenter);

        // ---------- 컨트롤러 ----------
        GameObject controllerObj = EnsureChild(root, "S10S2Controller").gameObject;
        var controller = controllerObj.AddComponent<Sequence10Scene2DialogueController>();
        var so = new SerializedObject(controller);

        SetObj(so, "dialogueManager", Object.FindObjectOfType<DialogueManager>(true), "대화 매니저", ctx);
        SetObj(so, "dialogueLoader", AssetDatabase.LoadAssetAtPath<DialogueLoader>(LoaderPath), "대화 로더", ctx);
        SetStr(so, "sceneId", "Sequence10Scene2");
        SetStr(so, "puzzleId", "S10S2");

        SetObj(so, "_playerMove", ctx.Haru.GetComponent<PlayerMove_Test_Lerp>(), "하루", ctx);
        SetObj(so, "_haruSitSprite", haruSit, "하루 앉은 자세", ctx);
        SetObj(so, "_luna", luna, "루나", ctx);
        SetObj(so, "_lunaAnimator", luna != null ? luna.GetComponentInChildren<Animator>(true) : null, "루나 애니메이터", ctx);
        SetObj(so, "_lunaSitSprite", lunaSit, "루나 앉은 자세", ctx);
        SetObj(so, "_lunaSeaPoint", lunaSea, null, ctx);

        SetArray(so, "_abstractSpace", room.Cast<Transform>().Select(t => (Object)t.gameObject).ToArray());
        SetObj(so, "_beach", beach.gameObject, "해변", ctx);
        SetObj(so, "_vcam", vcam, "카메라", ctx);

        SetObj(so, "_illustrationImage", illust, null, ctx);
        SetObj(so, "_illustrationPlaceholder", illustLabel, null, ctx);

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

    /// <summary>1-1 텔레비전 방을 원래 좌표 그대로 가져오고, 1-1 카메라 위치를 화면 중심으로 쓴다</summary>
    private static Transform ImportRoom(Ctx ctx, Transform mapRoot, out Vector3 camCenter)
    {
        camCenter = new Vector3(0.5f, 0.25f, 0f);

        Scene src = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(ctx.Scene);

        var container = new GameObject("AbstractSpace");
        container.transform.SetParent(mapRoot, false);

        foreach (string path in new[] { "=====Map=====/BG_LostRoom_v1", "=====Map=====/ANI_television_v1_0" })
        {
            Transform t = FindByPath(src, path);
            if (t == null)
            {
                ctx.Report.Add($"⚠ {RoomScenePath} 에서 '{path}' 를 못 찾음");
                continue;
            }

            CopyInto(t, container.transform);
        }

        Transform cam = FindByPath(src, "Virtual Camera");
        if (cam != null)
            camCenter = new Vector3(cam.position.x, cam.position.y, 0f);

        EditorSceneManager.CloseScene(src, true);
        StripScripts(container.transform, ctx);

        // 해변이 위에 겹쳐 올라오도록 추상공간은 가장 아래 레이어에 둔다
        int order = 0;
        foreach (SpriteRenderer sr in container.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = order++;
        }

        ctx.Report.Add($"추상공간 ← Sequence1S#1 텔레비전 방 (화면 중심 {camCenter})");
        return container.transform;
    }

    /// <summary>
    /// 여름 바람 해변을 가져와 기획서 해변 구도(BeachViewCenter)가 추상공간 화면 중심에 오도록 옮긴다.
    /// 알파 0으로 두고 디졸브 때 올린다.
    /// </summary>
    private static Transform ImportBeach(Ctx ctx, Transform mapRoot, Vector3 camCenter)
    {
        Scene src = EditorSceneManager.OpenScene(BeachScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(ctx.Scene);

        var container = new GameObject("Beach");
        container.transform.SetParent(mapRoot, false);

        Transform map = FindByPath(src, "=====Map=====/Clear_Map");
        if (map == null)
        {
            ctx.Report.Add($"⚠ {BeachScenePath} 에서 해변 맵(Clear_Map)을 못 찾음");
            EditorSceneManager.CloseScene(src, true);
            return null;
        }

        CopyInto(map, container.transform);
        EditorSceneManager.CloseScene(src, true);
        StripScripts(container.transform, ctx);

        container.transform.position = camCenter - BeachViewCenter;

        foreach (SpriteRenderer sr in container.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = "BackGround";
            sr.sortingOrder = 0;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
        }

        ctx.Report.Add("해변 ← SummerWind Clear_Map (파도 애니메이션 버전이 오면 Beach 아래 맵만 교체)");
        return container.transform;
    }

    private static void CopyInto(Transform src, Transform container)
    {
        GameObject copy = Object.Instantiate(src.gameObject, src.position, src.rotation);
        copy.name = src.name;
        copy.transform.localScale = src.lossyScale;
        copy.transform.SetParent(container, true);
        copy.SetActive(true);
    }

    private static void StripScripts(Transform root, Ctx ctx)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Assembly.GetName().Name == "Assembly-CSharp")
                Object.DestroyImmediate(mb);
        }

        foreach (Collider2D c in root.GetComponentsInChildren<Collider2D>(true))
            Object.DestroyImmediate(c);

        foreach (Rigidbody2D rb in root.GetComponentsInChildren<Rigidbody2D>(true))
            Object.DestroyImmediate(rb);
    }

    // ================================================================ 인물

    /// <summary>자세 스프라이트의 몸 중심(사각형 중심)이 (centerX, centerY)에 오도록 놓는다</summary>
    private static void PlaceByBodyCenter(Ctx ctx, Transform who, Sprite pose, float centerX, float centerY, string label)
    {
        var sr = who.GetComponentInChildren<SpriteRenderer>(true);

        if (sr != null && pose != null)
            sr.sprite = pose; // 에디터에서도 같은 자세가 보이게

        Sprite s = sr != null ? sr.sprite : null;
        if (s == null)
        {
            who.position = new Vector3(centerX, centerY, who.position.z);
            ctx.Report.Add($"⚠ {label} 스프라이트가 없어 대략 배치");
            return;
        }

        float scaleX = sr.transform.lossyScale.x;
        float scaleY = sr.transform.lossyScale.y;
        float midX = (s.rect.width * 0.5f - s.pivot.x) / s.pixelsPerUnit * scaleX;
        float midY = (s.rect.height * 0.5f - s.pivot.y) / s.pixelsPerUnit * scaleY;

        who.position = new Vector3(centerX - midX, centerY - midY, who.position.z);
        ctx.Report.Add($"{label} 배치 {who.position} (자세 {s.name})");
    }

    private static Sprite LoadPose(string path, Ctx ctx)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            ctx.Report.Add($"⚠ 자세 스프라이트를 못 찾았다: {path}");
        return sprite;
    }

    // ================================================================ UI / 카메라

    private static void MakeIllustrationCanvas(Ctx ctx, out Image illust, out TMP_Text label)
    {
        Transform canvasRoot = EnsureRoot("=====Canvas=====", ctx).transform;

        var go = new GameObject("S10S2 Illustration Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.layer = 5;
        go.transform.SetParent(canvasRoot, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 맵 위, 대화창(0) 아래. 일러스트와 대사를 한 화면에 함께 보여준다.
        canvas.sortingOrder = -1;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1344f, 960f);

        var imgGo = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
        imgGo.layer = 5;
        imgGo.transform.SetParent(go.transform, false);
        Stretch(imgGo.GetComponent<RectTransform>());
        illust = imgGo.GetComponent<Image>();
        illust.preserveAspect = true;
        illust.raycastTarget = false;
        imgGo.SetActive(false);

        var labelGo = new GameObject("PlaceholderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.layer = 5;
        labelGo.transform.SetParent(go.transform, false);
        var rt = labelGo.GetComponent<RectTransform>();
        Stretch(rt);
        rt.offsetMin = new Vector2(80f, 300f); // 대화창과 겹치지 않게 위쪽에
        rt.offsetMax = new Vector2(-80f, -80f);
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (ctx.Font != null)
            tmp.font = ctx.Font;
        labelGo.SetActive(false);
        label = tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static CinemachineVirtualCamera MakeCamera(Transform parent, Vector3 center)
    {
        var go = new GameObject("S10S2 Virtual Camera");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, -10f);

        // 추상공간·해변·빈 해변 모두 같은 고정 구도
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
        SetSound(so, "_tvBgm", FindByNames(TvNames), TvPlaceholder, "텔레비전 BGM", ctx, force);
        SetSound(so, "_wavesBgm", FindByNames(WavesNames), WavesPlaceholder, "파도 소리 BGM", ctx, force);
        SetSound(so, "_projectorBgm", FindByNames(ProjectorNames), ProjectorPlaceholder, "영사기 BGM", ctx, force);
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

    private static AudioClip FindByNames(string[] names)
    {
        foreach (string n in names)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{n} t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == n)
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }

        return null;
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("KoPubWorld Batang Medium t:TMP_FontAsset"))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f != null)
                return f;
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
        var entry = new EditorBuildSettingsScene(TargetScenePath, true);

        if (after >= 0)
            scenes.Insert(after + 1, entry);
        else
            scenes.Add(entry);

        EditorBuildSettings.scenes = scenes.ToArray();
        ctx.Report.Add("빌드 설정에 Sequence10S#2 추가 (10-1 바로 뒤)");
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
