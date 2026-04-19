using UnityEngine;

public class PuzzleLightController : MonoBehaviour
{
    [Header("Basic")]
    public bool IsActivated = false;
    public int CurrentIndex = 0;

    [Header("Spawn")]
    public GameObject LightRootPrefab;
    public Transform SpawnPoint;

    [Header("Runtime")]
    public GameObject SpawnedLightRoot;

    [Header("Light Variants")]
    [Tooltip("생성된 LightRoot 아래에 들어있는 색상 오브젝트들 순서대로 넣기")]
    public GameObject[] LightVariants;

    [Header("Solve")]
    [Tooltip("이 인덱스일 때 정답으로 판정")]
    public int SolvedIndex = 0;

    public bool IsSolved => IsActivated && CurrentIndex == SolvedIndex;

    public void ActivateLight()
    {
        if (IsActivated) return;
        if (LightRootPrefab == null || SpawnPoint == null) return;

        SpawnedLightRoot = Instantiate(LightRootPrefab, SpawnPoint.position, SpawnPoint.rotation);

        IsActivated = true;
        CurrentIndex = 0;

        CacheVariantsFromSpawnedRoot();
        ApplyCurrentVariant();
    }

    public void CycleLight()
    {
        if (!IsActivated) return;
        if (LightVariants == null || LightVariants.Length == 0) return;

        CurrentIndex++;
        if (CurrentIndex >= LightVariants.Length)
            CurrentIndex = 0;

        ApplyCurrentVariant();
    }

    public void ActivateOrCycle()
    {
        if (!IsActivated)
            ActivateLight();
        else
            CycleLight();
    }

    private void CacheVariantsFromSpawnedRoot()
    {
        if (SpawnedLightRoot == null) return;

        // 이미 인스펙터에서 직접 넣어놨으면 그대로 사용
        if (LightVariants != null && LightVariants.Length > 0)
            return;

        // 자식들을 자동 수집
        int childCount = SpawnedLightRoot.transform.childCount;
        LightVariants = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            LightVariants[i] = SpawnedLightRoot.transform.GetChild(i).gameObject;
        }
    }

    public void ApplyCurrentVariant()
    {
        if (LightVariants == null || LightVariants.Length == 0) return;

        for (int i = 0; i < LightVariants.Length; i++)
        {
            if (LightVariants[i] != null)
                LightVariants[i].SetActive(i == CurrentIndex);
        }
    }
}