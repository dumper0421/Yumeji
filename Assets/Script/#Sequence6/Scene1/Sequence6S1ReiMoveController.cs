using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence6S1ReiMoveController : MonoBehaviour
{
    [Header("Companion Cutscene")]
    [SerializeField] private Transform companion;
    [SerializeField] private Animator companionAnimator;
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveSnap = 0.0001f;
    [SerializeField] private float afterMoveDelay = 0.2f;

    private Coroutine _routine;

    public bool IsPlaying { get; private set; }

    public void Play()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(Run());
    }

    public IEnumerator PlayAndWait()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        yield return StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        IsPlaying = true;

        if (companion == null || waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("[Sequence6S1ReiMoveController] companion 또는 waypoints가 비었다.");
            IsPlaying = false;
            yield break;
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform wp = waypoints[i];
            if (wp == null) continue;

            Vector3 start = companion.position;
            Vector3 target = wp.position;

            Vector3 delta = target - start;
            float distance = delta.magnitude;

            if (distance <= arriveSnap)
                continue;

            Vector3 dir = delta.normalized;

            if (companionAnimator != null)
            {
                companionAnimator.SetFloat("DirX", dir.x);
                companionAnimator.SetFloat("DirY", dir.y);
                companionAnimator.SetFloat("AnimSpeed", 1f);
                companionAnimator.SetBool("Walking", true);
            }

            float elapsed = 0f;
            float duration = distance / Mathf.Max(0.01f, moveSpeed);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                companion.position = Vector3.Lerp(start, target, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            companion.position = target;
        }

        if (companionAnimator != null)
            companionAnimator.SetBool("Walking", false);

        yield return new WaitForSeconds(afterMoveDelay);

        _routine = null;
        IsPlaying = false;
    }
}