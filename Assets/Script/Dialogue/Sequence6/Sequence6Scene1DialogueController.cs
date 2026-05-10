using System.Collections;
using UnityEngine;

public enum S6S1State
{
    None,
    RayDialogueStarted,
    RayMoved,
    RayDialogueFinished
}

public class Sequence6Scene1DialogueController : DialogueController<S6S1State>
{
    [Header("Ray Move")]
    public Sequence6S1ReiMoveController reiMoveController;

    [Header("Ray Position Restore")]
    public Transform rayNpc;
    public Transform rayMoveTarget;

    [Header("Dialogue Start IDs")]
    public string rayDialogue1StartId = "ray_dialogue_1";
    public string rayDialogue2StartId = "ray_dialogue_2";

    [Header("Dialogue End IDs")]
    public string rayDialogue1EndId = "ray_dialogue_1_21";
    public string rayDialogue2EndId = "ray_dialogue_2_03";

    [Header("After Event Dialogue")]
    public string rayAfterDialogueId = "ray_after_dialogue";

    [Header("Player Lock")]
    public PlayerMove_Test_Lerp playerMoveTestLerp;
    public PlayerActionController playerAction;

    private bool isRayMoving = false;

    protected override void ApplyWorldByState()
    {
        // 저장된 상태가 레이 이동 이후라면,
        // 씬 재진입 시 레이를 이동 완료 위치에 배치
        if (IsStateAtLeast(S6S1State.RayMoved))
        {
            if (rayNpc != null && rayMoveTarget != null)
            {
                rayNpc.position = rayMoveTarget.position;
            }
        }

        // 저장 복원 시 조작 잠금이 남아있지 않게 보장
        if (state == S6S1State.RayDialogueFinished)
        {
            LockPlayerControls(false);
        }
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        if (dialogueId == rayDialogue1EndId)
        {
            StartCoroutine(MoveRayThenStartSecondDialogue());
        }
        else if (dialogueId == rayDialogue2EndId)
        {
            state = S6S1State.RayDialogueFinished;
            PersistPuzzleState();

            LockPlayerControls(false);
        }
        else if (dialogueId == rayAfterDialogueId)
        {
            LockPlayerControls(false);
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

    public void TryTalkToRay()
    {
        if (dialogueManager == null) return;

        // 최초 이벤트 전이면 긴 이벤트 대화 시작
        if (state == S6S1State.None)
        {
            StartRayEventDialogue();
            return;
        }

        // 모든 이벤트가 끝난 뒤 레이에게 말 걸면 반복 대사
        if (state == S6S1State.RayDialogueFinished)
        {
            StartRayAfterDialogue();
            return;
        }
    }

    private void StartRayEventDialogue()
    {
        state = S6S1State.RayDialogueStarted;
        PersistPuzzleState();

        LockPlayerControls(true);

        dialogueManager.StartDialogue(rayDialogue1StartId);
    }

    private void StartRayAfterDialogue()
    {
        LockPlayerControls(true);

        dialogueManager.StartDialogue(rayAfterDialogueId);
    }

    private IEnumerator MoveRayThenStartSecondDialogue()
    {
        if (isRayMoving) yield break;

        isRayMoving = true;

        LockPlayerControls(true);

        if (reiMoveController != null)
        {
            yield return StartCoroutine(reiMoveController.PlayAndWait());
        }
        else
        {
            Debug.LogWarning("[Sequence6Scene1DialogueController] reiMoveController가 연결되지 않았다.");
        }

        state = S6S1State.RayMoved;
        PersistPuzzleState();

        isRayMoving = false;

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(rayDialogue2StartId);
        }
    }

    private void LockPlayerControls(bool locked)
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.canMove = !locked;

        if (playerAction != null)
            playerAction.IsLockedByEvent = locked;
    }

    private bool IsStateAtLeast(S6S1State targetState)
    {
        return state.CompareTo(targetState) >= 0;
    }

    public bool IsRayDialogueFinished()
    {
        return state == S6S1State.RayDialogueFinished;
    }
}