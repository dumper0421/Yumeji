using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Slider를 상속해 포인터/드래그 입력을 통째로 무시합니다.
public class KeyboardOnlySlider : Slider
{
    public override void OnPointerDown(PointerEventData eventData) { /* no-op */ }
    public override void OnPointerUp(PointerEventData eventData) { /* no-op */ }
    public override void OnDrag(PointerEventData eventData) { /* no-op */ }
    public override void OnInitializePotentialDrag(PointerEventData eventData) { /* no-op */ }
}
