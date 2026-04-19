using UnityEngine;

public class PuzzleLightInteractObject : InspectableObject
{
    [Header("References")]
    public DialogueManager dialogueManager;
    public PuzzleLightController lightController;

    [Header("First Dialogue")]
    public string FirstDialogueId;

    protected override void OnInspect()
    {
        if (lightController == null) return;

        // 아직 꺼져 있으면 대화
        if (!lightController.IsActivated)
        {
            if (dialogueManager != null && !string.IsNullOrEmpty(FirstDialogueId))
            {
                dialogueManager.StartDialogue(FirstDialogueId);
            }
            return;
        }

        // 이미 켜져 있으면 바로 색 전환
        lightController.CycleLight();
    }
}