using System.Collections;
using UnityEngine;

public class Room205SilhouetteEvent : MonoBehaviour
{
    [Header("Luna Silhouette")]
    [SerializeField] private SpriteRenderer lunaRenderer;
    [SerializeField] private Collider2D lunaCollider;

    [Header("Camera Flash Visuals")]
    [SerializeField] private GameObject[] cameraFlashVisuals;

    [Header("Call Sheet Sparkle")]
    [SerializeField] private GameObject callSheetSparkle;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cameraFlashClip;

    [Header("Timing")]
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private float fadeDuration = 2f;

    // 화장실에 들어갔는지
    private bool enteredBathroom = false;

    // 방 복귀 이벤트가 이미 실행됐는지
    private bool returnEventTriggered = false;


    private void Awake()
    {
        // 콜시트 반짝이는 처음에는 숨김
        if (callSheetSparkle != null)
        {
            callSheetSparkle.SetActive(false);
        }

        // 카메라 플래시 이미지들도 처음에는 숨김
        foreach (GameObject flash in cameraFlashVisuals)
        {
            if (flash != null)
            {
                flash.SetActive(false);
            }
        }
    }


    // 화장실 진입 Trigger에서 호출
    public void EnterBathroom()
    {
        // 최초 1회만
        if (enteredBathroom)
            return;

        enteredBathroom = true;

        Debug.Log("[Room205] 화장실 진입");
        Debug.Log("[Room205] 카메라 플래시 SFX 3회 동시 재생");

        PlayCameraFlashSounds();
    }


    // 화장실에서 방으로 복귀하는 Trigger에서 호출
    public void ReturnToRoom()
    {
        // 화장실에 들어간 적이 없으면 실행하지 않음
        if (!enteredBathroom)
        {
            Debug.Log("[Room205] 화장실에 진입하지 않았으므로 복귀 이벤트 실행 안 함");
            return;
        }

        // 이미 발생한 이벤트면 재실행하지 않음
        if (returnEventTriggered)
            return;

        returnEventTriggered = true;

        Debug.Log("[Room205] ★ 방 복귀 이벤트 시작 ★");

        // 카메라 플래시 시각 효과
        StartCoroutine(CameraFlashVisualCoroutine());

        // 루나 실루엣 사라짐
        StartCoroutine(FadeLunaCoroutine());
    }


    // 화장실 진입 시 플래시 SFX 3개를 동시에 재생
    private void PlayCameraFlashSounds()
    {
        if (audioSource == null || cameraFlashClip == null)
        {
            Debug.LogWarning(
                "[Room205] AudioSource 또는 CameraFlashClip이 연결되지 않았습니다."
            );

            return;
        }

        // 시간차 없이 같은 순간에 3회 재생
        audioSource.PlayOneShot(cameraFlashClip);
        audioSource.PlayOneShot(cameraFlashClip);
        audioSource.PlayOneShot(cameraFlashClip);
    }


    // 방으로 돌아왔을 때 카메라 3대가 0.5초간 번쩍임
    private IEnumerator CameraFlashVisualCoroutine()
    {
        Debug.Log("[Room205] 카메라 플래시 이미지 3개 ON");

        foreach (GameObject flash in cameraFlashVisuals)
        {
            if (flash != null)
            {
                flash.SetActive(true);
            }
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (GameObject flash in cameraFlashVisuals)
        {
            if (flash != null)
            {
                flash.SetActive(false);
            }
        }

        Debug.Log("[Room205] 카메라 플래시 이미지 3개 OFF");
    }


    // 방으로 돌아왔을 때 루나가 2초간 서서히 사라짐
    private IEnumerator FadeLunaCoroutine()
    {
        if (lunaRenderer == null)
        {
            Debug.LogWarning(
                "[Room205] Luna Renderer가 연결되지 않았습니다."
            );

            yield break;
        }

        Debug.Log("[Room205] 루나 실루엣 Fade 시작");

        float timer = 0f;

        Color color = lunaRenderer.color;
        float startAlpha = color.a;


        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / fadeDuration
            );

            color.a = Mathf.Lerp(
                startAlpha,
                0f,
                t
            );

            lunaRenderer.color = color;

            yield return null;
        }


        // 완전히 투명하게 고정
        color.a = 0f;
        lunaRenderer.color = color;


        // 루나에게 더 이상 상호작용할 수 없도록 함
        if (lunaCollider != null)
        {
            lunaCollider.enabled = false;
        }


        // 루나가 있던 자리에 반짝이 활성화
        if (callSheetSparkle != null)
        {
            callSheetSparkle.SetActive(true);
        }


        Debug.Log(
            "[Room205] ★ 루나 실루엣 사라짐 / 콜시트 반짝이 활성화 ★"
        );
    }
}