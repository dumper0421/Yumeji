using System.Collections;
using Cinemachine;
using UnityEngine;

public class SceneIntroVerticalPan : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CinemachineVirtualCamera TiltDownVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera TheaterEntranceVirtualCamera;
    [SerializeField] private PlayerMove_Test_Lerp playerMove;

    [Header("Manual Pan (현재 위치가 시작점)")]
    [SerializeField] private float endY = -10f;
    [SerializeField] private float panDuration = 2.5f;
    [SerializeField] private float holdAtBottom = 0.4f;
    [SerializeField] private AnimationCurve ease;

    private IEnumerator Start()
    {
        if (ease == null || ease.length == 0)
            ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 1️⃣ 플레이어 이동 잠금
        if (playerMove != null)
        {
            playerMove.enabled = false;
            if (playerMove.animator != null)
            {
                playerMove.animator.SetBool("Walking", false);
                playerMove.animator.SetBool("Pushing", false);
            }
        }

        // 2️⃣ 카메라 상태 정리
        if (TheaterEntranceVirtualCamera != null)
            TheaterEntranceVirtualCamera.gameObject.SetActive(false);

        if (TiltDownVirtualCamera != null)
            TiltDownVirtualCamera.gameObject.SetActive(true);

        // 3️⃣ 현재 Y를 시작값으로 사용
        float startY = TiltDownVirtualCamera.transform.position.y;

        // 4️⃣ 패닝
        yield return PanY(startY, endY, panDuration);

        if (holdAtBottom > 0f)
            yield return new WaitForSeconds(holdAtBottom);

        // 5️⃣ 카메라 전환
        if (TiltDownVirtualCamera != null)
            TiltDownVirtualCamera.gameObject.SetActive(false);

        if (TheaterEntranceVirtualCamera != null)
            TheaterEntranceVirtualCamera.gameObject.SetActive(true);

        // 6️⃣ 플레이어 입력 해제
        if (playerMove != null)
            playerMove.enabled = true;
    }

    private IEnumerator PanY(float fromY, float toY, float duration)
    {
        if (TiltDownVirtualCamera == null)
            yield break;

        if (duration <= 0f)
        {
            SetIntroCamY(toY);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = ease.Evaluate(k);

            float y = Mathf.Lerp(fromY, toY, k);
            SetIntroCamY(y);

            yield return null;
        }

        SetIntroCamY(toY);
    }

    private void SetIntroCamY(float y)
    {
        var p = TiltDownVirtualCamera.transform.position;
        TiltDownVirtualCamera.transform.position = new Vector3(p.x, y, p.z);
    }
}