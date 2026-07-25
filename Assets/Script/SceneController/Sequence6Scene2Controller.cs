using UnityEngine;

public class Sequence6Scene2Controller : SceneController
{
    [Header("Dialogue")]
    [SerializeField]
    private Sequence6Scene2DialogueController dialogueController;

    [Header("Seated Characters")]
    [SerializeField]
    private GameObject seatedHaruObject;

    [SerializeField]
    private GameObject seatedRayObject;

    [Header("Gameplay Characters")]
    [SerializeField]
    private GameObject playerObject;

    [SerializeField]
    private GameObject rayCompanionObject;

    private bool introStarted;
    private bool introFinished;

    protected new void Start()
    {
        base.Start();

        StartIntroSequence();
    }

    private void StartIntroSequence()
    {
        if (introStarted)
            return;

        introStarted = true;

        seatedHaruObject.SetActive(true);
        seatedRayObject.SetActive(true);

        playerObject.SetActive(false);
        rayCompanionObject.SetActive(false);

        dialogueController.OnStartedDialogue("S6S2_Intro_Ray1");
    }

    public void FinishIntroSequence()
    {
        if (introFinished)
            return;

        introFinished = true;

        seatedHaruObject.SetActive(false);
        seatedRayObject.SetActive(false);

        playerObject.SetActive(true);
        rayCompanionObject.SetActive(true);
    }

    protected override void OnStopIntervalReached()
    {
    }
}