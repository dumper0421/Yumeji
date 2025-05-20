using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 이 컴포넌트가 붙은 오브젝트와 F 키로 상호작용하면 sceneName 씬으로 전환합니다.
/// </summary>
public class SceneChanger_Interaction : MonoBehaviour
{
    [Tooltip("로드할 씬 이름을 입력하세요 (Build Settings에 등록된 이름)")]
    [SerializeField] private string sceneName;

    /// <summary>
    /// 상호작용 시 호출될 메서드
    /// </summary>
    public void ChangeScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneChanger: sceneName이 비어있습니다.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}