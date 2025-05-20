using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public enum S1S7State
{
    None,
    Clear
}

public class Sequence1Scene7PuzzleController : DialogueController<S1S7State>
{
    public ItemData MatchData;
    public ItemData CharredPhotographData;


    public Image CharredPhotograph;
    public Image HaruMirror;
    public Image HaruMirrorBroken;

    public DialogueObject FirePlace;
    public DialogueObject Mirror;
    public DialoguePoint Cutton;
    public GameObject SceneChangeTrigger;

    [Header("Audio Clips")]
    public AudioClip MirrorBrokeClip;
    public AudioClip LPBgm;
    public AudioClip FireLPBgm;
    public AudioClip FireSFX;


    public Animator FirePlaceAnimator;
    public Animator[] FireAnimators;

    public Sprite BrokenMirror;
    public SpriteRenderer MirrorRenderer;

    public float delayTimer = 0f;

    public void LateUpdate()
    {
        if (CharredPhotograph.gameObject.activeSelf)
        {
            delayTimer += Time.deltaTime;
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && delayTimer >= 0.5f) 
            {
                CharredPhotograph.gameObject.SetActive(false);
                delayTimer = 0;
            }
        }

        if (HaruMirror.gameObject.activeSelf)
        {
            delayTimer += Time.deltaTime;
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && delayTimer >= 0.5f)
            {
                HaruMirror.gameObject.SetActive(false);
                StartCoroutine(ShowBrokenHaruImage());
                delayTimer = 0;
            }
        }
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch(dialogueId)
        {
            case "Save":
                PopupUIManager.Instance.SetSaveLoadPopup(true);
                break;

            case "Drawer_Match":
                InventoryManager.Instance.AddItem(MatchData);
                break;
            case "Bookshelf1_Slot1":
                InventoryManager.Instance.AddItem(CharredPhotographData);
                break;
            case "Fireplace_Lit":
                FirePlace.StartDialogue = "Fireplace2";
                FirePlaceAnimator.enabled = true;
                FirePlace.IsDisposable = true;
                FirePlace.hasBeenInspected = false;
                break;
            case "LPPlayerPlaying":
                SoundManager.Instance.StopAllSFX();
                if(state == S1S7State.Clear)
                    SoundManager.Instance.PlaySFX(LPBgm);
                else
                    SoundManager.Instance.PlaySFX(FireLPBgm);
                break;
            case "Mirror":
                HaruMirror.gameObject.SetActive(true);
                Mirror.StartDialogue = "Mirror_Break";
                MirrorRenderer.sprite = BrokenMirror;
                break;
        }
        TryProgress();
    }

    protected override void DialogueRunning(string dialogueId)
    {
        switch(dialogueId)
        {
            case "Bookshelf1_Illust":
                CharredPhotograph.gameObject.SetActive(true);
                break;
        }
    }

    protected override void HandleOption(string text, string nextId)
    {
        switch(nextId)
        {
            case "Fireplace_Lit":
                MatchData.Use();
                break;
            case "Fireplace_Phtograph":
                CharredPhotographData.Use();
                OnPuzzleComplete();
                break;
        }
    }

    protected override void OnPuzzleComplete()
    {
        SoundManager.Instance.PlaySFX(FireSFX);
        foreach (var anim in FireAnimators)
        {
            anim.gameObject.SetActive(true);
            anim.enabled = true;
        }

        state = S1S7State.Clear;
        Cutton.gameObject.SetActive(false);
        SceneChangeTrigger.SetActive(true);

    }

    protected override void TryProgress()
    {
    }

    IEnumerator ShowBrokenHaruImage()
    {
        HaruMirrorBroken.gameObject.SetActive(true);
        SoundManager.Instance.PlaySFX(MirrorBrokeClip);
        yield return new WaitForSeconds(2f);
        HaruMirrorBroken.gameObject.SetActive(false);
    }
}
