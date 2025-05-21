using System.Collections;
using UnityEngine;

public class DwarfController : MonoBehaviour, IFlashable
{
    [Header("Child Objects")]
    [SerializeField] private GameObject aliveChild;
    [SerializeField] private GameObject statueChild;

    [Header("Animator")]
    [SerializeField] private Animator aliveAnimator;

    [Header("Turn-to-Stone Settings")]
    [Tooltip("돌로 변하는 애니메이션 길이(초)")]
    [SerializeField] private float turnAnimDuration = 1f;

    [Header("Sound Settings")]
    [Tooltip("돌로 변할 때 재생할 SFX")]
    [SerializeField] private AudioClip turnToStoneSFX;
    private AudioSource audioSource;

    private bool isStatue = false;
    private Collider2D parentCollider;

    void Awake()
    {
        // 처음엔 살아있는 상태
        aliveChild.SetActive(true);
        statueChild.SetActive(false);
        isStatue = false;

        parentCollider = GetComponent<Collider2D>();

        audioSource = GetComponent<AudioSource>();
    }

    // IFlashable
    public void OnPhotoTaken(bool isEnhanced)
    {
        if (isStatue) return;
     

        // 1) 돌로 변하는 애니메이션 트리거
        aliveAnimator.SetTrigger("TurnToStone");

        // 2)소리
        if (turnToStoneSFX != null)
            audioSource.PlayOneShot(turnToStoneSFX);

        // 3) 전환 코루틴 시작
        StartCoroutine(SwitchToStatueAfterDelay());
    }

    private IEnumerator SwitchToStatueAfterDelay()
    {
        yield return new WaitForSeconds(turnAnimDuration);

        // 3) Alive → Statue
        aliveChild.SetActive(false);
        statueChild.SetActive(true);

        parentCollider.enabled = false;
        isStatue = true;
    }

    // 씬을 벗어났다가 다시 들어오면 Awake로 되돌아옵니다.
}
