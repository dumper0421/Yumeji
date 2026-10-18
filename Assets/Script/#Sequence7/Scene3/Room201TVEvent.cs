using UnityEngine;

public class Room201TVEvent : DialogueObject
{
    [Header("TV")]
    [SerializeField] private Animator tvAnimator;
    [SerializeField] private SpriteRenderer tvSpriteRenderer;
    [SerializeField] private Sprite tvOffSprite;
    [SerializeField] private Collider2D tvCollider;


    [Header("TV Noise")]
    [SerializeField] private TvNoiseTrigger tvNoiseTrigger;


    [Header("Call Sheet Sparkle")]
    [SerializeField] private GameObject callSheetSparkle;


    [Header("Completion")]
    [SerializeField] private RoomCompletionGate completionGate;


    private bool isTVOff = false;


    private void Awake()
    {
        // TV가 꺼질 때까지 반짝이는 숨김
        if (callSheetSparkle != null)
        {
            callSheetSparkle.SetActive(false);
        }
    }


    // =========================================================
    // 켜져 있는 TV 조사
    // =========================================================

    protected override void OnInspect()
    {
        if (DialogueManager.isRunning)
            return;


        if (isTVOff)
            return;


        // 전화 이벤트와 상관없이
        // TV가 켜져 있다면 room201_tv_on 대사 출력
        DialogueManager.StartDialogue(
            StartDialogue
        );
    }


    // =========================================================
    // TV OFF
    // =========================================================

    // Room201PhoneEvent에서
    // 박수 SFX가 끝나는 순간 호출
    public void TurnOffTV()
    {
        if (isTVOff)
            return;


        isTVOff = true;


        Debug.Log(
            "[Room201 TV] ★ TV 자동 OFF 시작 ★"
        );


        // TV 애니메이션 정지
        if (tvAnimator != null)
        {
            tvAnimator.enabled = false;
        }


        // 꺼진 TV 이미지 적용
        if (tvSpriteRenderer != null &&
            tvOffSprite != null)
        {
            tvSpriteRenderer.sprite =
                tvOffSprite;
        }


        // TV 잡음 종료
        if (tvNoiseTrigger != null)
        {
            tvNoiseTrigger.TurnOffNoise();
        }


        // TV 자체 상호작용 종료
        if (tvCollider != null)
        {
            tvCollider.enabled = false;
        }


        // 콜시트 반짝이 등장
        if (callSheetSparkle != null)
        {
            callSheetSparkle.SetActive(true);
        }


        // RoomCompletionGate의 TV 조건 완료
        if (completionGate != null)
        {
            completionGate.MarkTVCompleted();
        }


        Debug.Log(
            "[Room201 TV] ★ TV OFF / Noise OFF / Sparkle ON ★"
        );
    }
}