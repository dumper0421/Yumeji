using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Sequence4Scene2CameraInteractObject : InspectableObject
{
    [Header("References")]
    public DialogueManager dialogueManager;
    public Sequence4Scene2SceneController sceneController;

    [Header("Player Lock")]
    public PlayerMove_Test_Lerp playerMoveTestLerp;
    public PlayerActionController PlayerActionController;

    [Header("Direction Check")]
    public Animator playerAnimator;
    [Tooltip("카메라 아래쪽에서 상호작용할 때 플레이어가 바라봐야 하는 DirY 값")]
    public float requiredDirY = 1f;

    [Header("Dialogue IDs")]
    public string notReadyDialogueId = "camera_notReady";
    public string readyDialogueId = "camera_ready";
    public string wrongDirectionDialogueId = "camera_wrongDirection";

    [Header("Cutscene")]
    public AudioSource audioSource;
    public AudioClip slateClip;
    public AudioClip cutClip;

    [Tooltip("암전용 이미지(Canvas 안의 검은 Image)")]
    public Image fadeImage;

    [Tooltip("암전 시간")]
    public float fadeDuration = 1.3f;

    [Tooltip("컷 소리 전 슬레이트 후 대기 시간")]
    public float delayBeforeCutSound = 0.6f;

    [Tooltip("다음 씬 이름")]
    public string nextSceneName;

    private bool waitingForReadyDialogueEnd = false;
    private bool isSequencePlaying = false;

    protected override void OnInspect()
    {
        if (isSequencePlaying) return;
        if (dialogueManager == null || sceneController == null) return;

        // 1. 퍼즐 미완료
        if (!sceneController.AreAllPuzzlesSolved)
        {
            dialogueManager.StartDialogue(notReadyDialogueId);
            return;
        }

        // 2. 방향 틀림
        if (!IsInteractingFromBelow())
        {
            dialogueManager.StartDialogue(wrongDirectionDialogueId);
            return;
        }

        // 3. 퍼즐 완료 + 올바른 방향
        waitingForReadyDialogueEnd = true;
        dialogueManager.OnDialogueComplete += OnDialogueCompleteOnce;
        dialogueManager.StartDialogue(readyDialogueId);
    }

    private bool IsInteractingFromBelow()
    {
        if (playerAnimator == null) return false;

        // 네 프로젝트에서 DirY로 방향 관리한다는 전제
        float dirY = playerAnimator.GetFloat("DirY");
        return Mathf.Approximately(dirY, requiredDirY);
    }

    private void OnDialogueCompleteOnce(string dialogueId)
    {
        if (!waitingForReadyDialogueEnd) return;
        if (dialogueId != readyDialogueId) return;

        waitingForReadyDialogueEnd = false;
        dialogueManager.OnDialogueComplete -= OnDialogueCompleteOnce;

        StartCoroutine(PlayCameraSequence());
    }

    private IEnumerator PlayCameraSequence()
    {
        isSequencePlaying = true;

        LockPlayerControls(true);

        if (audioSource != null && slateClip != null)
            audioSource.PlayOneShot(slateClip);

        yield return new WaitForSeconds(delayBeforeCutSound);

        if (audioSource != null && cutClip != null)
            audioSource.PlayOneShot(cutClip);

        yield return StartCoroutine(FadeOutCoroutine(fadeDuration));

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        if (fadeImage == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    private void LockPlayerControls(bool locked)
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.canMove = !locked;

        if (PlayerActionController != null)
            PlayerActionController.IsLockedByEvent = locked;
    }

    private void OnDestroy()
    {
        if (dialogueManager != null)
            dialogueManager.OnDialogueComplete -= OnDialogueCompleteOnce;
    }
}