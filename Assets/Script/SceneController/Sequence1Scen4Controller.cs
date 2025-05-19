using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Sequence1Scen4Controller : SceneController
{

    [SerializeField]
    private Vector2 targetPos_ = new Vector2(0, -8);

    [SerializeField]
    private Vector2 targetPos2_ = new Vector2(0, -5);

    [SerializeField]
    private PlayableDirector director_;

    [SerializeField]
    private AudioClip SFX_;

    private bool hasReachedTarget_ = false;
    private bool playedSFX = false;


    // Update is called once per frame
    private void Start()
    {
        base.Start();
        director_.Stop();
    }
    void Update()
    {
        Debug.Log(Player.transform.position.y);
        if (Player.transform.position.y >= targetPos_.y && !hasReachedTarget_)
        {
            StartCoroutine(StopPlayer());
            hasReachedTarget_ = true;
        }
        if (Player.transform.position.y >= targetPos2_.y && !playedSFX)
        {
            SoundManager.Instance.PlaySFX(SFX_);
            playedSFX = true;
        }

    }

    protected override IEnumerator StopPlayer()
    {
        playerMoveTestLerp.enabled = false;
        playerAnimator.enabled = false;
        yield return new WaitForSeconds(stopInterval);
        OnStopIntervalReached();
    }

    protected override void OnStopIntervalReached()
    {
        playerMoveTestLerp.enabled = true;
        playerAnimator.enabled = true;

        if (stopInterval == 2.5f)
        {
            director_.Play();
            stopInterval = 1;
            StartCoroutine(StopPlayer());
        }
    }
}
