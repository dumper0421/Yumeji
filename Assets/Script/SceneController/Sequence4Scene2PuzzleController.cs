using UnityEngine;

public class Sequence4Scene2SceneController : MonoBehaviour
{
    [Header("Table Puzzle")]
    public bool IsTablePuzzleSolved = false;

    [Header("Light Puzzle")]
    public PuzzleLightController Light1;
    public PuzzleLightController Light2;
    [SerializeField] private bool IsLightlPuzzlesSolvedDebug;

    [Header("Mannequin Puzzle")]
    public bool IsMannequinPuzzleSolved = false;

    [Header("All Puzzle")]
    [SerializeField] private bool areAllPuzzlesSolvedDebug;

    public bool IsLightPuzzleSolved
    {
        get
        {
            if (Light1 == null || Light2 == null)
                return false;

            return Light1.IsSolved && Light2.IsSolved;
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
        IsLightlPuzzlesSolvedDebug = IsLightPuzzleSolved;
        areAllPuzzlesSolvedDebug = AreAllPuzzlesSolved;
    }
}