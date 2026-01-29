using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public abstract class SceneController : MonoBehaviour
{
    [SerializeField]
    protected float stopInterval = 1f;

    [SerializeField]
    protected AudioClip bgm;

    [SerializeField]
    protected GameObject Player;

    protected PlayerMove_Test_Lerp playerMoveTestLerp;
    public Animator playerAnimator;

    public List<CinemachineVirtualCamera> cinemachineCameras;

    protected virtual void Start()
    {
        if (bgm != null)
            SoundManager.Instance.PlayBGM(bgm);
    }
    private void Awake()
    {
        playerMoveTestLerp = Player.GetComponent<PlayerMove_Test_Lerp>();
        playerAnimator = Player.GetComponent<Animator>();
    }
    protected virtual IEnumerator StopPlayer()
    {
        Player.GetComponent<PlayerMove_Test_Lerp>().enabled = false;
        playerAnimator.enabled = false;
        yield return new WaitForSeconds(stopInterval);
        Player.GetComponent<PlayerMove_Test_Lerp>().enabled = true;
        playerAnimator.enabled = true;

        OnStopIntervalReached();
    }

    protected abstract void OnStopIntervalReached();



    public void ChangeCinemachineCamera(CinemachineVirtualCamera target)
    {
        foreach (var cam in cinemachineCameras)
        {
            if (target == cam)
                target.gameObject.SetActive(true);
            else
                cam.gameObject.SetActive(false);
        }
    }
}
