using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  플레이어가 말걸 떄 플레이어 방향으로 회전하는 오브젝트
/// </summary>
public class CharacterObject : DialogueObject
{
    [SerializeField]
    private PlayerMove_Test_Lerp _playerMove;

    public Animator CharacterAnimator;

    protected override void OnInspect()
    {
       float playerdDirX = _playerMove.vector.x;    
       float playerdDirY = _playerMove.vector.y;

        CharacterAnimator.SetFloat("DirX", playerdDirX * -1);
        CharacterAnimator.SetFloat("DirY", playerdDirY * -1);

        base.OnInspect();
    }
}
