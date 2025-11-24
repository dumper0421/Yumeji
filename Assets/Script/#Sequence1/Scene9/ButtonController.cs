using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(AudioSource))]
public class ButtonController : MonoBehaviour
{
    [Header("Button Graphics")]
    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Activator Objects (둘 이상 가능)")]
    [SerializeField] private GameObject[] activatorObjects;

    [Header("Object to Toggle (버튼 상태에 따라 꺼졌다 켜질 오브젝트)")]
    [SerializeField] private GameObject objectToRemove;

    [Header("Sound Effect")]
    [SerializeField] private AudioClip pressSfx;
    private AudioSource audioSource;

    [Header("Script to Disable on Activator (옵션)")]
    [SerializeField] private PushableObject pushableObject;

    [Header("Scene9Controller 참조")]
    [SerializeField] private Sequence1Scene9Controller scene9Controller;

    private bool isPressed = false;

    // 버튼 위에 올라와 있는 activator 개수
    private int activatorCount = 0;

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

        // 굳이 필요하면 첫 번째 activator에서 PushableObject 가져오는 식으로 쓸 수 있다 했다.
        if ((activatorObjects != null && activatorObjects.Length > 0) && pushableObject == null)
        {
            if (activatorObjects[0] != null)
                pushableObject = activatorObjects[0].GetComponent<PushableObject>();
        }
    }

    // 이 콜라이더가 activatorObjects 중 하나인지 검사했다.
    private bool IsActivator(Collider2D other)
    {
        if (activatorObjects == null || activatorObjects.Length == 0) return false;

        foreach (var obj in activatorObjects)
        {
            if (obj == null) continue;

            if (other.gameObject == obj || other.transform.IsChildOf(obj.transform))
                return true;
        }
        return false;
    }

    // ▶ 어떤 activator든 버튼 위에 "처음" 올라오는 순간
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActivator(other)) return;

        activatorCount++;

        // 0 → 1이 되는 순간에만 실제로 버튼을 누른다 했다.
        if (activatorCount == 1)
        {
            SetPressed(true);
        }
    }

    // ▶ 버튼 위에 있던 activator가 하나 빠져나갈 때
    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsActivator(other)) return;

        activatorCount--;
        if (activatorCount < 0) activatorCount = 0; // 안전빵 클램프했다.

        // 마지막 activator까지 내려간 순간 (1 → 0)이면 버튼 해제한다 했다.
        if (activatorCount == 0)
        {
            SetPressed(false);
        }
    }

    /// <summary>
    /// 버튼이 눌렸는지/안 눌렸는지에 따라 그래픽 + 오브젝트 토글했다.
    /// </summary>
    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed) return;

        isPressed = pressed;

        // 1) 스프라이트 변경
        if (buttonRenderer != null)
        {
            buttonRenderer.sprite = pressed ? pressedSprite : unpressedSprite;
        }

        // 2) 오브젝트 ON/OFF (눌리면 숨김, 떨어지면 다시 표시)
        if (objectToRemove != null)
        {
            objectToRemove.SetActive(!pressed);
        }

        // 3) 효과음 (눌릴 때만 재생)
        if (pressed && pressSfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(pressSfx);
        }

        // 4) BGM 교체는 "처음 눌릴 때만" 하고 싶으면 pressed == true일 때만 호출했다.
        if (pressed && scene9Controller != null)
        {
            scene9Controller.ChangeBGMToButtonClip();
        }

        // 필요하면 여기에서 pushableObject.enabled = !pressed; 같은 것도 넣을 수 있다 했다.
    }
}
