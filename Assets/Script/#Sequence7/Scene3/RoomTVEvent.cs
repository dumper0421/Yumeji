using System.Collections;
using UnityEngine;

public class RoomTVEvent : DialogueObject
{
    [Header("Completion")]
    [SerializeField] private RoomCompletionGate completionGate;


    [Header("TV")]
    [SerializeField] private Animator tvAnimator;
    [SerializeField] private SpriteRenderer tvSpriteRenderer;
    [SerializeField] private Sprite tvOffSprite;


    [Header("TV Noise")]
    [SerializeField] private TvNoiseTrigger tvNoiseTrigger;


    [Header("Dialogue")]
    [SerializeField] private string tvOffDialogueId;


    private bool hasTalkedToTV = false;
    private bool isTVOff = false;


    protected override void OnInspect()
    {
        if (DialogueManager.isRunning)
            return;


        DialogueManager.StartDialogue(StartDialogue);


        // 켜진 TV를 최초로 조사했을 때만
        if (!isTVOff && !hasTalkedToTV)
        {
            StartCoroutine(WaitForTVDialogueEnd());
        }
    }


    private IEnumerator WaitForTVDialogueEnd()
    {
        // 대화 시작 대기
        yield return new WaitUntil(
            () => DialogueManager.isRunning
        );


        // 대화 종료 대기
        yield return new WaitUntil(
            () => !DialogueManager.isRunning
        );


        hasTalkedToTV = true;

        Debug.Log("[Room TV] TV 상호작용 완료");


        CheckTVOffCondition();
    }


    // 메인 이벤트가 나중에 끝난 경우에도
    // 외부에서 이 함수를 다시 호출할 수 있음
    public void CheckTVOffCondition()
    {
        if (isTVOff)
            return;


        if (completionGate == null)
        {
            Debug.LogWarning(
                "[Room TV] CompletionGate가 연결되지 않았습니다."
            );

            return;
        }


        Debug.Log(
            "[Room TV] TV OFF 조건 검사 | " +
            "메인 이벤트: " +
            completionGate.MainEventCompleted +
            " / TV 조사: " +
            hasTalkedToTV
        );


        // 메인 이벤트 + TV 조사
        if (completionGate.MainEventCompleted &&
            hasTalkedToTV)
        {
            TurnOffTV();
        }
    }


    private void TurnOffTV()
    {
        if (isTVOff)
            return;


        isTVOff = true;


        // TV 애니메이션 종료
        if (tvAnimator != null)
        {
            tvAnimator.enabled = false;
        }


        // 꺼진 TV 이미지
        if (tvSpriteRenderer != null &&
            tvOffSprite != null)
        {
            tvSpriteRenderer.sprite = tvOffSprite;
        }


        // TV 잡음 종료
        if (tvNoiseTrigger != null)
        {
            tvNoiseTrigger.TurnOffNoise();
        }


        // 이후 상호작용 대사 변경
        StartDialogue = tvOffDialogueId;


        Debug.Log("[Room TV] ★ TV OFF 완료 ★");


        // TV가 실제로 꺼진 시점에
        // 방 퍼즐 조건 하나 완료
        completionGate.MarkTVCompleted();
    }
}