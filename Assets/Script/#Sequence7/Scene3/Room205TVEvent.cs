using System.Collections;
using UnityEngine;

public class Room205TVEvent : DialogueObject
{
    [Header("Scene Controller")]
    public Sequence7Scene3Controller SceneController;

    [Header("TV")]
    public Animator TVAnimator;
    public SpriteRenderer TVSpriteRenderer;
    public Sprite TVOffSprite;

    [Header("TV Sound")]
    public AudioSource TVNoiseAudioSource;

    [Header("Dialogue")]
    public string TVOffDialogueId = "room205_tv_off";


    private bool hasTalkedToTV = false;
    private bool isTVOff = false;


    protected override void OnInspect()
    {
        if (DialogueManager.isRunning)
            return;


        DialogueManager.StartDialogue(StartDialogue);


        if (!isTVOff && !hasTalkedToTV)
        {
            StartCoroutine(WaitForTVDialogueEnd());
        }
    }


    private IEnumerator WaitForTVDialogueEnd()
    {
        yield return new WaitUntil(
            () => DialogueManager.isRunning
        );

        yield return new WaitUntil(
            () => !DialogueManager.isRunning
        );


        hasTalkedToTV = true;

        Debug.Log("[Room205 TV] TV 조사 완료");


        CheckTVOffCondition();
    }


    public void CheckTVOffCondition()
    {
        if (isTVOff)
            return;


        if (SceneController == null)
            return;


        Debug.Log(
            "[Room205 TV] 조건 검사 | " +
            "콜시트 획득: " +
            SceneController.Room205Completed +
            " / TV 조사: " +
            hasTalkedToTV
        );


        if (
            SceneController.Room205Completed &&
            hasTalkedToTV
        )
        {
            TurnOffTV();
        }
    }


    private void TurnOffTV()
    {
        if (isTVOff)
            return;


        isTVOff = true;


        // TV 애니메이터 종료
        if (TVAnimator != null)
            TVAnimator.enabled = false;


        // 꺼진 화면으로 변경
        if (
            TVSpriteRenderer != null &&
            TVOffSprite != null
        )
        {
            TVSpriteRenderer.sprite = TVOffSprite;
        }


        // TV 잡음 종료
        if (TVNoiseAudioSource != null)
        {
            TVNoiseAudioSource.Stop();
        }


        // 이후 조사 대사 변경
        StartDialogue = TVOffDialogueId;


        Debug.Log("[Room205 TV] TV OFF");
    }
}