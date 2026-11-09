using System.Collections;
using UnityEngine;

public class Room204TVEvent : DialogueObject
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
    public string TVOffDialogueId = "room204_tv_off";

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
        yield return new WaitUntil(() => DialogueManager.isRunning);
        yield return new WaitUntil(() => !DialogueManager.isRunning);

        hasTalkedToTV = true;

        Debug.Log("[Room204 TV] TV 조사 완료");

        CheckTVOffCondition();
    }


    public void CheckTVOffCondition()
    {
        if (isTVOff)
            return;

        if (SceneController == null)
            return;

        if (SceneController.Room204Completed && hasTalkedToTV)
        {
            TurnOffTV();
        }
    }


    // 방에 들어왔을 때 호출
    public void StartTVNoise()
    {
        if (isTVOff)
        {
            Debug.Log("[Room204 TV] TV가 이미 꺼져 있어서 잡음을 재생하지 않음");
            return;
        }

        if (TVNoiseAudioSource == null)
            return;

        // 이미 재생 중이면 다시 시작하지 않음
        if (!TVNoiseAudioSource.isPlaying)
        {
            TVNoiseAudioSource.Play();
            Debug.Log("[Room204 TV] TV 잡음 재생");
        }
    }


    private void TurnOffTV()
    {
        if (isTVOff)
            return;

        isTVOff = true;

        // TV 애니메이션 종료
        if (TVAnimator != null)
            TVAnimator.enabled = false;

        // 꺼진 TV 이미지로 변경
        if (TVSpriteRenderer != null && TVOffSprite != null)
            TVSpriteRenderer.sprite = TVOffSprite;

        // TV 잡음 종료
        if (TVNoiseAudioSource != null)
        {
            TVNoiseAudioSource.Stop();
        }

        // 이후 TV 대사 변경
        StartDialogue = TVOffDialogueId;

        Debug.Log("[Room204 TV] TV OFF / 잡음 종료");
    }
}