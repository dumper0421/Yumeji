using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sequence1Scene1Controller : SceneController
{
    [Header("페이드 인/아웃 시간")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("페이드 사이 화면을 보여주는 시간")]
    [SerializeField] private float displayDuration = 6f;

    [Header("페이드 아웃 후 이동할 씬 (없으면 비워둠)")]
    [SerializeField] private string nextSceneName;

    protected override void Start()
    {
        base.Start();

        playerMoveTestLerp.enabled = false;

        // 하루의 뒷모습(TV를 바라보는 방향)으로 고정
        playerAnimator.SetFloat("DirX", 0f);
        playerAnimator.SetFloat("DirY", 1f);
        playerAnimator.Update(0f);
        playerAnimator.enabled = false;

        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // CutsceneManager.Start()가 화면을 암전 상태로 만든 뒤 페이드인을 시작하기 위해 한 프레임 대기
        yield return null;

        CutsceneManager.Instance.FadeFromBlack(null, fadeDuration);

        yield return new WaitForSeconds(fadeDuration + displayDuration);

        CutsceneManager.Instance.FadeToBlack(() =>
        {
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }, fadeDuration);
    }

    protected override void OnStopIntervalReached() { }
}
