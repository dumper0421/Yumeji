using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReiMoveController : MonoBehaviour
{
    [Header("Player Lock")]
    [SerializeField] private PlayerMove_Test_Lerp playerMove;
    [SerializeField] private bool lockPlayer = true;

    [Header("Companion Cutscene")]
    [SerializeField] private Transform companion;
    [SerializeField] private GameObject Rei;
    [SerializeField] private GameObject Rei_Seated;
    [SerializeField] private Animator companionAnimator;
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 2f;      // 초당 이동 유닛 수
    [SerializeField] private float arriveSnap = 0.0001f;

    private Coroutine _routine;
    private CompanionSystem _savedCompanion;

    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (companion == null || waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("[ReiMoveController] companion 또는 waypoints가 비었다.");
            yield break;
        }

        if (lockPlayer)
        {
            if (playerMove == null)
            {
                Debug.LogWarning("[ReiMoveController] playerMove가 비었다.");
                yield break;
            }

            if (playerMove.animator != null)
                playerMove.animator.SetBool("Walking", false);

            playerMove.enabled = false;

            _savedCompanion = playerMove.Companion;
            playerMove.Companion = null;
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
                companionAnimator.SetFloat("AnimSpeed", 1f); // 필요하면 여기 조절
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

        yield return new WaitForSeconds(1f);
        if (lockPlayer && playerMove != null)
        {
            playerMove.Companion = _savedCompanion;
            playerMove.enabled = true;
        }

        _routine = null;
        Destroy(Rei);
        Rei_Seated.SetActive(true);
    }
}