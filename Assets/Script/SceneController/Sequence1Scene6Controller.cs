using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence1Scene6Controller : SceneController
{
    public GameObject Enemy;
    public DialogueManager DialogueManager;

    [SerializeField]
    private Animator lunaAnimator_;

    [SerializeField]
    private Vector2 targetPos_ = new Vector2(0,-2);

    private bool hasReachedTarget_ = false;

    public CinemachineVirtualCamera Camera;

    private int _boundaryNum = 0;

    private void Start()
    {
        playerAnimator.SetFloat("DirY", 1);
        playerAnimator.enabled = false;
        StartCoroutine(StopPlayer());
    }

    private void Update()
    {
        if (Player.transform.position.y == targetPos_.y && !hasReachedTarget_)
        {
            playerMoveTestLerp.enabled = false;
            playerAnimator.enabled = false;
            StartCoroutine(StopPlayer());
            hasReachedTarget_ = true;
        }

        if (Player.transform.position.x <= -110.2f)
        {
            Player.GetComponent<SpriteRenderer>().color = new Color(217f / 255f, 187f/255f, 187f/255f,255f/255f);
        }
    }

    protected override void OnStopIntervalReached()
    {
       // playerAnimator.enabled = true;
        lunaAnimator_.gameObject.SetActive(false);

        if (hasReachedTarget_)
        {
            Enemy.gameObject.SetActive(true);
            DialogueManager.StartDialogue("CrewMember");
        }
    }
}

