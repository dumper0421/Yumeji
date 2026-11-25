
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sequence9S#1_A 전용 위치 교환 이벤트.
/// 첫 번째 도플갱어와 위치 교환이 완료되면
/// 첫 번째 도플갱어를 제거하고 두 번째 도플갱어를 페이드인시킨다.
/// </summary>
public class Sequence9S1_ASwapEvent : MonoBehaviour
{
    [Header("Doppelganger References")]
    [SerializeField] private GameObject firstDoppelganger;
    [SerializeField] private GameObject secondDoppelganger;

    [Header("Second Doppelganger Position")]
    [Tooltip("두 번째 도플갱어가 등장할 위치")]
    [SerializeField] private Transform secondSpawnPoint;

    [Header("Fade In Settings")]
    [Tooltip("두 번째 도플갱어가 나타나는 데 걸리는 시간")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 1f;

    [Header("Events")]
    [Tooltip("두 번째 도플갱어가 완전히 나타난 후 실행")]
    [SerializeField] private UnityEvent onSecondAppeared;

    private SpriteRenderer[] secondRenderers;
    private Color[] originalColors;

    private DoppelgangerPositionSwap secondSwap;
    private bool secondSwapWasEnabled;

    private bool sequenceStarted = false;

    private void Awake()
    {
        if (secondDoppelganger == null)
            return;

        // 두 번째 도플갱어의 SpriteRenderer를 모두 가져온다.
        secondRenderers =
            secondDoppelganger.GetComponentsInChildren<SpriteRenderer>(true);

        originalColors = new Color[secondRenderers.Length];

        for (int i = 0; i < secondRenderers.Length; i++)
        {
            originalColors[i] = secondRenderers[i].color;
        }

        // 페이드인 중에는 두 번째 도플갱어의 위치 교환을 막는다.
        secondSwap =
            secondDoppelganger.GetComponent<DoppelgangerPositionSwap>();

        if (secondSwap != null)
        {
            secondSwapWasEnabled = secondSwap.enabled;
            secondSwap.enabled = false;
        }

        // 씬 시작 시 두 번째 도플갱어는 등장하지 않는다.
        secondDoppelganger.SetActive(false);
    }

    /// <summary>
    /// 첫 번째 도플갱어의 On Swap Resumed 이벤트에서 호출.
    /// 위치 교환과 점멸, 추가 대기가 끝나면 실행된다.
    /// </summary>
    public void FirstSwapCompleted()
    {
        if (sequenceStarted)
            return;

        if (firstDoppelganger == null || secondDoppelganger == null)
        {
            Debug.LogWarning(
                "[Sequence9S1_A] 도플갱어 참조가 누락되었습니다.",
                this
            );
            return;
        }

        sequenceStarted = true;

        // 첫 번째 도플갱어를 사라지게 한다.
        firstDoppelganger.SetActive(false);

        // 두 번째 도플갱어 등장 시작.
        StartCoroutine(FadeInSecondDoppelganger());
    }

    private IEnumerator FadeInSecondDoppelganger()
    {
        // 지정된 등장 위치로 배치한다.
        if (secondSpawnPoint != null)
        {
            Vector3 position = secondSpawnPoint.position;
            position.z = secondDoppelganger.transform.position.z;

            secondDoppelganger.transform.position = position;
        }

        // 두 번째 도플갱어 활성화.
        secondDoppelganger.SetActive(true);

        // 처음에는 완전히 투명하게 만든다.
        SetSecondAlpha(0f);

        float elapsed = 0f;

        // 서서히 페이드인.
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = fadeInDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeInDuration)
                : 1f;

            SetSecondAlpha(t);

            yield return null;
        }

        // 원래 투명도로 복구.
        RestoreSecondColors();

        // 페이드인이 끝난 후 위치 교환 기능 복구.
        if (secondSwap != null)
            secondSwap.enabled = secondSwapWasEnabled;

        // 후속 이벤트 실행.
        onSecondAppeared?.Invoke();
    }

    /// <summary>
    /// 두 번째 도플갱어의 원래 Alpha값을 기준으로 투명도를 조절한다.
    /// </summary>
    private void SetSecondAlpha(float alpha)
    {
        if (secondRenderers == null)
            return;

        for (int i = 0; i < secondRenderers.Length; i++)
        {
            if (secondRenderers[i] == null)
                continue;

            Color color = originalColors[i];
            color.a *= Mathf.Clamp01(alpha);

            secondRenderers[i].color = color;
        }
    }

    private void RestoreSecondColors()
    {
        if (secondRenderers == null)
            return;

        for (int i = 0; i < secondRenderers.Length; i++)
        {
            if (secondRenderers[i] != null)
                secondRenderers[i].color = originalColors[i];
        }
    }
}
