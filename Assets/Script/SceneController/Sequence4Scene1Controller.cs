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

    protected override void OnStopIntervalReached()
    {
    }

    protected override void Start()
    {
        base.Start();
       _dialogueManager.StartDialogue("HotelEntrance_Haru_OpeningMonologue");

    }


}
