using UnityEngine;

public class Sequence4Scene2SceneController : MonoBehaviour
{
    [Header("Table Puzzle")]
    public bool IsTablePuzzleSolved = false;

    [Header("Light Puzzle")]
    public PuzzleLightController Light1;
    public PuzzleLightController Light2;
    [SerializeField] private bool IsLightPuzzleSolvedDebug;

    [Header("Mannequin Puzzle")]
    public bool IsMannequin1Solved = false;
    public MannequinPoseController Mannequin2Controller;
    [SerializeField] private bool IsMannequin2SolvedDebug;

    [Header("All Puzzle")]
    [SerializeField] private bool AreAllPuzzlesSolvedDebug;

    public bool IsLightPuzzleSolved
    {
        get
        {
            if (Light1 == null || Light2 == null)
                return false;

            return Light1.IsSolved && Light2.IsSolved;
        }
    }

    public bool IsMannequin2Solved
    {
        get
        {
            if (Mannequin2Controller == null)
                return false;

            return Mannequin2Controller.IsSolved;
        }
    }

    public bool IsMannequinPuzzleSolved
    {
        get
        {
            return IsMannequin1Solved && IsMannequin2Solved;
        }
    }

    public bool AreAllPuzzlesSolved
    {
        get
        {
            return IsTablePuzzleSolved
                && IsLightPuzzleSolved
                && IsMannequinPuzzleSolved;
        }
    }

    private void Update()
    {
        IsLightPuzzleSolvedDebug = IsLightPuzzleSolved;
        IsMannequin2SolvedDebug = IsMannequin2Solved;
        AreAllPuzzlesSolvedDebug = AreAllPuzzlesSolved;
    }
}