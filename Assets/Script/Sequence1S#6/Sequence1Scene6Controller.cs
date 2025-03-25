using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene6Controller : MonoBehaviour
{
    [SerializeField]
    private Animator lunaAnimator_;
    [SerializeField]
    private float stopInterval_ = 1f;
    [SerializeField]
    private Vector2 targetPos = new Vector2(0,-2);
    [SerializeField]
    private AudioClip bgm_;

    private float stopTimer_;
    private bool hasReachedTarget_ = false;
    private PlayerMove_Test_Lerp playerMoveTestLerp_;
    private Animator playerAnimator_;


    public GameObject Player;
    public GameObject Enemy;
    

    private void Start()
    {
        playerMoveTestLerp_ = Player.GetComponent<PlayerMove_Test_Lerp>();
        playerAnimator_ = Player.GetComponent<Animator>();
        playerAnimator_.SetFloat("DirY", 1);
        playerMoveTestLerp_.enabled = false;
        playerAnimator_.enabled = false;

        SoundManager.Instance.PlayBGM(bgm_);
    }

    private void Update()
    {
        stopTimer_ += Time.deltaTime;
        if ( stopTimer_ > stopInterval_)
        {
            playerMoveTestLerp_.enabled = true;
            playerAnimator_.enabled = true;

            if (hasReachedTarget_)
            {
                Enemy.gameObject.SetActive(true);
                lunaAnimator_.gameObject.SetActive(false);
                enabled = false;
            }
        }

        float distance = Vector2.Distance(Player.transform.position, targetPos);
        if (Player.transform.position.y == targetPos.y && !hasReachedTarget_)
        {
            stopTimer_ = 0;
            playerMoveTestLerp_.enabled = false;
            playerAnimator_.enabled = false;
            hasReachedTarget_ = true;
            stopInterval_ = 2f;
        }

        // 공포 브금 및 적 등장
        
    }
}
