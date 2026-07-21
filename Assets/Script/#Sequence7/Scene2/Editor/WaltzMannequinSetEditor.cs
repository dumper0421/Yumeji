using UnityEditor;
using UnityEngine;

/// <summary>
/// 왈츠 마네킹 세트 인스펙터 — 기획자용 버튼 모음.
/// 동선 포인트를 클릭 한 번으로 만들고, Scene 뷰에서 드래그로 옮길 수 있게 한다.
/// </summary>
[CustomEditor(typeof(WaltzMannequinSet))]
public class WaltzMannequinSetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var set = (WaltzMannequinSet)target;

        EditorGUILayout.HelpBox(
            "동선 만드는 법\n" +
            "1) 아래 '동선 포인트 만들기' 버튼을 누르면 포인트 4개가 생깁니다.\n" +
            "2) Scene 뷰에서 노란 점을 드래그해 원하는 위치로 옮기세요.\n" +
            "3) 포인트를 클릭하고 '대기 시간'을 넣으면 그 자리에서 잠깐 멈춥니다.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("동선 포인트 만들기 (4개)", GUILayout.Height(28)))
            CreateWaypoints(set, 4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("포인트 3개"))
            CreateWaypoints(set, 3);
        if (GUILayout.Button("포인트 5개"))
            CreateWaypoints(set, 5);
        if (GUILayout.Button("포인트 6개"))
            CreateWaypoints(set, 6);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("이 세트 복제하기 (마네킹 추가)", GUILayout.Height(24)))
            DuplicateSet(set);

        EditorGUILayout.Space(8);
        DrawDefaultInspector();
    }

    /// <summary>세트 주변에 다이아몬드 모양으로 포인트를 만들고 자동 연결한다.</summary>
    private void CreateWaypoints(WaltzMannequinSet set, int count)
    {
        var pathRoot = new GameObject($"{set.name}_동선");
        Undo.RegisterCreatedObjectUndo(pathRoot, "동선 포인트 만들기");

        // 세트의 자식으로 두면 세트가 움직일 때 포인트도 따라가므로 반드시 바깥에 둔다
        pathRoot.transform.SetParent(set.transform.parent, false);
        pathRoot.transform.position = set.transform.position;

        var points = new Transform[count];
        float radius = 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            var point = new GameObject($"p{i + 1}");
            Undo.RegisterCreatedObjectUndo(point, "동선 포인트 만들기");

            point.transform.SetParent(pathRoot.transform, false);
            point.transform.localPosition = new Vector3(
                Mathf.Sin(angle) * radius,
                Mathf.Cos(angle) * radius,
                0f);

            point.AddComponent<WaltzPoint>();
            points[i] = point.transform;
        }

        // _waypoints 배열에 자동 연결
        var so = new SerializedObject(set);
        var prop = so.FindProperty("_waypoints");
        prop.arraySize = count;
        for (int i = 0; i < count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        so.ApplyModifiedProperties();

        Selection.activeGameObject = pathRoot;
        EditorGUIUtility.PingObject(pathRoot);
    }

    private void DuplicateSet(WaltzMannequinSet set)
    {
        var copy = Instantiate(set.gameObject, set.transform.parent);
        copy.name = set.name + " (복제)";
        copy.transform.position = set.transform.position + new Vector3(1.5f, -1.5f, 0f);

        Undo.RegisterCreatedObjectUndo(copy, "세트 복제");
        Selection.activeGameObject = copy;
        EditorGUIUtility.PingObject(copy);

        Debug.Log(
            $"[{copy.name}] 복제 완료. 동선은 원본과 같으니 " +
            "'동선 포인트 만들기'로 새 동선을 만들어 주세요.");
    }
}
