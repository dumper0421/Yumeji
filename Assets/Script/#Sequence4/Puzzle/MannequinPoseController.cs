using UnityEngine;

public class MannequinPoseController : MonoBehaviour
{
    [Header("Pose Visuals")]
    [Tooltip("p0, p1, p2, p3 순서대로 넣기")]
    public GameObject[] poseObjects;

    [SerializeField] private int poseIndex = 0;

    public int PoseIndex => poseIndex;
    public bool IsSolved => poseIndex == 3;

    private void Start()
    {
        ApplyPose();
    }

    public void NextPose()
    {
        if (poseObjects == null || poseObjects.Length == 0) return;

        poseIndex++;
        if (poseIndex >= poseObjects.Length)
            poseIndex = 0;

        ApplyPose();
    }

    public void ApplyPose()
    {
        if (poseObjects == null || poseObjects.Length == 0) return;

        for (int i = 0; i < poseObjects.Length; i++)
        {
            if (poseObjects[i] != null)
                poseObjects[i].SetActive(i == poseIndex);
        }
    }
}