using UnityEngine;
using UnityEngine.EventSystems;

// 마우스(Pointer) 이벤트를 받아서 즉시 소모(Use)해 버립니다.
public class BlockMouseInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public void OnPointerDown(PointerEventData eventData) => eventData.Use();
    public void OnPointerUp(PointerEventData eventData) => eventData.Use();
    public void OnDrag(PointerEventData eventData) => eventData.Use();
}
