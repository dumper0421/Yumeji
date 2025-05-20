using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public enum OpeningState
{
    None
}

public class OpeningDialogueController : DialogueController<OpeningState>
{
    [Header("BGM Clips")]
    public AudioClip RainBGM;
    public AudioClip ScaredBGM;
    public AudioClip Rain2BGM;

    [Header("SFX Clips")]
    public AudioClip CrackSFX;
    public AudioClip CameraCrackSFX;
    public AudioClip WhisperSFX;
    public AudioClip AccidentSFX;
    public AudioClip AmbulanceSFX;
    public AudioClip LaughterSFX;
    public AudioClip HornSFX;

    [Header("Cutscene Images")]
    public Image CutSceneImage;
    public Sprite[] CutSceneSprites; // Ensure length >= 5
    public SceneChanger SceneChanger;
    public void Start()
    {
        dialogueManager.StartDialogue("Effect0");
    }

    protected override void DialogueRunning(string dialogueId) { }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "Effect0":
                // 게임 시작 후 빗소리 BGM 재생
                SoundManager.Instance.PlayBGM(RainBGM);
                break;

            case "Effect1":
                // 컷1 페이드인
                CutSceneImage.gameObject.SetActive(true);
                CutSceneImage.sprite = CutSceneSprites[0];
                CutsceneManager.Instance.FadeFromBlack();
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect2":
                // 컷2 이미지 교체
                CutSceneImage.sprite = CutSceneSprites[1];
                break;

            case "Effect3":
                // 암전 + 우당탕탕 SFX
                CutSceneImage.gameObject.SetActive(false);
                SoundManager.Instance.PlaySFX(CrackSFX);
                break;

            case "Effect4":
                // 컷3 페이드인
                CutSceneImage.gameObject.SetActive(true);
                CutSceneImage.sprite = CutSceneSprites[2];
                CutsceneManager.Instance.FadeFromBlack();
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect5":
                // 빗소리1 끄고 기괴한 BGM 온
                SoundManager.Instance.StopBGM();
                SoundManager.Instance.PlayBGM(ScaredBGM);
                break;

            case "Effect6":
                // 컷4 페이드인
                CutSceneImage.sprite = CutSceneSprites[3];
                CutsceneManager.Instance.FadeFromBlack();
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect7":
                // 어두운 진홍색 페이드인 + 카메라 깨지고 속삭이는 SFX
                CutsceneManager.Instance.FadeFromBlack();
                SoundManager.Instance.PlaySFX(CameraCrackSFX);
                SoundManager.Instance.PlaySFX(WhisperSFX);
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect8":
                // 기괴한 BGM 끄고 두 번째 빗소리 BGM 재생
                SoundManager.Instance.StopBGM();
                SoundManager.Instance.PlayBGM(Rain2BGM);
                break;

            case "Effect9":
                // 회색빛 화면 페이드인 + 사고 소리 SFX
                CutsceneManager.Instance.FadeFromBlack();
                SoundManager.Instance.PlaySFX(AccidentSFX);
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect10":
                // 컷5 페이드인 + 앰뷸런스 SFX
                CutSceneImage.gameObject.SetActive(true);
                CutSceneImage.sprite = CutSceneSprites[4];
                CutsceneManager.Instance.FadeFromBlack();
                SoundManager.Instance.PlaySFX(AmbulanceSFX);
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect11":
                // 암전 + 웃음소리 SFX
                CutsceneManager.Instance.FadeToBlack();
                SoundManager.Instance.PlaySFX(LaughterSFX);
                StartCoroutine(StopDialogue(CutsceneManager.Instance.FadeDuration));
                break;

            case "Effect12":
                // 경적 소리 SFX
                SoundManager.Instance.PlaySFX(HornSFX);
                StartCoroutine(ChangeScene());
                break;
        }
    }

    protected override void HandleOption(string text, string nextId) { }
    protected override void OnPuzzleComplete() { }
    protected override void TryProgress() { }

    IEnumerator StopDialogue(float time)
    {
        dialogueManager.IsStop = true;
        yield return new WaitForSeconds(time);
        dialogueManager.IsStop = false;
    }

    IEnumerator ChangeScene()
    {
        yield return new WaitForSeconds(1f);
        SceneChanger.ChangeScene("Scenes/Choiwoohyck/Sequence1S#3_1_CutScene1");
    }
}
