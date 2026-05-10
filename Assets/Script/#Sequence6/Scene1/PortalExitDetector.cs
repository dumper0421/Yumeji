using System.Collections;
using UnityEngine;

public class PortalExitDetector : MonoBehaviour
{
    [Header("Portal")]
    public GameObject portalObject;
    public Animator portalAnimator;
    public AudioSource portalAudioSource;
    public AudioClip portalDestroySfx;

    [Header("Animation State Names")]
    public string portalDestroyStateName = "Portal_Destroy";

    [Header("Timing")]
    public float portalFrameRate = 12f;
    public int destroyFrameCount = 3;

    [Header("Option")]
    public bool disableTriggerAfterUse = true;

    private bool isUsed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isUsed) return;

        if (other.CompareTag("Player"))
        {
            StartCoroutine(DestroyPortal());
        }
    }

    private IEnumerator DestroyPortal()
    {
        isUsed = true;

        if (portalAudioSource != null && portalDestroySfx != null)
            portalAudioSource.PlayOneShot(portalDestroySfx);

        if (portalAnimator != null)
            portalAnimator.Play(portalDestroyStateName, 0, 0f);

        float destroyDuration = destroyFrameCount / portalFrameRate;
        yield return new WaitForSeconds(destroyDuration);

        if (portalObject != null)
            portalObject.SetActive(false);

        if (disableTriggerAfterUse)
            gameObject.SetActive(false);
    }
}