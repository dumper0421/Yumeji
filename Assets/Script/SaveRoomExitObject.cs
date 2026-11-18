
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveRoomExitObject : DialogueObject
{
    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName_;

    private bool isTransitioning_ = false;

    public void MoveToNextScene()
    {
        if (isTransitioning_)
            return;

        if (string.IsNullOrEmpty(nextSceneName_))
        {
            Debug.LogWarning("다음 씬이 설정되지 않았습니다.");
            return;
        }

        isTransitioning_ = true;

        SoundManager.Instance.StopAllSFX();
        SoundManager.Instance.StopBGM();

        // 기존 SceneChangeTrigger의 특수 씬 처리 유지
        if (nextSceneName_ != "Scenes/Final/Sequence1#7")
            SceneManager.LoadScene(nextSceneName_);
        else
            SceneManager.LoadScene(7);
    }
}
