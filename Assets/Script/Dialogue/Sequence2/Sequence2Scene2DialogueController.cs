using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum S2S2State
{
    None,
    TicketSelected,
    TicketReceived
}
public class Sequence2Scene2DialogueController : DialogueController<S2S2State>

{
    [SerializeField] private DialogueObject ticketBoothObject;
    [SerializeField] private GameObject entranceWall;

    [Header("Ticket Items")]
    [SerializeField] private ItemData lilyTicket;
    [SerializeField] private ItemData mulhollandTicket;
    [SerializeField] private ItemData oneOrTwoTicket;

    private ItemData selectedTicket;

    [Header("SFX")]
    [SerializeField] private AudioClip haruCarSFX;

    [SerializeField] private string nextSceneName;
    protected override void HandleOption(string text, string nextId)
    {
        // 선택지 → 티켓 결정
        switch (text)
        {
            case "릴리에 관한 모든 것":
                selectedTicket = lilyTicket;
                break;

            case "멀홀랜드 드라이빙":
                selectedTicket = mulhollandTicket;
                break;

            case "하나 혹은 둘":
                selectedTicket = oneOrTwoTicket;
                break;
        }

        state = S2S2State.TicketSelected;

        dialogueManager.StartDialogue(nextId);
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == "Car_After_Movie")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);

        }

        if (dialogueId == "Haru_Car" && haruCarSFX != null)
        {
            SoundManager.Instance.PlaySFX(haruCarSFX);
        }

        if (dialogueId == "TicketBooth06"
            && state == S2S2State.TicketSelected
            && selectedTicket != null)
        {
            InventoryManager.Instance.AddItem(selectedTicket);
            state = S2S2State.TicketReceived;

            ticketBoothObject.StartDialogue = "TicketBooth_Received";
            ticketBoothObject.hasBeenInspected = false;

            entranceWall.SetActive(false);
        }

        TryProgress();
    }

    protected override void TryProgress()
    {
    }

    protected override void OnPuzzleComplete() { }

    protected override void ApplyWorldByState()
    {
    }
}