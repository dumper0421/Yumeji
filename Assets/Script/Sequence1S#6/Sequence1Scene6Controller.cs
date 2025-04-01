using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene6Controller : SceneController
{
    public GameObject Enemy;

    [SerializeField]
    private Animator lunaAnimator_;

    [SerializeField]
    private Vector2 targetPos_ = new Vector2(0,-2);

    private bool hasReachedTarget_ = false;

    private void Start()
    {
        playerAnimator.SetFloat("DirY", 1);
        playerAnimator.enabled = false;
        SoundManager.Instance.PlayBGM(bgm_);
        StartCoroutine(StopPlayer());
    }

    private void Update()
    {
        Debug.Log(Player.transform.position.y + " " + targetPos_.y + " " +hasReachedTarget_);
        if (Player.transform.position.y == targetPos_.y && !hasReachedTarget_)
        {
            playerMoveTestLerp.enabled = false;
            playerAnimator.enabled = false;
            StartCoroutine(StopPlayer());
            hasReachedTarget_ = true;
        }
    }

    protected override void OnStopIntervalReached()
    {
        playerAnimator.enabled = true;
        lunaAnimator_.gameObject.SetActive(false);

        if (hasReachedTarget_)
        {
            Enemy.gameObject.SetActive(true);
        }

    }

}

