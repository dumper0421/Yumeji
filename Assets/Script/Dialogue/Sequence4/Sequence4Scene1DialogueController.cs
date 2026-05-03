using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum S4S1State
{

}

public class Sequence4Scene1DialogueController : DialogueController<S4S1State>
{
    public Image FadeImage;

    [Header("Haru")]
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] private Animator _haruAnimator;

    [SerializeField] private ItemData _invitationData;
    [Header("Rei")]
    [SerializeField] private CompanionSystem _reiCompanionSystem;
    [SerializeField] private Animator _reiAnimator;


    [SerializeField] private DialoguePoint _partyDialoguePoint;
    [SerializeField] private SceneChangeTrigger _sceneChangeTrigger;
    [SerializeField] private CharacterObject _hotelier;

    protected override void ApplyWorldByState()
    {
    }

    protected override void DialogueRunning(string dialogueId)
    {
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "HotelEntrance_Haru_TryRemember":
                InventoryManager.Instance.AddItem(_invitationData);

                StartCoroutine(FadeOut(2f));
                break;
            case "HotelEntrance_CheckInvitePrompt":
                _playerMove.SetCompanion(_reiCompanionSystem, "∑π¿Ã");
                _reiAnimator.enabled = true;
                _haruAnimator.enabled = true;
                break;
            case "HotelEntrance_Hotelier":
                _invitationData.Use();
                _partyDialoguePoint.enabled = false;
                _sceneChangeTrigger.enabled = true;
                _hotelier.StartDialogue = "Already_Hotelier";
                break;

        }
    }

    protected override void HandleOption(string text, string nextId)
    {
    }

    protected override void OnPuzzleComplete()
    {
    }

    protected override void TryProgress()
    {
    }

    IEnumerator FadeOut(float fadeDuration)
    {
        if (FadeImage == null) yield break;
        _playerMove.enabled = false;
        _haruAnimator.enabled = false;
        float elapsed = 0f;
        Color c = FadeImage.color;

        FadeImage.color = new Color(c.r, c.g, c.b, 1f);
        elapsed = 0;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : (elapsed / fadeDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);
            FadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        FadeImage.color = new Color(c.r, c.g, c.b, 0f);
    }

}
