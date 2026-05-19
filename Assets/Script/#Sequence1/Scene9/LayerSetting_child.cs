using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class LayerSetting_child : MonoBehaviour
{
    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()   
    {
        // transform.position.y 는 자식의 월드 Y 좌표이므로
        // 부모 계층 구조와 상관없이 항상 올바른 화면상의 높이를 가져옵니다.
        _sr.sortingOrder = Mathf.RoundToInt(transform.position.y * -1f);
        // * -100f 는 소수점 차이를 더 세밀하게 반영하기 위해 곱한 예시입니다.
    }
}

