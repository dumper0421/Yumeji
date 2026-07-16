using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Sequence4Scene1Controller : SceneController
{
    [Header("Haru")]
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] private Animator _haruAnimator;

    [SerializeField] private DialogueManager _dialogueManager;
    [SerializeField] private Sequence4Scene1DialogueController _dialogueController;

    protected override void OnStopIntervalReached()
    {
    }

    protected override void Start()
    {
        base.Start();

        // 이미 저장된 진행 상태(state)가 있다면 오프닝 대사/페이드를 다시 재생하지 않는다.
        if (_dialogueController == null || _dialogueController.state == S4S1State.None)
            _dialogueManager.StartDialogue("HotelEntrance_Haru_OpeningMonologue");
    }


}
