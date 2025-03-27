using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SceneController : MonoBehaviour
{
    [SerializeField]
    protected float stopInterval = 1f;

    [SerializeField]
    protected AudioClip bgm_;

    [SerializeField]
    protected GameObject Player;

    protected PlayerMove_Test_Lerp playerMoveTestLerp;
    protected Animator playerAnimator;
    private void Awake()
    {
        playerMoveTestLerp = Player.GetComponent<PlayerMove_Test_Lerp>();
        playerAnimator = Player.GetComponent<Animator>();
    }
    protected virtual IEnumerator StopPlayer()
    {
        Player.GetComponent<PlayerMove_Test_Lerp>().enabled = false;
        yield return new WaitForSeconds(stopInterval);
        Player.GetComponent<PlayerMove_Test_Lerp>().enabled = true;

        OnStopIntervalReached();
    }

    protected abstract void OnStopIntervalReached();

}
