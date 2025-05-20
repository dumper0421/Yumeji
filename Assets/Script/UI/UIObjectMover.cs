using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIObjectMover : MonoBehaviour
{
    [SerializeField] private RectTransform uiRect;  // 이동할 UI 객체
    [SerializeField, Min(0f)] private float duration = 1f;  // 애니메이션 지속 시간(초)
    public SceneChanger sceneChanger;
    private void Start()
    {
        // 시작 시 자동으로 이동을 원하지 않으면 이 줄을 제거하고,
        // 원하는 시점에 MoveUI()를 호출하세요.
        StartCoroutine(MoveUI(400f, 4000f, duration));
    }

    /// <summary>
    /// y값을 from에서 to로 duration 초 동안 부드럽게 이동
    /// </summary>
    private IEnumerator MoveUI(float from, float to, float duration)
    {
        if (uiRect == null)
        {
            Debug.LogError("UIObjectMover: uiRect가 할당되지 않았습니다.");
            yield break;
        }

        Vector2 pos = uiRect.anchoredPosition;
        float elapsed = 0f;

        // 초기 위치 세팅
        pos.y = from;
        uiRect.anchoredPosition = pos;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 부드러운 보간 (Linear), 원한다면 Mathf.SmoothStep(t) 사용 가능
            float newY = Mathf.Lerp(from, to, t);

            pos.y = newY;
            uiRect.anchoredPosition = pos;

            yield return null;
        }

        // 최종 위치 보정
        pos.y = to;
        uiRect.anchoredPosition = pos;
        sceneChanger.ChangeScene("TitleScene");
    }

    /// <summary>
    /// 외부에서 호출해서 이동 시작하기
    /// </summary>
    public void PlayMove()
    {
        StopAllCoroutines();
        StartCoroutine(MoveUI(400f, 4000f, duration));
    }
}
