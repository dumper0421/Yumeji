
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 도플갱어가 기존 IFlashable 촬영 이벤트를 받아 위치를 교환한다.
/// 이 스크립트는 Doppelganger_Base에 붙이고,
/// 촬영 판정용 Collider2D와 같은 GameObject에 둔다.
/// </summary>
[DisallowMultipleComponent]
public class DoppelgangerPositionSwap : MonoBehaviour, IFlashable
{
    [Header("Player Reference")]
    [SerializeField] private PlayerMove_Test_Lerp player;

    [Header("Position Swap Timing")]
    [SerializeField, Min(0f)] private float blinkDuration = 0.5f;
    [SerializeField, Min(0.02f)] private float blinkInterval = 0.08f;
    [SerializeField, Min(0f)] private float resumeDelay = 0.2f;

    [Header("External Movement Lock")]
    [Tooltip("컷신/가구 속박 등으로 도플갱어가 이동할 수 없으면 체크")]
    [SerializeField] private bool externallyMovementLocked;

    [Header("Optional Movement Controller Events")]
    [Tooltip("교환 직전에 도플갱어의 이동 코루틴 또는 추격 AI를 정지")]
    [SerializeField] private UnityEvent onSwapStarted;

    [Tooltip("플레이어보다 0.2초 늦게 AI 경로 재계산과 이동 재개")]
    [SerializeField] private UnityEvent onSwapResumed;

    private bool swapping;
    private PlayerMove_Test_Lerp swappingPlayer;

    private SpriteRenderer[] playerSprites;
    private SpriteRenderer[] doppelSprites;

    private bool[] playerOriginalEnabled;
    private bool[] doppelOriginalEnabled;

    public bool IsSwapping => swapping;
    public bool IsMovementLocked => externallyMovementLocked || swapping;

    public void SetMovementLocked(bool locked)
    {
        externallyMovementLocked = locked;
    }

    // PlayerMove_Test_Lerp의 기존 IFlashable 호출을 통해 실행된다.
    public void OnPhotoTaken(bool unused)
    {
        if (!isActiveAndEnabled || IsMovementLocked)
            return;

        if (player == null)
            player = FindObjectOfType<PlayerMove_Test_Lerp>();

        if (player == null || !player.TryBeginPositionSwap())
            return;

        swapping = true;
        swappingPlayer = player;

        StartCoroutine(SwapCoroutine(player));
    }

    private IEnumerator SwapCoroutine(PlayerMove_Test_Lerp targetPlayer)
    {
        // 향후 Normal/Chase 이동 제어 스크립트가 이벤트에 연결된다.
        onSwapStarted?.Invoke();

        Vector3 oldPlayerPos = targetPlayer.transform.position;
        Vector3 oldDoppelPos = transform.position;

        // 위치의 X/Y만 교환하고 Z값, 바라보는 방향은 각각 유지한다.
        targetPlayer.transform.position = new Vector3(
            oldDoppelPos.x,
            oldDoppelPos.y,
            oldPlayerPos.z
        );

        transform.position = new Vector3(
            oldPlayerPos.x,
            oldPlayerPos.y,
            oldDoppelPos.z
        );

        Physics2D.SyncTransforms();

        playerSprites = targetPlayer.GetComponentsInChildren<SpriteRenderer>(true);
        doppelSprites = GetComponentsInChildren<SpriteRenderer>(true);

        playerOriginalEnabled = CaptureEnabled(playerSprites);
        doppelOriginalEnabled = CaptureEnabled(doppelSprites);

        float elapsed = 0f;

        while (elapsed < blinkDuration)
        {
            bool visible =
                Mathf.FloorToInt(
                    elapsed / Mathf.Max(0.02f, blinkInterval)
                ) % 2 == 0;

            SetBlinkVisible(
                playerSprites,
                playerOriginalEnabled,
                visible
            );

            SetBlinkVisible(
                doppelSprites,
                doppelOriginalEnabled,
                visible
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreSprites();

        // 위치 교환으로부터 0.5초 뒤 플레이어 이동 복구.
        targetPlayer.EndPositionSwap();
        swappingPlayer = null;

        // 도플갱어는 플레이어보다 0.2초 늦게 이동을 재개한다.
        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        swapping = false;

        onSwapResumed?.Invoke();
    }

    private static bool[] CaptureEnabled(SpriteRenderer[] renderers)
    {
        bool[] values = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            values[i] =
                renderers[i] != null && renderers[i].enabled;
        }

        return values;
    }

    private static void SetBlinkVisible(
        SpriteRenderer[] renderers,
        bool[] originals,
        bool visible)
    {
        if (renderers == null || originals == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = originals[i] && visible;
            }
        }
    }

    private void RestoreSprites()
    {
        SetBlinkVisible(
            playerSprites,
            playerOriginalEnabled,
            true
        );

        SetBlinkVisible(
            doppelSprites,
            doppelOriginalEnabled,
            true
        );
    }

    private void OnDisable()
    {
        if (!swapping)
            return;

        // 씬 전환 중 오브젝트가 꺼져도 플레이어 잠금이 남지 않도록 한다.
        RestoreSprites();

        if (swappingPlayer != null)
            swappingPlayer.EndPositionSwap();

        swappingPlayer = null;
        swapping = false;
    }
}
