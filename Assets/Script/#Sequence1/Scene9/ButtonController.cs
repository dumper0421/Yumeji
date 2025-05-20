using UnityEngine;
[RequireComponent(typeof(Collider2D), typeof(AudioSource))]
public class ButtonController : MonoBehaviour
{
    [Header("Button Graphics")]
    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Activator Object")]
    [SerializeField] private GameObject activatorObject;

    [Header("Object to Remove")]
    [SerializeField] private GameObject objectToRemove;

    [Header("Sound Effect")]
    [SerializeField] private AudioClip pressSfx;
    private AudioSource audioSource;

    [Header("Scene9Controller 참조")]
    [SerializeField] private Sequence1Scene9Controller scene9Controller;

    private bool isPressed = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private bool IsActivator(Collider2D other)
    {
        if (activatorObject == null) return false;
        return other.gameObject == activatorObject
            || other.transform.IsChildOf(activatorObject.transform);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isPressed && IsActivator(other))
        {
            isPressed = true;

            // 1) 버튼 스프라이트를 눌린 상태로 교체
            if (buttonRenderer != null && pressedSprite != null)
                buttonRenderer.sprite = pressedSprite;

            // 2) 효과음 한 번 재생
            if (pressSfx != null)
                audioSource.PlayOneShot(pressSfx);

            // 3) 지정된 오브젝트 제거
            if (objectToRemove != null)
                Destroy(objectToRemove);

            // 4) Scene9Controller 에 BGM 교체 요청 (한 번만 호출)
            if (scene9Controller != null)
                scene9Controller.ChangeBGMToButtonClip();
        }
    }

    // 눌린 상태를 유지하기 위해 비워두거나 주석 처리
    void OnTriggerExit2D(Collider2D other)
    {
        // 아무 동작 안 함
    }
}
