using UnityEngine;
using UnityEngine.EventSystems;

public class ImageHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale_;
    [SerializeField]
    private float scaleFactor_ = 1.2f;

    private void Start()
    {
        originalScale_ = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale_ * scaleFactor_;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale_;
    }
}
