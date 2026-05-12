using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sequence6S1ProjectorInteraction : InspectableObject
{
    [Header("Player Lock")]
    [SerializeField] private PlayerMove_Test_Lerp playerMoveTestLerp;
    [SerializeField] private PlayerActionController playerAction;

    [Header("Required Item")]
    [SerializeField] private ItemData requiredFilmItem;

    [Header("Scene")]
    [SerializeField] private string cutsceneSceneName = "SummerWindCutscene";

    [Header("White Out")]
    [SerializeField] private CanvasGroup whiteFadeCanvasGroup;
    [SerializeField] private float whiteOutDuration = 1.5f;

    private bool isRunning = false;

    protected override void OnInspect()
    {
        if (isRunning) return;

        StartCoroutine(RunProjectorEvent());
    }

    private IEnumerator RunProjectorEvent()
    {
        isRunning = true;

        LockPlayerControls(true);

        // 1. ¾ÆÀÌÅÛ Á¦°Å
        if (requiredFilmItem != null)
        {
            requiredFilmItem.Use();
        }

        // 2. È­ÀÌÆ®¾Æ¿ô
        yield return StartCoroutine(WhiteOut());

        // 3. ÄÆ¾À ¾À ÀüÈ¯
        SceneManager.LoadScene(cutsceneSceneName);
    }

    private IEnumerator WhiteOut()
    {
        if (whiteFadeCanvasGroup == null)
            yield break;

        whiteFadeCanvasGroup.gameObject.SetActive(true);
        whiteFadeCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < whiteOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / whiteOutDuration;
            whiteFadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        whiteFadeCanvasGroup.alpha = 1f;
    }

    private void LockPlayerControls(bool locked)
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.canMove = !locked;

        if (playerAction != null)
            playerAction.IsLockedByEvent = locked;
    }
}