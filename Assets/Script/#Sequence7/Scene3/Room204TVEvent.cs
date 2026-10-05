using System.Collections;
using UnityEngine;

public class Room204TVEvent : DialogueObject
{
    [Header("Room 204")]
    public Sequence7Scene3Controller SceneController;

    [Header("TV")]
    public Animator TVAnimator;

    private bool hasTalkedToTV = false;
    private bool isTVOff = false;

    protected override void OnInspect()
    {
        if (DialogueManager.isRunning)
            return;

        DialogueManager.StartDialogue(StartDialogue);

        // TV가 켜져 있을 때만 최초 대화 여부 확인
        if (!isTVOff && !hasTalkedToTV)
        {
            StartCoroutine(WaitForTVDialogueEnd());
        }
    }

    private IEnumerator WaitForTVDialogueEnd()
    {
        // 대화 시작 대기
        yield return new WaitUntil(() => DialogueManager.isRunning);

        // 대화 종료 대기
        yield return new WaitUntil(() => !DialogueManager.isRunning);

        hasTalkedToTV = true;

        CheckTVOffCondition();
    }

    public void CheckTVOffCondition()
    {
        if (isTVOff)
            return;

        // 드레스 이벤트 완료 + TV 1회 이상 조사
        if (SceneController.Room204Completed && hasTalkedToTV)
        {
            TurnOffTV();
        }
    }

    private void TurnOffTV()
    {
        isTVOff = true;

        // TV 애니메이션 종료
        if (TVAnimator != null)
            TVAnimator.enabled = false;

        // 이후 TV 조사 대사 변경
        StartDialogue = "room204_tv_off";

        Debug.Log("[Room204] TV OFF");
    }
}