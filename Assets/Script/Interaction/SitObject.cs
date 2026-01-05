using UnityEngine;

public class SitObject : MonoBehaviour
{
    [Tooltip("플레이어가 앉았을 때 위치 오프셋 (이 오브젝트 기준)")]
    public Vector2 sitOffset = Vector2.zero;

    [Tooltip("앉았을 때 바라보는 방향 (4방 중 하나)")]
    public Vector2 sitDirection = Vector2.down;
}
