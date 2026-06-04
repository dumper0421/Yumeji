using System.Collections;
using Cinemachine;
using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    [Header("Teleport")]
    public Vector3 TargetPoint;

    [Header("Camera")]
    public CinemachineVirtualCamera cinemachine;
    public CinemachineVirtualCameraBase cinemachineBase;
    public SceneController controller;

    [Header("Fade (Optional)")]
    [SerializeField]
    private bool useFade = true;

    [SerializeField]
    private CanvasGroup fadeGroup; // 공용 페이드 UI를 드래그해도 되고, 비워두면 자동 탐색하게 만들 수도 있었다.

    [SerializeField]
    private float fadeOutTime = 0.2f;

    [SerializeField]
    private float holdBlackTime = 0.5f;

    [SerializeField]
    private float fadeInTime = 0.3f;

    public bool Isrunning;
    public bool IsActive = true;

    private void Awake()
    {
        // 씬에 페이드 캔버스가 1개면 자동으로 잡히게(원하면 제거해도 됐다.)
        if (fadeGroup == null)
            fadeGroup = FindFirstObjectByType<CanvasGroup>();
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsActive) return;
        if (Isrunning) return;
        if (!collision.CompareTag("Player")) return;

        if (useFade && fadeGroup != null)
            StartCoroutine(CoFadeTeleport(collision));
        else
            DoTeleportImmediate(collision);
    }

    private IEnumerator CoFadeTeleport(Collider2D playerCol)
    {
        Isrunning = true;

        var mover = playerCol.GetComponent<PlayerMove_Test_Lerp>();
        if (mover != null)
        {
            // PlayerMove_Test_Lerp의 canMove는 private라 직접 접근 불가였다. :contentReference[oaicite:2]{index=2}
            // 가장 확실한 입력 차단은 컴포넌트 비활성화였다.
            mover.enabled = false;
            if (mover.animator != null)
            {
                mover.animator.SetBool("Walking", false);
                mover.animator.SetBool("Pushing", false);
            }
        }

        yield return FadeTo(1f, fadeOutTime);

        if (holdBlackTime > 0f)
            yield return new WaitForSecondsRealtime(holdBlackTime);

        DoTeleportImmediate(playerCol);

        yield return FadeTo(0f, fadeInTime);

        if (mover != null)
            mover.enabled = true;

        Isrunning = false;
    }

    private void DoTeleportImmediate(Collider2D playerCol)
    {
        Vector3 before = playerCol.transform.position;

        // 위치 이동(기존 로직 유지)
        playerCol.transform.position = TargetPoint;

        var mover = playerCol.GetComponent<PlayerMove_Test_Lerp>();
        if (mover != null)
            mover.Teleport(TargetPoint); // 내부에서 StopAllCoroutines + 애니 정리까지 해주고 있었다. :contentReference[oaicite:3]{index=3}

        if (controller != null && cinemachine != null)
            controller.ChangeCinemachineCamera(cinemachine);

        if (cinemachineBase != null)
        {
            cinemachineBase.Follow = playerCol.transform;

            // 워프 보정(텔레포트 후 카메라가 튀는 느낌 줄이기)
            Vector3 delta = TargetPoint - before;
            cinemachineBase.OnTargetObjectWarped(playerCol.transform, delta);
        }

        if (GameManager.Instance.HasCompanion() && mover.Companion != null)
        {
            mover.Companion.StopAllCoroutines();
            mover.Companion.ImmediatelySetPosition(TargetPoint - mover.vector, mover.vector);
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        fadeGroup.blocksRaycasts = true;

        float start = fadeGroup.alpha;
        float t = 0f;

        // duration 0 방어
        if (duration <= 0f)
        {
            fadeGroup.alpha = targetAlpha;
            fadeGroup.blocksRaycasts = !Mathf.Approximately(targetAlpha, 0f);
            yield break;
        }

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f))
            fadeGroup.blocksRaycasts = false;
    }
}
