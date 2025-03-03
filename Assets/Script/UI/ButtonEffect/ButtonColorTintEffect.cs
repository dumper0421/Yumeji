using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonColorTintEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Color originColor_;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
      originColor_ = GetComponent<Image>().color;
      GetComponent<Image>().color = originColor_ * 1.3f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().color = originColor_;
    }
}
