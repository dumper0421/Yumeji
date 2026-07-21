using UnityEditor;
using UnityEngine;

/// <summary>
/// [7-2] 씬에 마네킹을 추가하는 메뉴 도구.
/// 유니티 상단 메뉴 → Tools → 유메지 [7-2] 에서 사용합니다.
/// 화면(Scene 뷰)에서 보고 있는 위치에 만들어지므로, 원하는 곳을 보면서 누르면 됩니다.
/// </summary>
public static class S7S2SceneTools
{
    private const string WaltzPrefabPath =
        "Assets/Prefab/Object/Sequence7/Scene2/WaltzMannequinSet .prefab";

    private const string MannequinPrefabPath =
        "Assets/Prefab/Object/Sequence7/Scene2/OBJ_mannequin_v1.prefab";

    private const string TriggerPrefabPath =
        "Assets/Prefab/Object/Sequence7/Scene2/Trigger.prefab";

    // ---------- 기믹 1) 왈츠 마네킹 ----------
    [MenuItem("Tools/유메지 [7-2]/왈츠 마네킹 추가 (동선 포함)", false, 1)]
    public static void AddWaltzSet()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaltzPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "프리팹을 찾지 못했습니다",
                $"아래 파일이 있는지 확인해주세요.\n\n{WaltzPrefabPath}",
                "확인");
            return;
        }

        Vector3 spawnPos = GetSceneViewCenter();

        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "왈츠 마네킹 추가");
        obj.transform.position = spawnPos;
        obj.name = MakeUniqueName("WaltzSet");

        // 동선 자동 생성
        var set = obj.GetComponent<WaltzMannequinSet>();
        if (set != null)
            CreateDefaultPath(set, spawnPos);

        Selection.activeGameObject = obj;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"[7-2] '{obj.name}' 추가됨. Scene 뷰에서 노란 점을 드래그해 동선을 조정하세요.");
    }

    /// <summary>세트 주변에 다이아몬드 모양 동선을 만들고 연결한다.</summary>
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

    // ---------- 기믹 2) 기본 마네킹 ----------
    [MenuItem("Tools/유메지 [7-2]/기본 마네킹 추가 (안 움직임)", false, 2)]
    public static void AddStaticMannequin()
    {
        var obj = InstantiateMannequin("Mannequin");
        if (obj == null) return;

        if (obj.GetComponent<MannequinObstacle>() == null)
            obj.AddComponent<MannequinObstacle>();

        EnsureTriggerCollider(obj);

        Selection.activeGameObject = obj;
        Debug.Log($"[7-2] '{obj.name}' 추가됨. 닿으면 게임오버되는 기본 마네킹입니다.");
    }

    // ---------- 기믹 2) 트리거로 움직이는 마네킹 ----------
    [MenuItem("Tools/유메지 [7-2]/움직이는 마네킹 추가 (트리거 + 목표 지점)", false, 3)]
    public static void AddTriggeredMannequin()
    {
        Vector3 center = GetSceneViewCenter();

        // 1) 마네킹
        var mannequin = InstantiateMannequin("Mannequin_기믹");
        if (mannequin == null) return;

        if (mannequin.GetComponent<MannequinObstacle>() == null)
            mannequin.AddComponent<MannequinObstacle>();
        EnsureTriggerCollider(mannequin);

        // 2) 목표 지점
        var target = new GameObject(mannequin.name + "_목표지점");
        Undo.RegisterCreatedObjectUndo(target, "움직이는 마네킹 추가");
        target.transform.position = center + new Vector3(2f, 0f, 0f);

        // 3) 트리거 타일
        GameObject trigger;
        var triggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TriggerPrefabPath);

        if (triggerPrefab != null)
        {
            trigger = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab);
            Undo.RegisterCreatedObjectUndo(trigger, "움직이는 마네킹 추가");
        }
        else
        {
            trigger = new GameObject("Trigger");
            Undo.RegisterCreatedObjectUndo(trigger, "움직이는 마네킹 추가");
            var box = trigger.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        trigger.name = MakeUniqueName("Trigger");
        trigger.transform.position = center + new Vector3(0f, -2f, 0f);
        EnsureTriggerCollider(trigger);

        var tm = trigger.GetComponent<TriggeredMannequin>();
        if (tm == null)
            tm = trigger.AddComponent<TriggeredMannequin>();

        // 마네킹 ↔ 목표 지점 자동 연결
        var so = new SerializedObject(tm);
        var moves = so.FindProperty("_moves");
        moves.arraySize = 1;

        var first = moves.GetArrayElementAtIndex(0);
        first.FindPropertyRelative("mannequin").objectReferenceValue = mannequin.transform;
        first.FindPropertyRelative("targetPoint").objectReferenceValue = target.transform;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = trigger;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log(
            $"[7-2] '{trigger.name}' 추가됨. " +
            "트리거 타일과 목표 지점을 원하는 위치로 옮기세요. " +
            "마네킹을 더 붙이려면 인스펙터의 Moves 배열 크기를 늘리면 됩니다.");
    }

    // ---------- 공통 ----------
    private static GameObject InstantiateMannequin(string baseName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MannequinPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "프리팹을 찾지 못했습니다",
                $"아래 파일이 있는지 확인해주세요.\n\n{MannequinPrefabPath}",
                "확인");
            return null;
        }

        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "마네킹 추가");
        obj.transform.position = GetSceneViewCenter();
        obj.name = MakeUniqueName(baseName);
        return obj;
    }

    private static void EnsureTriggerCollider(GameObject obj)
    {
        var col = obj.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = obj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = Vector2.one;
        }
    }

    /// <summary>Scene 뷰에서 지금 보고 있는 위치를 반환한다.</summary>
    private static Vector3 GetSceneViewCenter()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;

        Vector3 p = view.pivot;
        return new Vector3(Mathf.Round(p.x * 2f) / 2f, Mathf.Round(p.y * 2f) / 2f, 0f);
    }

    /// <summary>씬에 같은 이름이 있으면 뒤에 번호를 붙인다.</summary>
    private static string MakeUniqueName(string baseName)
    {
        int index = 1;
        while (GameObject.Find($"{baseName}_{index}") != null)
            index++;

        return $"{baseName}_{index}";
    }
}
