using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PushableObject : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("이 레이어에 속한 오브젝트를 밀 수 없습니다.")]
    public LayerMask obstacleLayer;
    [Tooltip("1 타일 거리 밀 때 걸리는 시간(초)")]
    public float pushDuration = 0.2f;

    [Header("Sound Settings")]
    [Tooltip("밀릴 때 재생할 효과음")]
    [SerializeField] private AudioClip pushSFX;


    private bool isMoving = false;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;
    private AudioSource audioSource;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// 외부(플레이어)에서 호출.
    /// dir 방향으로 한 타일만큼 밀 수 있으면 이동을 시작하고 true 반환.
    /// </summary>
    public bool TryPush(Vector2 dir)
    {
        if (isMoving) return false;

        Vector2 start = transform.position;
        Vector2 dest = start + dir;

        // 장애물 체크: 다음 칸에 벽·다른 Pushable·NoPass 레이어가 있으면 밀지 않음
        RaycastHit2D hit = Physics2D.BoxCast(
            start, boxCollider.size, 0f, dir, 1f, obstacleLayer
        );
        if (hit.collider != null) return false;

        // **밀기 시작 시점에 효과음 재생**
        if (pushSFX != null)
            audioSource.PlayOneShot(pushSFX);

        StartCoroutine(PushCoroutine(start, dest));
        return true;
    }

    private IEnumerator PushCoroutine(Vector2 start, Vector2 dest)
    {
        isMoving = true;
        float elapsed = 0f;

        while (elapsed < pushDuration)
        {
            Vector2 newPos = Vector2.Lerp(start, dest, elapsed / pushDuration);
            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = dest;
        isMoving = false;
    }
}
