using System.Collections;
using UnityEngine;

public class Room204MirrorEvent : MonoBehaviour
{
    [Header("Mirror Sprites")]
    [SerializeField] private SpriteRenderer beforeRenderer; // 실루엣 있는 세면대
    [SerializeField] private SpriteRenderer afterRenderer;  // 실루엣 없는 세면대

    [Header("Trigger Settings")]
    [SerializeField] private float stayTime = 2f;
    [SerializeField] private float fadeTime = 3f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip screamClip;

    private bool playerInside = false;
    private bool triggered = false;

    private Coroutine stayCoroutine;


    private void Awake()
    {
        // 실루엣 있는 이미지
        if (beforeRenderer != null)
        {
            Color color = beforeRenderer.color;
            color.a = 1f;
            beforeRenderer.color = color;

            beforeRenderer.gameObject.SetActive(true);
        }

        // 실루엣 없는 이미지
        if (afterRenderer != null)
        {
            Color color = afterRenderer.color;
            color.a = 0f;
            afterRenderer.color = color;

            afterRenderer.gameObject.SetActive(true);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("[Room204 Mirror] 플레이어가 거울 앞 Trigger에 진입했습니다.");

        // 이미 이벤트가 끝났다면 다시 실행하지 않음
        if (triggered)
        {
            Debug.Log("[Room204 Mirror] 이미 완료된 이벤트입니다.");
            return;
        }

        playerInside = true;

        if (stayCoroutine == null)
        {
            Debug.Log(
                "[Room204 Mirror] " +
                stayTime +
                "초 대기를 시작합니다."
            );

            stayCoroutine = StartCoroutine(WaitForPlayer());
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("[Room204 Mirror] 플레이어가 거울 앞 Trigger에서 나갔습니다.");

        playerInside = false;

        // 이미 이벤트가 실행된 뒤라면 취소할 필요 없음
        if (triggered)
            return;

        if (stayCoroutine != null)
        {
            StopCoroutine(stayCoroutine);
            stayCoroutine = null;

            Debug.Log(
                "[Room204 Mirror] 2초를 채우지 못해 이벤트가 취소되었습니다."
            );
        }
    }


    private IEnumerator WaitForPlayer()
    {
        float timer = 0f;

        while (timer < stayTime)
        {
            if (!playerInside)
            {
                stayCoroutine = null;
                yield break;
            }

            timer += Time.deltaTime;

            yield return null;
        }

        stayCoroutine = null;

        TriggerMirrorEvent();
    }


    private void TriggerMirrorEvent()
    {
        if (triggered)
            return;

        triggered = true;

        Debug.Log(
            "[Room204 Mirror] ★ 거울 이벤트 발생! ★"
        );


        // 비명 SFX
        if (audioSource != null && screamClip != null)
        {
            audioSource.PlayOneShot(screamClip);

            Debug.Log(
                "[Room204 Mirror] 여자 비명 SFX 재생"
            );
        }
        else
        {
            Debug.LogWarning(
                "[Room204 Mirror] AudioSource 또는 ScreamClip이 연결되지 않았습니다."
            );
        }


        // 실루엣 사라지는 연출
        StartCoroutine(FadeMirror());
    }


    private IEnumerator FadeMirror()
    {
        Debug.Log(
            "[Room204 Mirror] 실루엣 페이드 시작"
        );

        float timer = 0f;

        Color beforeColor = Color.white;
        Color afterColor = Color.white;


        if (beforeRenderer != null)
            beforeColor = beforeRenderer.color;

        if (afterRenderer != null)
            afterColor = afterRenderer.color;


        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeTime);


            // 실루엣 있는 세면대
            // Alpha 1 → 0
            if (beforeRenderer != null)
            {
                beforeColor.a = Mathf.Lerp(1f, 0f, t);

                beforeRenderer.color = beforeColor;
            }


            // 일반 세면대
            // Alpha 0 → 1
            if (afterRenderer != null)
            {
                afterColor.a = Mathf.Lerp(0f, 1f, t);

                afterRenderer.color = afterColor;
            }


            yield return null;
        }


        // 최종 상태 확실하게 고정
        if (beforeRenderer != null)
        {
            beforeColor.a = 0f;

            beforeRenderer.color = beforeColor;
            beforeRenderer.gameObject.SetActive(false);
        }


        if (afterRenderer != null)
        {
            afterColor.a = 1f;

            afterRenderer.color = afterColor;
        }


        Debug.Log(
            "[Room204 Mirror] ★ 거울 이벤트 완료 ★"
        );
    }
}