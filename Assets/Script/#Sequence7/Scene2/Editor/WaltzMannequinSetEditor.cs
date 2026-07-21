using UnityEditor;
using UnityEngine;

/// <summary>
/// 왈츠 마네킹 세트 인스펙터 — 기획자용 동선 편집 도구.
/// 포인트를 추가/삭제/순서변경하고, Scene 뷰에서 직접 드래그해 옮길 수 있다.
/// </summary>
[CustomEditor(typeof(WaltzMannequinSet))]
public class WaltzMannequinSetEditor : Editor
{
    private SerializedProperty _waypointsProp;

    private void OnEnable()
    {
        _waypointsProp = serializedObject.FindProperty("_waypoints");
    }

    public override void OnInspectorGUI()
    {
        var set = (WaltzMannequinSet)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "동선 만드는 법\n" +
            "1) 동선이 없으면 '동선 새로 만들기'를 누르세요.\n" +
            "2) Scene 뷰에서 노란 점을 드래그해 위치를 옮깁니다.\n" +
            "3) 길을 더 만들고 싶으면 '포인트 추가'를 누르세요.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        DrawWaypointList(set);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("동선 전체", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("동선 새로 만들기 (4개)", GUILayout.Height(26)))
            CreateWaypoints(set, 4);
        if (GUILayout.Button("3개", GUILayout.Height(26), GUILayout.Width(44)))
            CreateWaypoints(set, 3);
        if (GUILayout.Button("5개", GUILayout.Height(26), GUILayout.Width(44)))
            CreateWaypoints(set, 5);
        if (GUILayout.Button("6개", GUILayout.Height(26), GUILayout.Width(44)))
            CreateWaypoints(set, 6);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("이 마네킹 복제하기 (같은 동선 공유)", GUILayout.Height(24)))
            DuplicateSet(set);

        EditorGUILayout.Space(10);
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    // ---------- 포인트 목록 ----------
    private void DrawWaypointList(WaltzMannequinSet set)
    {
        EditorGUILayout.LabelField(
            $"동선 포인트 ({_waypointsProp.arraySize}개)", EditorStyles.boldLabel);

        if (_waypointsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "동선 포인트가 없어서 이 마네킹은 움직이지 않습니다.\n" +
                "아래 '동선 새로 만들기'를 눌러주세요.",
                MessageType.Warning);
        }

        for (int i = 0; i < _waypointsProp.arraySize; i++)
        {
            var element = _waypointsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"p{i + 1}", GUILayout.Width(28));
            EditorGUILayout.PropertyField(element, GUIContent.none);

            // 순서 바꾸기
            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("↑", GUILayout.Width(24)))
                {
                    _waypointsProp.MoveArrayElement(i, i - 1);
                    break;
                }
            }

            using (new EditorGUI.DisabledScope(i == _waypointsProp.arraySize - 1))
            {
                if (GUILayout.Button("↓", GUILayout.Width(24)))
                {
                    _waypointsProp.MoveArrayElement(i, i + 1);
                    break;
                }
            }

            if (GUILayout.Button("삭제", GUILayout.Width(44)))
            {
                DeletePoint(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("＋ 포인트 추가", GUILayout.Height(24)))
            AddPoint(set);
    }

    /// <summary>마지막 포인트 옆에 새 포인트를 하나 만든다.</summary>
    private void AddPoint(WaltzMannequinSet set)
    {
        Transform pathRoot = FindOrCreatePathRoot(set);

        // 마지막 포인트에서 조금 떨어진 곳에 배치
        Vector3 spawnPos = set.transform.position + new Vector3(1.5f, 0f, 0f);

        if (_waypointsProp.arraySize > 0)
        {
            var last = _waypointsProp
                .GetArrayElementAtIndex(_waypointsProp.arraySize - 1)
                .objectReferenceValue as Transform;

            if (last != null)
                spawnPos = last.position + new Vector3(1.5f, 0f, 0f);
        }

        var point = new GameObject($"p{_waypointsProp.arraySize + 1}");
        Undo.RegisterCreatedObjectUndo(point, "포인트 추가");
        point.transform.SetParent(pathRoot, true);
        point.transform.position = spawnPos;
        point.AddComponent<WaltzPoint>();

        _waypointsProp.arraySize++;
        _waypointsProp
            .GetArrayElementAtIndex(_waypointsProp.arraySize - 1)
            .objectReferenceValue = point.transform;

        serializedObject.ApplyModifiedProperties();
        Selection.activeGameObject = point;
    }

    private void DeletePoint(int index)
    {
        var point = _waypointsProp
            .GetArrayElementAtIndex(index)
            .objectReferenceValue as Transform;

        // 배열에서 먼저 제거 (첫 삭제는 참조만 비우므로 두 번 호출)
        _waypointsProp.DeleteArrayElementAtIndex(index);
        if (index < _waypointsProp.arraySize &&
            _waypointsProp.GetArrayElementAtIndex(index).objectReferenceValue == null)
        {
            _waypointsProp.DeleteArrayElementAtIndex(index);
        }

        serializedObject.ApplyModifiedProperties();

        if (point != null)
            Undo.DestroyObjectImmediate(point.gameObject);
    }

    private Transform FindOrCreatePathRoot(WaltzMannequinSet set)
    {
        // 기존 포인트가 있으면 그 부모를 쓴다
        for (int i = 0; i < _waypointsProp.arraySize; i++)
        {
            var existing = _waypointsProp
                .GetArrayElementAtIndex(i)
                .objectReferenceValue as Transform;

            if (existing != null && existing.parent != null)
                return existing.parent;
        }

        var pathRoot = new GameObject($"{set.name}_동선");
        Undo.RegisterCreatedObjectUndo(pathRoot, "동선 만들기");

        // 세트의 자식으로 두면 세트가 움직일 때 포인트도 따라가므로 반드시 바깥에 둔다
        pathRoot.transform.SetParent(set.transform.parent, false);
        pathRoot.transform.position = set.transform.position;
        return pathRoot.transform;
    }

    /// <summary>세트 주변에 원형으로 포인트를 새로 만들고 자동 연결한다.</summary>
    private void CreateWaypoints(WaltzMannequinSet set, int count)
    {
        var pathRoot = new GameObject($"{set.name}_동선");
        Undo.RegisterCreatedObjectUndo(pathRoot, "동선 새로 만들기");

        pathRoot.transform.SetParent(set.transform.parent, false);
        pathRoot.transform.position = set.transform.position;

        _waypointsProp.arraySize = count;
        float radius = 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;

            var point = new GameObject($"p{i + 1}");
            Undo.RegisterCreatedObjectUndo(point, "동선 새로 만들기");

            point.transform.SetParent(pathRoot.transform, false);
            point.transform.localPosition = new Vector3(
                Mathf.Sin(angle) * radius,
                Mathf.Cos(angle) * radius,
                0f);

            point.AddComponent<WaltzPoint>();
            _waypointsProp.GetArrayElementAtIndex(i).objectReferenceValue = point.transform;
        }

        serializedObject.ApplyModifiedProperties();
        EditorGUIUtility.PingObject(pathRoot);
    }

    private void DuplicateSet(WaltzMannequinSet set)
    {
        var copy = Instantiate(set.gameObject, set.transform.parent);
        copy.name = set.name + " (복제)";
        copy.transform.position = set.transform.position + new Vector3(1.5f, -1.5f, 0f);

        Undo.RegisterCreatedObjectUndo(copy, "마네킹 복제");
        Selection.activeGameObject = copy;
        EditorGUIUtility.PingObject(copy);
    }

    // ---------- Scene 뷰에서 점 드래그 ----------
    private void OnSceneGUI()
    {
        var set = (WaltzMannequinSet)target;
        serializedObject.Update();

        for (int i = 0; i < _waypointsProp.arraySize; i++)
        {
            var point = _waypointsProp
                .GetArrayElementAtIndex(i)
                .objectReferenceValue as Transform;

            if (point == null) continue;

            Handles.color = new Color(1f, 0.92f, 0.2f);
            Handles.Label(point.position + Vector3.up * 0.35f, $"p{i + 1}");

            EditorGUI.BeginChangeCheck();

            // 2D 게임이라 Z는 고정하고 XY만 움직이게 한다
            Vector3 moved = Handles.FreeMoveHandle(
                point.position,
                0.22f,
                Vector3.zero,
                Handles.DotHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point, "포인트 이동");
                point.position = new Vector3(moved.x, moved.y, point.position.z);
                EditorUtility.SetDirty(point);
            }
        }
    }
}
