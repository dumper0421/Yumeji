using UnityEngine;

/// <summary>
/// 왈츠 마네킹 동선의 포인트 하나.
/// 웨이포인트 오브젝트에 붙이면 그 지점에서 잠깐 멈추게 할 수 있다.
/// 붙이지 않으면 대기 없이 바로 다음 포인트로 이동한다.
/// </summary>
public class WaltzPoint : MonoBehaviour
{
    [Tooltip("이 포인트에 도착했을 때 멈춰 있는 시간(초). 0이면 멈추지 않고 바로 이동")]
    [Range(0f, 10f)]
    public float waitSeconds = 0f;

    [Tooltip("Scene 뷰에 표시할 색")]
    public Color gizmoColor = new Color(1f, 0.85f, 0.2f);

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.18f);

        if (waitSeconds > 0f)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.35f);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
