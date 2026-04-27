using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum S5S3State
{
    None
}
public class Sequence5Scene3DialogueController : DialogueController<S5S3State>
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private AstarEnemy _enemy;
    [SerializeField] private float _enemyWaitTime;


    [Header("Haru")]
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] private Animator _haruAnimator;
    [SerializeField] private SpriteRenderer _haruSpriteRenderer;
    [SerializeField] private Sprite _haruRightStand;
    protected override void ApplyWorldByState()
    {
        _haruAnimator.enabled = false;
        _playerMove.enabled = false;
        _haruSpriteRenderer.sprite = _haruRightStand;

        dialogueManager.StartDialogue("Haru_01");
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId)
        {
            case "Haru_03":
                _enemy.isMove = false;
                _haruAnimator.enabled = true;
                Utility.PlayDirector(_director);
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

    public void MoveEnemy()
    {
        _haruAnimator.enabled = true;

        StartCoroutine(Co_MoveEnemy()); 
    }

    IEnumerator Co_MoveEnemy()
    {
        yield return new WaitForSeconds(_enemyWaitTime);
        _enemy.isMove = true;
    }
}
