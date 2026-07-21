using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [7-2] 씬에 마네킹을 추가하는 메뉴 도구.
/// 유니티 상단 메뉴 → Tools → 유메지 [7-2] 에서 사용합니다.
///
/// 프리팹이 씬 오브젝트(대화 매니저, 씬 컨트롤러)를 참조할 수 없기 때문에,
/// 이 도구가 추가 직후 그 연결까지 자동으로 채워 넣습니다.
/// 결과는 Console 창에 무엇이 연결됐는지 함께 출력됩니다.
/// </summary>
public static class S7S2SceneTools
{
    private const string WaltzPrefabPath =
        "Assets/Prefab/Object/Sequence7/Scene2/WaltzMannequinSet .prefab";

    private const string MannequinPrefabPath =
        "Assets/Prefab/Object/Sequence7/Scene2/OBJ_mannequin_v1.prefab";

    private const string TriggerPrefabPath =
        "Assets/Prefab/Object/Sequence7/Scene2/Trigger.prefab";

    private const string MoveSfxPath = "Assets/Resources/SFX/7-2_SFX_mannequin_move.mp3";

    // ---------- 기믹 1) 왈츠 마네킹 ----------
    [MenuItem("Tools/유메지 [7-2]/왈츠 마네킹 추가 (동선 포함)", false, 1)]
    public static void AddWaltzSet()
    {
        var prefab = LoadPrefab(WaltzPrefabPath);
        if (prefab == null) return;

        Vector3 spawnPos = GetSceneViewCenter();

        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(root, "왈츠 마네킹 추가");
        root.transform.position = spawnPos;
        root.name = MakeUniqueName("WaltzMannequinSet");

        // 스크립트는 루트가 아니라 자식(WaltzSet)에 붙어 있다
        var set = root.GetComponentInChildren<WaltzMannequinSet>(true);
        if (set == null)
        {
            Debug.LogError(
                "[7-2] 프리팹에서 WaltzMannequinSet 스크립트를 찾지 못했습니다. " +
                "프리팹이 바뀌었는지 확인해주세요.");
            return;
        }

        var report = new List<string>();

        // 동선은 프리팹에 이미 들어 있다. 비어 있을 때만 새로 만든다.
        if (set.WaypointCount == 0)
        {
            CreateDefaultPath(set, spawnPos);
            report.Add("동선 4개 새로 생성");
        }
        else
        {
            report.Add($"동선 {set.WaypointCount}개 (프리팹 내장)");
        }

        // 씬 참조 자동 연결
        if (AssignDialogueManager(set, "_dialogueManager"))
            report.Add("대화 매니저 연결");

        var controller = Object.FindObjectOfType<Sequence7Scene2DialogueController>();
        if (controller != null)
        {
            if (AppendToArray(controller, "_waltzSets", set))
                report.Add("씬 컨트롤러의 왈츠 목록에 등록");

            if (AppendToArray(controller, "_mannequins", root))
                report.Add("전력 복구 시 사라지는 목록에 등록");
        }
        else
        {
            report.Add("⚠ 씬 컨트롤러를 못 찾아 목록 등록은 못 했습니다");
        }

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"[7-2] '{root.name}' 추가 — {string.Join(" / ", report)}");
    }

    // ---------- 기믹 2) 기본 마네킹 ----------
    [MenuItem("Tools/유메지 [7-2]/기본 마네킹 추가 (안 움직임)", false, 2)]
    public static void AddStaticMannequin()
    {
        var obj = InstantiateMannequin("Mannequin");
        if (obj == null) return;

        var report = new List<string>();

        var obstacle = obj.GetComponent<MannequinObstacle>();
        if (obstacle == null)
        {
            obstacle = Undo.AddComponent<MannequinObstacle>(obj);
            report.Add("게임오버 판정 추가");
        }

        EnsureTriggerCollider(obj);

        if (AssignDialogueManager(obstacle, "_dialogueManager"))
            report.Add("대화 매니저 연결");

        var controller = Object.FindObjectOfType<Sequence7Scene2DialogueController>();
        if (controller != null && AppendToArray(controller, "_mannequins", obj))
            report.Add("전력 복구 시 사라지는 목록에 등록");

        Selection.activeGameObject = obj;
        Debug.Log($"[7-2] '{obj.name}' 추가 — {string.Join(" / ", report)}");
    }

    // ---------- 기믹 2) 트리거로 움직이는 마네킹 ----------
    [MenuItem("Tools/유메지 [7-2]/움직이는 마네킹 추가 (트리거 + 목표 지점)", false, 3)]
    public static void AddTriggeredMannequin()
    {
        Vector3 center = GetSceneViewCenter();
        var report = new List<string>();

        // 지우기 쉽도록 세 오브젝트를 하나의 부모로 묶는다
        var group = new GameObject(MakeUniqueName("움직이는마네킹"));
        Undo.RegisterCreatedObjectUndo(group, "움직이는 마네킹 추가");
        group.transform.position = center;

        // 1) 마네킹
        var mannequin = InstantiateMannequin("Mannequin_기믹");
        if (mannequin == null) return;

        Undo.SetTransformParent(mannequin.transform, group.transform, "움직이는 마네킹 추가");

        var obstacle = mannequin.GetComponent<MannequinObstacle>();
        if (obstacle == null)
            obstacle = Undo.AddComponent<MannequinObstacle>(mannequin);

        EnsureTriggerCollider(mannequin);
        AssignDialogueManager(obstacle, "_dialogueManager");

        // 2) 목표 지점
        var target = new GameObject(mannequin.name + "_목표지점");
        Undo.RegisterCreatedObjectUndo(target, "움직이는 마네킹 추가");
        target.transform.SetParent(group.transform, true);
        target.transform.position = center + new Vector3(2f, 0f, 0f);

        // 3) 트리거 타일
        GameObject trigger;
        var triggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TriggerPrefabPath);

        if (triggerPrefab != null)
        {
            trigger = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab);
        }
        else
        {
            trigger = new GameObject("Trigger");
            var box = trigger.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        Undo.RegisterCreatedObjectUndo(trigger, "움직이는 마네킹 추가");
        trigger.name = MakeUniqueName("Trigger");
        trigger.transform.SetParent(group.transform, true);
        trigger.transform.position = center + new Vector3(0f, -2f, 0f);
        EnsureTriggerCollider(trigger);

        var tm = trigger.GetComponent<TriggeredMannequin>();
        if (tm == null)
            tm = Undo.AddComponent<TriggeredMannequin>(trigger);

        // 마네킹 ↔ 목표 지점 연결
        var so = new SerializedObject(tm);
        var moves = so.FindProperty("_moves");
        moves.arraySize = 1;

        var first = moves.GetArrayElementAtIndex(0);
        first.FindPropertyRelative("mannequin").objectReferenceValue = mannequin.transform;
        first.FindPropertyRelative("targetPoint").objectReferenceValue = target.transform;

        // 효과음이 비어 있으면 채워 넣는다
        var sfxProp = so.FindProperty("_moveSfx");
        if (sfxProp != null && sfxProp.objectReferenceValue == null)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MoveSfxPath);
            if (clip != null)
            {
                sfxProp.objectReferenceValue = clip;
                report.Add("이동 효과음 연결");
            }
        }

        so.ApplyModifiedProperties();
        report.Add("마네킹·목표지점·트리거 연결");

        var controller = Object.FindObjectOfType<Sequence7Scene2DialogueController>();
        if (controller != null && AppendToArray(controller, "_mannequins", mannequin))
            report.Add("전력 복구 시 사라지는 목록에 등록");

        Selection.activeGameObject = group;
        SceneView.lastActiveSceneView?.FrameSelected();

        report.Add("세 오브젝트를 한 그룹으로 묶음");
        Debug.Log($"[7-2] '{group.name}' 추가 — {string.Join(" / ", report)}");
    }

    // ---------- 삭제 후 정리 ----------
    [MenuItem("Tools/유메지 [7-2]/삭제한 마네킹 정리 (빈 칸 없애기)", false, 21)]
    public static void CleanupMissingReferences()
    {
        var controller = Object.FindObjectOfType<Sequence7Scene2DialogueController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "정리", "Sequence7Scene2DialogueController를 찾지 못했습니다.", "확인");
            return;
        }

        int removed = 0;
        removed += CompactArray(controller, "_waltzSets");
        removed += CompactArray(controller, "_mannequins");

        // 각 마네킹의 동선 목록에서도 빈 칸 제거
        foreach (var set in Object.FindObjectsOfType<WaltzMannequinSet>(true))
            removed += CompactArray(set, "_waypoints");

        string message = removed == 0
            ? "빈 칸이 없습니다. 정리할 것이 없습니다."
            : $"빈 칸 {removed}개를 정리했습니다.";

        EditorUtility.DisplayDialog("정리", message, "확인");
        Debug.Log($"[7-2] 빈 칸 {removed}개 정리됨");
    }

    /// <summary>배열에서 비어 있는(삭제된) 항목을 제거한다.</summary>
    private static int CompactArray(Object owner, string fieldName)
    {
        var so = new SerializedObject(owner);
        var array = so.FindProperty(fieldName);
        if (array == null || !array.isArray) return 0;

        int removed = 0;

        for (int i = array.arraySize - 1; i >= 0; i--)
        {
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                array.DeleteArrayElementAtIndex(i);
                removed++;
            }
        }

        if (removed > 0)
        {
            Undo.RecordObject(owner, "빈 칸 정리");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);
        }

        return removed;
    }

    // ---------- 점검 도구 ----------
    [MenuItem("Tools/유메지 [7-2]/씬 점검 (빠진 연결 찾기)", false, 20)]
    public static void ValidateScene()
    {
        var problems = new List<string>();

        var controller = Object.FindObjectOfType<Sequence7Scene2DialogueController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "씬 점검",
                "Sequence7Scene2DialogueController를 찾지 못했습니다.\n" +
                "이 씬이 7-2가 맞는지 확인해주세요.",
                "확인");
            return;
        }

        var so = new SerializedObject(controller);
        var waltzList = so.FindProperty("_waltzSets");

        var registered = new HashSet<Object>();
        for (int i = 0; i < waltzList.arraySize; i++)
            registered.Add(waltzList.GetArrayElementAtIndex(i).objectReferenceValue);

        foreach (var set in Object.FindObjectsOfType<WaltzMannequinSet>(true))
        {
            if (set.WaypointCount == 0)
                problems.Add($"· {set.name} — 동선 포인트가 없습니다");

            if (!registered.Contains(set))
                problems.Add($"· {set.name} — 씬 컨트롤러의 왈츠 목록에 없습니다 (대사 후 안 움직임)");

            if (set.gameObject.layer != LayerMask.NameToLayer("Flashable"))
                problems.Add($"· {set.name} — 레이어가 Flashable이 아닙니다 (촬영해도 안 멈춤)");

            if (set.GetComponent<Collider2D>() == null)
                problems.Add($"· {set.name} — 콜라이더가 없습니다 (충돌 판정 안 됨)");
        }

        foreach (var t in Object.FindObjectsOfType<TriggeredMannequin>(true))
        {
            if (t.MannequinCount == 0)
                problems.Add($"· {t.name} — 움직일 마네킹이 지정되지 않았습니다");
        }

        string message = problems.Count == 0
            ? "문제를 찾지 못했습니다. 모든 연결이 정상입니다."
            : $"{problems.Count}개 항목을 확인해주세요.\n\n" + string.Join("\n", problems);

        EditorUtility.DisplayDialog("씬 점검", message, "확인");

        if (problems.Count > 0)
            Debug.LogWarning("[7-2 씬 점검]\n" + string.Join("\n", problems));
        else
            Debug.Log("[7-2 씬 점검] 이상 없음");
    }

    // ---------- 공통 ----------
    /// <summary>씬의 DialogueManager를 찾아 지정된 필드에 넣는다.</summary>
    private static bool AssignDialogueManager(Object target, string fieldName)
    {
        var manager = Object.FindObjectOfType<DialogueManager>();
        if (manager == null) return false;

        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue != null) return false;

        prop.objectReferenceValue = manager;
        so.ApplyModifiedProperties();
        return true;
    }

    /// <summary>컨트롤러의 배열 끝에 오브젝트를 추가한다. 이미 있으면 아무것도 하지 않는다.</summary>
    private static bool AppendToArray(Object owner, string fieldName, Object value)
    {
        var so = new SerializedObject(owner);
        var array = so.FindProperty(fieldName);
        if (array == null || !array.isArray) return false;

        for (int i = 0; i < array.arraySize; i++)
        {
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                return false;
        }

        Undo.RecordObject(owner, "목록에 추가");
        array.arraySize++;
        array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
        return true;
    }

    private static void CreateDefaultPath(WaltzMannequinSet set, Vector3 center)
    {
        var pathRoot = new GameObject($"{set.name}_동선");
        Undo.RegisterCreatedObjectUndo(pathRoot, "동선 만들기");
        pathRoot.transform.SetParent(set.transform.parent, false);
        pathRoot.transform.position = center;

        const int count = 4;
        var points = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;

            var point = new GameObject($"p{i + 1}");
            Undo.RegisterCreatedObjectUndo(point, "동선 만들기");
            point.transform.SetParent(pathRoot.transform, false);
            point.transform.localPosition =
                new Vector3(Mathf.Sin(angle) * 2f, Mathf.Cos(angle) * 2f, 0f);

            point.AddComponent<WaltzPoint>();
            points[i] = point.transform;
        }

        var so = new SerializedObject(set);
        var prop = so.FindProperty("_waypoints");
        prop.arraySize = count;
        for (int i = 0; i < count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        so.ApplyModifiedProperties();
    }

    private static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "프리팹을 찾지 못했습니다",
                $"아래 파일이 있는지 확인해주세요.\n\n{path}",
                "확인");
        }
        return prefab;
    }

    private static GameObject InstantiateMannequin(string baseName)
    {
        var prefab = LoadPrefab(MannequinPrefabPath);
        if (prefab == null) return null;

        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "마네킹 추가");
        obj.transform.position = GetSceneViewCenter();
        obj.name = MakeUniqueName(baseName);
        return obj;
    }

    private static void EnsureTriggerCollider(GameObject obj)
    {
        if (obj.GetComponent<Collider2D>() != null) return;

        var box = Undo.AddComponent<BoxCollider2D>(obj);
        box.isTrigger = true;
        box.size = Vector2.one;
    }

    /// <summary>Scene 뷰에서 지금 보고 있는 위치를 반환한다.</summary>
    private static Vector3 GetSceneViewCenter()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;

        Vector3 p = view.pivot;
        return new Vector3(Mathf.Round(p.x * 2f) / 2f, Mathf.Round(p.y * 2f) / 2f, 0f);
    }

    private static string MakeUniqueName(string baseName)
    {
        int index = 1;
        while (GameObject.Find($"{baseName}_{index}") != null)
            index++;

        return $"{baseName}_{index}";
    }
}
