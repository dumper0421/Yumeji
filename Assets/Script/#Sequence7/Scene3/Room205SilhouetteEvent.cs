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

    private bool enteredBathroom = false;
    private bool returnEventTriggered = false;


    private void Awake()
    {
        // 반짝이는 처음에는 보이지 않음
        if (callSheetSparkle != null)
            callSheetSparkle.SetActive(false);


        // 카메라 플래시 이미지들도 처음에는 OFF
        foreach (GameObject flash in cameraFlashVisuals)
        {
            if (flash != null)
                flash.SetActive(false);
        }
    }


    // 화장실에 처음 들어왔을 때 호출
    public void EnterBathroom()
    {
        if (enteredBathroom)
            return;

        enteredBathroom = true;

        Debug.Log("[Room205] 플레이어 화장실 진입");

        PlayCameraFlashSounds();
    }


    // 화장실에서 다시 방으로 돌아왔을 때 호출
    public void ReturnToRoom()
    {
        if (!enteredBathroom)
        {
            Debug.Log("[Room205] 아직 화장실 이벤트가 발생하지 않음");
            return;
        }

        if (returnEventTriggered)
            return;

        returnEventTriggered = true;

        Debug.Log("[Room205] ★ 루나 실루엣 이벤트 시작 ★");

        StartCoroutine(ReturnEventCoroutine());
        StartCoroutine(FadeLunaCoroutine());
    }


    private void PlayCameraFlashSounds()
    {
        if (audioSource == null || cameraFlashClip == null)
        {
            Debug.LogWarning(
                "[Room205] 카메라 플래시 AudioSource 또는 AudioClip이 없음"
            );

            return;
        }

        // 카메라 3대의 플래시 SFX
        for (int i = 0; i < 3; i++)
        {
            audioSource.PlayOneShot(cameraFlashClip);
        }

        Debug.Log("[Room205] 카메라 플래시 SFX × 3");
    }


    private IEnumerator ReturnEventCoroutine()
    {
        // 카메라 3대 플래시 ON
        foreach (GameObject flash in cameraFlashVisuals)
        {
            if (flash != null)
                flash.SetActive(true);
        }

        Debug.Log("[Room205] 카메라 플래시 이미지 ON");


        yield return new WaitForSeconds(flashDuration);


        // 0.5초 후 플래시 OFF
        foreach (GameObject flash in cameraFlashVisuals)
        {
            if (flash != null)
                flash.SetActive(false);
        }

        Debug.Log("[Room205] 카메라 플래시 이미지 OFF");
    }


    private IEnumerator FadeLunaCoroutine()
    {
        if (lunaRenderer == null)
        {
            Debug.LogWarning("[Room205] Luna SpriteRenderer가 없음");
            yield break;
        }


        float timer = 0f;

        Color color = lunaRenderer.color;

        float startAlpha = color.a;


        Debug.Log("[Room205] 루나 실루엣 페이드 시작");


        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeDuration);

            color.a = Mathf.Lerp(startAlpha, 0f, t);

            lunaRenderer.color = color;

            yield return null;
        }


        // 완전히 투명하게 고정
        color.a = 0f;
        lunaRenderer.color = color;


        // 사라진 루나는 더 이상 조사할 수 없음
        if (lunaCollider != null)
            lunaCollider.enabled = false;


        // 콜시트 획득용 반짝이 활성화
        if (callSheetSparkle != null)
            callSheetSparkle.SetActive(true);


        Debug.Log("[Room205] ★ 루나 실루엣 사라짐 / 반짝이 활성화 ★");
    }
}