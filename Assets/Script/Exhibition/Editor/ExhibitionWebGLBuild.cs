using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 전시용 웹빌드(체험판, 시퀀스 7까지).
/// Tools → 유메지 [전시] 메뉴, 또는 배치모드:
///   Unity.exe -batchmode -buildTarget WebGL -projectPath . -executeMethod ExhibitionWebGLBuild.BuildFromCommandLine
///
/// - 빌드 목록은 아래 Scenes 배열만 쓴다 (EditorBuildSettings는 건드리지 않는다)
/// - YUMEJI_EXHIBITION 심볼은 이 빌드에만 붙는다 (ExhibitionBuild.Redirect가 시퀀스 8 이후를 안내 화면으로 바꾼다)
/// - 영상은 웹에서 VideoClip을 못 쓰므로 mp4 사본(.bytes)을 데이터에 넣어 Blob URL로 재생한다 (WebGLVideoUrl)
/// </summary>
public static class ExhibitionWebGLBuild
{
    private const string Tag = "[전시 웹빌드]";
    private const string SceneDir = "Assets/Scenes/2026PlayX4/";
    private const string DemoEndScenePath = SceneDir + ExhibitionBuild.EndSceneName + ".unity";
    private const string FontPath = "Assets/Font/TextMeshPro/KoPubWorld Batang Medium.asset";
    private const string OutDir = "../YumejiBuilds/WebGL_Exhibition"; // 저장소 밖 (C:/YumejiBuilds)

    /// <summary>체험판에 들어가는 씬. 씬 전환은 모두 이름으로 하므로 순서는 타이틀이 0번이기만 하면 된다.</summary>
    private static readonly string[] Scenes =
    {
        "TitleScene",
        "OpeningScene",
        "OpeningScene2",
        "Sequence1S#1",
        "Sequence2S#1",
        "Sequence2S#2",
        "Sequence2S#2_2",
        "Sequence2S#3_3",
        "Sequence2S#3_3CutScene",
        "Sequence2S#3_4",
        "Sequence2S#3_5",
        "Sequence2S#3_6",
        "Sequence2S#3_7",
        "Sequence2S#3_8",
        "Sequence2S#3_9",
        "Sequence2S#4",
        "Sequence3S#1",
        "Sequence3S#2",
        "Sequence3S#3",
        "Sequence3S#3Puzzle",
        "Sequence3S#4",
        "Sequence4S#1",
        "Sequence4S#2",
        "Sequence4S#2_2",
        "Sequence4S#2Puzzle",
        "Sequence4S#2PuzzleSolved",
        "Sequence5S#1",
        "Sequence5S#2",
        "Sequence5S#3",
        "Sequence6S#1",
        "SummerWind",
        "Sequence6S#2",
        "Sequence7S#1",
        "Sequence7S#2",
        "Sequence7S#3",
        "Sequence7S#4",
        ExhibitionBuild.EndSceneName,
    };

    /// <summary>
    /// VideoClip 파일 이름 → 웹에서 재생할 mp4 사본(.bytes, 게임 데이터에 들어간다).
    /// Unity Play는 StreamingAssets를 올려주지 않아서 별도 파일 대신 데이터 안에 넣는다.
    /// 3-4 엔딩 영상(유메지_SEQ3_ED)은 Sequence3Scene4Controller에서 재생이 주석 처리되어 있어 넣지 않는다.
    /// </summary>
    private static readonly Dictionary<string, string> WebVideos = new Dictionary<string, string>
    {
        { "플포오프닝", "Assets/Exhibition/WebVideo/yumeji_opening.bytes" },
    };

    // ================================================================ 메뉴

    [MenuItem("Tools/유메지 [전시]/체험판 준비 (안내 씬 + 영상 웹 대응)", false, 1)]
    public static void PrepareMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Prepare();
    }

    [MenuItem("Tools/유메지 [전시]/WebGL 체험판 빌드", false, 2)]
    public static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Prepare();
        Build();
    }

    public static void BuildFromCommandLine()
    {
        Prepare();
        bool ok = Build();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static void PrepareFromCommandLine()
    {
        Prepare();
        EditorApplication.Exit(0);
    }

    // ================================================================ 준비

    private static void Prepare()
    {
        if (!File.Exists(DemoEndScenePath))
            CreateDemoEndScene();

        AttachVideoUrls(embed: false);
        WireSceneFlow();
        AddToEditorBuildSettings();
    }

    // ================================================================ 씬 연결 (기획서 기준, 정식 게임에도 그대로 남는다)
    //   6-1 영사실 → 여름바람 컷씬 → 6-2 프레임 아웃 → 7-1 → 7-2 → 7-3 → 7-4
    //   (6-1은 2026 PlayX4 때 임시로 엔딩 영상에 연결되어 있었다)

    [MenuItem("Tools/유메지 [전시]/씬 연결만 맞추기 (6-1 → 여름바람 → 6-2, 7-2 → 7-3 → 7-4)", false, 3)]
    public static void WireSceneFlowMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        WireSceneFlow();
    }

    private static void WireSceneFlow()
    {
        // 6-1: 영사기에 필름을 쓰면 여름바람 컷씬
        EditScene("Sequence6S#1", () =>
        {
            foreach (var p in Object.FindObjectsOfType<Sequence6S1ProjectorInteraction>(true))
                SetString(p, "cutsceneSceneName", "SummerWind");
        });

        // 여름바람 컷씬이 끝나면 6-2
        EditScene("SummerWind", () =>
        {
            foreach (var c in Object.FindObjectsOfType<SummerBreezeCutsceneController>(true))
                SetString(c, "_nextSceneName", "Sequence6S#2");
        });

        // 7-2: 로프 너머 계단 꼭대기 트리거 → 2층. 하루에 Rigidbody2D가 없으니 트리거 쪽에 키네마틱을 붙인다
        EditScene("Sequence7S#2", () =>
        {
            foreach (var t in Object.FindObjectsOfType<SceneChangeTrigger>(true))
            {
                SetString(t, "sceneName_", "Sequence7S#3");

                var rb = t.GetComponent<Rigidbody2D>();
                if (rb == null)
                    rb = t.gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        });

        // 7-3: 비상문을 연 대사가 끝나면 3층
        EditScene("Sequence7S#3", () =>
        {
            var c = Object.FindObjectOfType<Sequence7Scene3DialogueController>(true);
            if (c == null)
            {
                Debug.LogError($"{Tag} 7-3 컨트롤러가 없다");
                return;
            }

            var field = typeof(Sequence7Scene3DialogueController).GetField(
                "onEmergencyDoorOpened",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
            );
            var evt = field?.GetValue(c) as UnityEvent;
            if (evt == null)
            {
                Debug.LogError($"{Tag} 7-3 onEmergencyDoorOpened 이벤트를 찾지 못했다");
                return;
            }

            if (evt.GetPersistentEventCount() > 0)
                return; // 이미 누가 연결해 두었으면 건드리지 않는다

            var changer = Object.FindObjectOfType<SceneChanger>(true);
            if (changer == null)
                changer = c.gameObject.AddComponent<SceneChanger>();

            UnityEventTools.AddStringPersistentListener(evt, changer.ChangeScene, "Sequence7S#4");
            EditorUtility.SetDirty(c);
        });
    }

    private static void EditScene(string name, System.Action edit)
    {
        string path = SceneDir + name + ".unity";
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        edit();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{Tag} 씬 연결 확인: {name}");
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"{Tag} {target.name}에 {field} 필드가 없다");
            return;
        }

        if (p.stringValue != value)
            Debug.Log($"{Tag} {target.name}.{field}: '{p.stringValue}' → '{value}'");

        p.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>에디터에서 타이틀부터 플레이해도 같은 흐름이 되도록 빠진 씬을 빌드 목록 끝에 추가한다 (기존 순서는 그대로)</summary>
    private static void AddToEditorBuildSettings()
    {
        var list = EditorBuildSettings.scenes.ToList();
        foreach (string name in new[] { "SummerWind", "Sequence6S#2", "Sequence7S#3", ExhibitionBuild.EndSceneName })
        {
            string path = SceneDir + name + ".unity";
            if (list.All(s => s.path != path) && File.Exists(path))
                list.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = list.ToArray();
    }

    /// <summary>
    /// VideoClip을 쓰는 VideoPlayer에 WebGLVideoUrl을 붙인다.
    /// embed=true면 영상 바이트를 연결하고(웹빌드 직전), false면 비운다(평소. PC 빌드에 영상이 두 번 들어가지 않게).
    /// 웹 사본이 없는 영상의 WebGLVideoUrl은 떼어낸다.
    /// </summary>
    private static void AttachVideoUrls(bool embed)
    {
        foreach (string name in Scenes.Where(n => n != ExhibitionBuild.EndSceneName))
        {
            string path = SceneDir + name + ".unity";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"{Tag} 씬 없음: {path}");
                continue;
            }

            string text = File.ReadAllText(path);
            if (!text.Contains("VideoPlayer:"))
                continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;

            foreach (VideoPlayer vp in Object.FindObjectsOfType<VideoPlayer>(true))
            {
                if (vp.clip == null)
                    continue;

                string clipName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(vp.clip));
                var url = vp.GetComponent<WebGLVideoUrl>();

                if (!WebVideos.TryGetValue(clipName, out string bytesPath))
                {
                    if (url != null)
                    {
                        Object.DestroyImmediate(url, true);
                        dirty = true;
                        Debug.Log($"{Tag} {name}/{vp.name}: '{clipName}'은 웹 사본이 없어 WebGLVideoUrl을 뗐다");
                    }
                    continue;
                }

                TextAsset bytes = null;
                if (embed)
                {
                    bytes = AssetDatabase.LoadAssetAtPath<TextAsset>(bytesPath);
                    if (bytes == null)
                        Debug.LogError($"{Tag} {name}/{vp.name}: {bytesPath} 가 없다. 웹에서 영상 없이 넘어간다.");
                }

                if (url == null)
                    url = vp.gameObject.AddComponent<WebGLVideoUrl>();

                var so = new SerializedObject(url);
                so.FindProperty("_videoBytes").objectReferenceValue = bytes;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
                Debug.Log($"{Tag} {name}/{vp.name}: 웹 영상 {(bytes != null ? "연결 " + bytesPath : "비움")}");
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    /// <summary>체험판 끝 안내 화면</summary>
    private static void CreateDemoEndScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1344f, 960f);
        scaler.matchWidthOrHeight = 0.5f;
        var group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        AddText(canvasGo.transform, "Title", "체험판은 여기까지입니다", 52f, new Vector2(0f, 70f), new Color(0.95f, 0.93f, 0.9f), font);
        AddText(canvasGo.transform, "Body", "하루의 이야기는 정식 출시 후에 이어집니다.\n출시 후 끝까지 함께해 주세요.", 30f, new Vector2(0f, -40f), new Color(0.8f, 0.78f, 0.75f), font);
        AddText(canvasGo.transform, "Thanks", "플레이해 주셔서 감사합니다", 24f, new Vector2(0f, -190f), new Color(0.6f, 0.58f, 0.56f), font);

        var screen = new GameObject("DemoEndScreen").AddComponent<DemoEndScreen>();
        var so = new SerializedObject(screen);
        so.FindProperty("_group").objectReferenceValue = group;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, DemoEndScenePath);
        Debug.Log($"{Tag} 안내 씬 생성: {DemoEndScenePath}");
    }

    private static void AddText(Transform parent, string name, string text, float size, Vector2 pos, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1200f, 200f);
        rt.anchoredPosition = pos;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.lineSpacing = 20f;
        if (font != null)
            tmp.font = font;
    }

    // ================================================================ 빌드

    private static bool Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Debug.LogError($"{Tag} WebGL Build Support 모듈이 설치되어 있지 않다. Unity Hub에서 모듈을 추가할 것.");
            return false;
        }

        string[] paths = Scenes.Select(n => SceneDir + n + ".unity").ToArray();
        string[] missing = paths.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length > 0)
        {
            Debug.LogError($"{Tag} 없는 씬:\n - " + string.Join("\n - ", missing));
            return false;
        }

        // 어떤 웹서버에서도 열리도록: Gzip + 압축 해제 폴백 (서버 헤더 설정 없이도 동작)
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;

        var options = new BuildPlayerOptions
        {
            scenes = paths,
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
            extraScriptingDefines = new[] { "YUMEJI_EXHIBITION" },
        };

        BuildReport report;
        AttachVideoUrls(embed: true);
        try
        {
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            AttachVideoUrls(embed: false);
        }

        BuildSummary s = report.summary;
        Debug.Log($"{Tag} 결과 {s.result}, 크기 {s.totalSize / (1024f * 1024f):0.0} MB, 시간 {s.totalTime}, 경고 {s.totalWarnings}, 에러 {s.totalErrors} → {Path.GetFullPath(OutDir)}");

        return s.result == BuildResult.Succeeded;
    }
}
