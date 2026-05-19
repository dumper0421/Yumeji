using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  플레이어가 말걸 때 플레이어 방향으로 회전하는 오브젝트
/// </summary>
public class CharacterObject : DialogueObject
{

    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] private bool isSprite;
    public Animator CharacterAnimator;
    public SpriteRenderer CharacterSpriteRenderer;

    public Sprite LeftSprite;
    public Sprite FrontSprite;
    public Sprite BackSprite;



    protected override void OnInspect()
    {
       float playerdDirX = _playerMove.vector.x;    
       float playerdDirY = _playerMove.vector.y;

        if (!isSprite)
        {

            CharacterAnimator.SetFloat("DirX", playerdDirX * -1);
            CharacterAnimator.SetFloat("DirY", playerdDirY * -1);
        }

        else
        {
            if(playerdDirY == 0)
            {
                CharacterSpriteRenderer.sprite = LeftSprite;
                CharacterSpriteRenderer.flipX = playerdDirX == -1;
            }
            else
            {
                if (playerdDirY == -1 && BackSprite == null) return;
                CharacterSpriteRenderer.sprite = FrontSprite;
            }
        }
        
        base.OnInspect();
    }
}