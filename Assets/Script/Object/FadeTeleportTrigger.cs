using System.Collections;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class FadeTeleportTrigger : MonoBehaviour
{
    [Header("Teleport Target")]
    [SerializeField] private Vector3 targetPoint;

    [Header("Fade UI (CanvasGroup)")]
    [SerializeField] private CanvasGroup fadeGroup;   // 검은 이미지가 붙은 CanvasGroup
    [SerializeField] private float fadeOutTime = 0.2f;
    [SerializeField] private float holdBlackTime = 2.0f;
    [SerializeField] private float fadeInTime = 0.3f;

    [Header("Camera (Optional)")]
    [SerializeField] private CinemachineVirtualCamera switchToCamera;
    [SerializeField] private CinemachineVirtualCameraBase followBase; // Brain이 아니라 Follow 가진 vcam base
    [SerializeField] private SceneController sceneController;         // 네 프로젝트에 이미 있는 카메라 전환 함수

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip enterSfx;

    private bool _running = false;

    private void Reset()
    {
        // Trigger 자동 세팅
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_running) return;
        if (!collision.CompareTag("Player")) return;

        StartCoroutine(CoTeleport(collision));
    }

    private IEnumerator CoTeleport(Collider2D playerCol)
    {
        _running = true;

        var mover = playerCol.GetComponent<PlayerMove_Test_Lerp>();

        // 1) 플레이어 입력 차단(스크립트 비활성화) - PlayerMove 수정 없이 가능한 가장 확실한 방법
        if (mover != null)
        {
            mover.canMove = false;               // 혹시 남아있을 이동 코루틴 대비
            mover.animator.SetBool("Walking", false);
            mover.animator.SetBool("Pushing", false);
            mover.enabled = false;
        }

        // 2) Fade Out
        if (fadeGroup != null)
            yield return FadeTo(1f, fadeOutTime);

        // 3) 암전 시작 SFX(선택)
        if (enterSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(enterSfx);

        // 4) 완전 암전 유지
        yield return new WaitForSeconds(holdBlackTime);

        // 5) 텔레포트
        if (mover != null)
        {
            // mover.enabled=false라도 Teleport는 호출 가능(외부에서 호출이므로)
            mover.Teleport(targetPoint);
        }
        else
        {
            playerCol.transform.position = targetPoint;
        }

        // 6) 카메라 전환(선택)
        if (sceneController != null && switchToCamera != null)
            sceneController.ChangeCinemachineCamera(switchToCamera);

        if (followBase != null)
        {
            followBase.Follow = playerCol.transform;
            followBase.OnTargetObjectWarped(playerCol.transform, Vector3.zero);
        }

        // 7) Fade In
        if (fadeGroup != null)
            yield return FadeTo(0f, fadeInTime);

        // 8) 입력 복구
        if (mover != null)
            mover.enabled = true;

        _running = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        fadeGroup.blocksRaycasts = true;

        float start = fadeGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // timescale 영향 안 받음
            float k = Mathf.Clamp01(t / duration);
            fadeGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f))
            fadeGroup.blocksRaycasts = false;
    }
}
