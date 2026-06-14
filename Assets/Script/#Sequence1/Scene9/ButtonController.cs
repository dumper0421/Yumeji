using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(AudioSource))]
public class ButtonController : MonoBehaviour
{
    [Header("Button Graphics")]
    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Activator Object")]
    [SerializeField] private GameObject activatorObject;

    [Header("Object to Remove")]
    [SerializeField] private GameObject objectToRemove;

    [Header("Sound Effect")]
    [SerializeField] private AudioClip pressSfx;
    private AudioSource audioSource;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private string doorOpenedDialogueId = "button_door_opened";

    [Header("Script to Disable on Activator")]
    [SerializeField] private PushableObject pushableObject;

    [Header("삭제딜레이시간")]
    [SerializeField] private float disableDelay = 0.5f;

    [Header("Scene9Controller 참조")]
    [SerializeField] private Sequence1Scene9Controller scene9Controller;

    private bool isPressed = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (activatorObject != null)
        {
            pushableObject = activatorObject.GetComponent<PushableObject>();

            if (pushableObject == null)
                Debug.LogError($"[ButtonController] '{activatorObject.name}'에 PushableObject 컴포넌트가 없습니다!");
        }
    }

    private bool IsActivator(Collider2D other)
    {
        if (activatorObject == null) return false;

        return other.gameObject == activatorObject
            || other.transform.IsChildOf(activatorObject.transform);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isPressed && IsActivator(other))
        {
            isPressed = true;
            StartCoroutine(PressButtonRoutine());
        }
    }

    private IEnumerator PressButtonRoutine()
    {
        // 1) 버튼 스프라이트를 눌린 상태로 교체
        if (buttonRenderer != null && pressedSprite != null)
            buttonRenderer.sprite = pressedSprite;

        // 2) 효과음 재생
        if (pressSfx != null)
        {
            audioSource.PlayOneShot(pressSfx);

            // 효과음이 끝난 뒤 대사 출력
            yield return new WaitForSeconds(pressSfx.length);
        }

        // 3) 문 열림 대사 출력
        if (dialogueManager != null && !string.IsNullOrEmpty(doorOpenedDialogueId))
        {
            dialogueManager.StartDialogue(doorOpenedDialogueId);
        }

        // 4) 지정된 오브젝트 제거
        if (objectToRemove != null)
            Destroy(objectToRemove);

        // 5) Scene9Controller 에 BGM 교체 요청
        if (scene9Controller != null)
            scene9Controller.ChangeBGMToButtonClip();

        // 6) 밀기 해제하고 위치 고정
        if (pushableObject != null)
            StartCoroutine(DestroyPushableAfterDelay());
    }

    private IEnumerator DestroyPushableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);

        if (pushableObject != null)
            Destroy(pushableObject);
    }
}