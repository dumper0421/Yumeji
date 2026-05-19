using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraIntroThenFollow : MonoBehaviour
{
    [Header("참조")]
    public CinemachineVirtualCamera vcam;   // Virtual Camera
    public Transform staticTarget;          // 초반에 바라볼 빈 오브젝트
    public Transform playerTarget;          // 최종 Follow할 PlayerHaru

    [Header("설정")]
    public float introDuration = 3f;        // 초반 고정 시간(초)

    void Start()
    {
        // 1) 처음엔 staticTarget 바라보기
        vcam.Follow = staticTarget;

        // 2) 지정된 시간 후에 PlayerHaru 바라보도록 변경
        StartCoroutine(SwitchToPlayerAfterDelay());
    }

    IEnumerator SwitchToPlayerAfterDelay()
    {
        yield return new WaitForSeconds(introDuration);

        // 카메라 Follow 타깃을 플레이어로 전환
        vcam.Follow = playerTarget;
    }
}