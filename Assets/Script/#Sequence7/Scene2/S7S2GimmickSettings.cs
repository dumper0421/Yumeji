using UnityEngine;

/// <summary>
/// [7-2] 기믹 수치 설정 파일.
///
/// 테스트 패널에서 조절한 값을 '저장' 버튼으로 여기에 기록하면
/// 게임을 껐다 켜도 그대로 남습니다. (씬이 아니라 에셋 파일에 저장되기 때문)
///
/// 파일 위치: Assets/Resources/S7S2GimmickSettings.asset
/// 마네킹들은 게임이 시작될 때 이 파일의 값을 자동으로 읽어갑니다.
/// </summary>
[CreateAssetMenu(
    fileName = "S7S2GimmickSettings",
    menuName = "Yumeji/[7-2] 기믹 수치 설정")]
public class S7S2GimmickSettings : ScriptableObject
{
    private const string ResourcePath = "S7S2GimmickSettings";

    [Header("기믹 1) 마네킹 왈츠")]
    [Tooltip("이동 속도. 클수록 빠름 (기획서 기준 2)")]
    [Range(0.2f, 8f)]
    public float waltzMoveSpeed = 2f;

    [Tooltip("촬영 후 완전히 멈춰 있는 시간 (기획서 기준 2초)")]
    [Range(0f, 8f)]
    public float waltzStillDuration = 2f;

    [Tooltip("다시 움직이기 전 흔들리는 시간 (기획서 기준 1초)")]
    [Range(0f, 4f)]
    public float waltzShakeDuration = 1f;

    [Header("기믹 2) 마네킹 미로")]
    [Tooltip("기믹 위치로 나가는 시간 (기획서 기준 0.25초)")]
    [Range(0.05f, 2f)]
    public float mazeMoveOutDuration = 0.25f;

    [Tooltip("나간 자리에서 멈춰 있는 시간 (기획서 기준 3초)")]
    [Range(0f, 10f)]
    public float mazeHoldDuration = 3f;

    [Tooltip("제자리로 돌아오는 시간 (기획서 기준 0.4초)")]
    [Range(0.05f, 2f)]
    public float mazeReturnDuration = 0.4f;

    private static S7S2GimmickSettings _instance;

    /// <summary>
    /// Resources 폴더에서 설정 파일을 찾는다. 없으면 null을 반환하고,
    /// 이 경우 각 마네킹은 인스펙터에 직접 입력된 값을 그대로 사용한다.
    /// </summary>
    public static S7S2GimmickSettings Get()
    {
        if (_instance == null)
            _instance = Resources.Load<S7S2GimmickSettings>(ResourcePath);

        return _instance;
    }

    /// <summary>씬을 새로 로드할 때 캐시를 비운다.</summary>
    public static void ClearCache()
    {
        _instance = null;
    }

    /// <summary>기획서 기준값으로 되돌린다.</summary>
    public void ResetToSpec()
    {
        waltzMoveSpeed = 2f;
        waltzStillDuration = 2f;
        waltzShakeDuration = 1f;
        mazeMoveOutDuration = 0.25f;
        mazeHoldDuration = 3f;
        mazeReturnDuration = 0.4f;
    }
}
