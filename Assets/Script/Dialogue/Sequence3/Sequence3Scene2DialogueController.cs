using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
public enum S3S2State
{
    None
}
public class Sequence3Scene2DialogueController : DialogueController<S3S2State>
{
    public DialogueManager DialogueManager;

    public SpriteRenderer HaruSpriteRenderer;
    public Sprite HaruSitDownSprite;

    public SpriteRenderer ReiSpriteRenderer;
    public Sprite ReiSitDownSprite;

    public Animator HaruAnimator;
    public Animator ReiAnimator;

    public AudioClip HeadPhoneBGM;
    public AudioClip TrainBGM;

    public PlayableDirector Director2;

    public Image FadeImage;

    public PlayerMove_Test_Lerp PlayMove;

    public DialogueObject ReiDialogueObject;
    public List<DialoguePoint> DialoguePoints;

    protected override void Awake()
    {
        base.Awake();
        SoundManager.Instance.PlayBGM(HeadPhoneBGM);
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "HaruRei1_2":
                SoundManager.Instance.StopBGM();
                SoundManager.Instance.SetBGMSourceVolume(0.5f);
                SoundManager.Instance.PlayBGM(TrainBGM);
                HaruSpriteRenderer.sprite = HaruSitDownSprite;
                break;
            case "HaruRei1_29":
                Utility.PlayDirector(Director2);
                HaruSpriteRenderer.sprite = HaruSitDownSprite;
                PlayMove.enabled = false;
                break;
            case "HaruRei1_30":
                SoundManager.Instance.SetBGMSourceVolume(1.0f);

                break;
            case "HaruRei1_32":
                StartCoroutine(FadeOutToIn(1.0f));
                break;
            case "HaruRei1_33":
                ReiAnimator.enabled = false;
                ReiSpriteRenderer.sprite = ReiSitDownSprite;
                break;
            case "HaruRei1_36":
                ReiDialogueObject.IsDisposable = false;
                ReiDialogueObject.hasBeenInspected = false;
                ReiDialogueObject.StartDialogue = "HaruRepeat";
                foreach(var dialoguePoint in DialoguePoints) 
                    dialoguePoint.gameObject.SetActive(false);
                break;
        }
    }

    public void StopPlayer()
    {
        PlayMove.enabled = false;
    }

    public override void OnStartedDialogue(string DialogueID)
    {
        base.OnStartedDialogue(DialogueID);
        if (DialogueID == "HaruRei1_1")
        {
            HaruAnimator.enabled = false;
        }
        if (DialogueID == "HaruRei1_30")
        {
            ReiAnimator.SetTrigger("isSleep");
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
    }

    protected override void TryProgress()
    {
    }

    protected override void OnPuzzleComplete()
    {
    }


    IEnumerator FadeOutToIn(float fadeDuration)
    {
        if (FadeImage == null) yield break;
        PlayMove.enabled = false;
        float elapsed = 0f;
        Color c = FadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : (elapsed / fadeDuration);
            float alpha = Mathf.Lerp(0f, 1f, t);
            FadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        FadeImage.color = new Color(c.r, c.g, c.b, 1f);
        elapsed = 0;
        while (elapsed < fadeDuration * 2)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : (elapsed / fadeDuration*2f);
            float alpha = Mathf.Lerp(1f, 0f, t);
            FadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        FadeImage.color = new Color(c.r, c.g, c.b, 0f);
        PlayMove.gameObject.transform.position = new Vector3(-11.5f, 0.25f);
        HaruAnimator.enabled = true;
        HaruAnimator.ResetTrigger("SitDown");
        HaruAnimator.SetTrigger("SitDownEnd");
        PlayMove.enabled = true;
    }
}
