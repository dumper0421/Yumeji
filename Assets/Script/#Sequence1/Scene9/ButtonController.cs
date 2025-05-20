using UnityEngine;

[RequireComponent(typeof(Collider2D))]
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

    private bool isPressed = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private bool IsActivator(Collider2D other)
    {
        if (activatorObject == null)
            return false;
        // 트리거에 닿은 콜라이더가 activatorObject거나 그 자식인 경우
        return other.gameObject == activatorObject
            || other.transform.IsChildOf(activatorObject.transform);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isPressed && IsActivator(other))
        {
            isPressed = true;
            if (buttonRenderer != null && pressedSprite != null)
                buttonRenderer.sprite = pressedSprite;
            if (objectToRemove != null)
                Destroy(objectToRemove);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isPressed && IsActivator(other))
        {
            isPressed = false;
            if (buttonRenderer != null && unpressedSprite != null)
                buttonRenderer.sprite = unpressedSprite;
        }
    }
}
