using UnityEngine;

public class ThirdFloorEmergencyDoorEvent : DialogueObject
{
    [Header("Key")]
    [SerializeField]
    private ItemData thirdFloorEmergencyKey;


    [Header("Dialogue IDs")]
    [SerializeField]
    private string noKeyDialogueId =
        "emergencyDoor_noKey";

    [SerializeField]
    private string hasKeyDialogueId =
        "emergencyDoor_hasKey";


    private bool doorOpened = false;


    public override void TryInspect()
    {
        if (doorOpened)
            return;


        if (DialogueManager == null)
            return;


        if (DialogueManager.isRunning)
            return;


        OnInspect();
    }


    protected override void OnInspect()
    {
        bool hasKey =
            InventoryManager.Instance != null &&
            thirdFloorEmergencyKey != null &&
            InventoryManager.Instance.HasItem(
                thirdFloorEmergencyKey
            );


        if (hasKey)
        {
            DialogueManager.StartDialogue(
                hasKeyDialogueId
            );
        }
        else
        {
            DialogueManager.StartDialogue(
                noKeyDialogueId
            );
        }
    }


    public void MarkOpened()
    {
        doorOpened = true;
    }
}