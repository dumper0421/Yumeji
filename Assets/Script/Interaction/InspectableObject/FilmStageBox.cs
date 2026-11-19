using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilmStageBox : DialogueObject
{

    [SerializeField]
    private string _slideFilmDialogue;

    public bool IsFinshBadGuest = false;

    // 보관함 콜라이더 하나가 서랍 두 칸을 모두 덮고 있어서, 반짝이지 않는 칸을 조사해도 아이템을 얻는 문제가 있었다.
    // 지금 반짝이는 칸 앞에 서 있을 때만 조사되도록 칸 위치를 받아 플레이어 x 좌표로 확인한다. (비워두면 기존처럼 어디서든 조사)
    [Header("서랍 칸 (반짝이 위치)")]
    [Tooltip("첫 번째 아이템(노란색 사진 봉투) 서랍 칸")]
    [SerializeField] private Transform _firstDrawer;
    [Tooltip("두 번째 아이템(슬라이드 필름) 서랍 칸")]
    [SerializeField] private Transform _secondDrawer;
    [Tooltip("플레이어와 서랍 칸의 x 거리 허용치 (1칸 = 1)")]
    [SerializeField] private float _drawerTolerance = 0.5f;

    private PlayerMove_Test_Lerp _player;

    public override void TryInspect()
    {
        if (!IsFacingActiveDrawer())
            return;

        base.TryInspect();
    }

    private bool IsFacingActiveDrawer()
    {
        Transform drawer = IsFinshBadGuest ? _secondDrawer : _firstDrawer;
        if (drawer == null)
            return true;

        if (_player == null)
            _player = FindObjectOfType<PlayerMove_Test_Lerp>();
        if (_player == null)
            return true;

        return Mathf.Abs(_player.transform.position.x - drawer.position.x) < _drawerTolerance;
    }

    [SerializeField]
    protected override void OnInspect()
    {
        if (!DialogueManager.isRunning)
            DialogueManager.StartDialogue(StartDialogue);
        if (IsFinshBadGuest)
            DialogueManager.StartDialogue(_slideFilmDialogue);
    }
}
