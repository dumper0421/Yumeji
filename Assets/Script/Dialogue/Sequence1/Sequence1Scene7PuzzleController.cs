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
    public PlayerMove_Test_Lerp PlayerMove;

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
    public Animator PlayerAnimator;

    public Animator[] FireAnimators;

    public Sprite BrokenMirror;
    public SpriteRenderer MirrorRenderer;

    public float delayTimer = 0f;
    public GameObject Bloom;
    public GameObject[] InteractionLight;
    Coroutine ShowBrokenHaruImageCoroutine;
    public void LateUpdate()
    {
        if (CharredPhotograph.gameObject.activeSelf)
        {
            delayTimer += Time.deltaTime;
            PlayerMove.enabled = false;
            dialogueManager.IsStop = true;
            if ((Input.GetKeyDown(KeyCode.Return)) && delayTimer >= 0.5f) 
            {
                CharredPhotograph.gameObject.SetActive(false);
                dialogueManager.IsStop = false;
                dialogueManager._waitingForInput = true;
                PlayerMove.enabled = true;
                PlayerAnimator.enabled = true;
                delayTimer = 0;
            }
        }

        if (HaruMirror.gameObject.activeSelf)
        {
            delayTimer += Time.deltaTime;
            PlayerMove.enabled = false;
            dialogueManager.isRunning = true;
            if ((Input.GetKeyDown(KeyCode.Return)) && delayTimer >= 0.5f)
            {
                if (ShowBrokenHaruImageCoroutine == null)
                ShowBrokenHaruImageCoroutine = StartCoroutine(ShowBrokenHaruImage());
                delayTimer = 0;
            }
        }
    }
    protected override void HandleDialogueEnd(string dialogueId)
    {
        PlayerAnimator.enabled = true;
        PlayerMove.enabled = true;

        switch (dialogueId)
        {
            case "Save":
                PopupUIManager.Instance.SetSaveLoadPopup(true);
                break;

            case "Drawer_Match":
                InventoryManager.Instance.AddItem(MatchData);
                InteractionLight[0].SetActive(false);
                PlayerAnimator.enabled = true;
                PlayerMove.enabled = true;
                break;
            case "Drawer1":
                PlayerAnimator.enabled = true;
                PlayerMove.enabled = true;
                break;
            case "Bookshelf1_Slot1":
                InventoryManager.Instance.AddItem(CharredPhotographData);
                InteractionLight[1].SetActive(false);
                break;
            case "Bookshelf1_Illust":
                PlayerMove.enabled = false;
                break;
            case "Fireplace_Lit":
                FirePlace.StartDialogue = "Fireplace2";
                FirePlaceAnimator.enabled = true;
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
            case "Bookshelf1_Illust_Earn":
                PlayerMove.enabled = true;
                break;
        }
        TryProgress();
    }

    protected override void DialogueRunning(string dialogueId)
    {
        PlayerMove.enabled = false;
        PlayerAnimator.enabled = false;

        switch (dialogueId)
        {
            case "Bookshelf1_Illust":
                CharredPhotograph.gameObject.SetActive(true);
                break;
            case "Drawer_Match":
                PlayerMove.enabled = false;
                PlayerAnimator.enabled = false;
                break;
            case "Drawer1":
                PlayerMove.enabled = false;
                PlayerAnimator.enabled = false;
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
                FirePlace.IsDisposable = true;
                FirePlace.hasBeenInspected = false;
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
        Bloom.SetActive(true);
        state = S1S7State.Clear;
        Cutton.gameObject.SetActive(false);
        SceneChangeTrigger.SetActive(true);

    }

    protected override void TryProgress()
    {
    }

    IEnumerator ShowBrokenHaruImage()
    {
        yield return new WaitForSeconds(1.5f);
        HaruMirror.gameObject.SetActive(false);
        HaruMirrorBroken.gameObject.SetActive(true);
        SoundManager.Instance.PlaySFX(MirrorBrokeClip);
        yield return new WaitForSeconds(2f);
        HaruMirrorBroken.gameObject.SetActive(false);
        dialogueManager.isRunning = false;
        PlayerMove.enabled = true;
    }

    protected override void ApplyWorldByState()
    {
    }
}
