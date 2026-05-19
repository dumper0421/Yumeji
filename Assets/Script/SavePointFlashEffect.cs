using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePointFlashEffect : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource_;
    [SerializeField] private AudioClip shutterClip_;

    [Header("Targets")]
    [SerializeField] private SpriteRenderer savePointRenderer_;
    [SerializeField] private SpriteRenderer playerRenderer_;
    [SerializeField] private SpriteRenderer companionRenderer_;

    [Header("Flash Settings")]
    [SerializeField] private float flashAlpha_ = 0.9f;
    [SerializeField] private float flashDuration_ = 0.12f;
    [SerializeField] private int sortingOrderOffset_ = 5;
    [SerializeField] private float scaleMultiplier_ = 1.12f;

    private readonly List<SpriteRenderer> sourceRenderers_ = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> overlayRenderers_ = new List<SpriteRenderer>();

    public IEnumerator PlayEffect()
    {
        SetupTargets();

        if (audioSource_ != null && shutterClip_ != null)
            audioSource_.PlayOneShot(shutterClip_);

        EnableOverlay(true);

        float elapsed = 0f;
        while (elapsed < flashDuration_)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration_);

            SyncOverlaySprites();

            float alpha = Mathf.Lerp(flashAlpha_, 0f, t);
            for (int i = 0; i < overlayRenderers_.Count; i++)
            {
                if (overlayRenderers_[i] == null) continue;
                overlayRenderers_[i].color = new Color(1f, 1f, 1f, alpha);
            }

            yield return null;
        }

        EnableOverlay(false);
    }

    private void SetupTargets()
    {
        sourceRenderers_.Clear();

        if (savePointRenderer_ != null)
            sourceRenderers_.Add(savePointRenderer_);

        if (playerRenderer_ != null)
            sourceRenderers_.Add(playerRenderer_);

        if (companionRenderer_ != null)
            sourceRenderers_.Add(companionRenderer_);

        while (overlayRenderers_.Count < sourceRenderers_.Count)
        {
            SpriteRenderer source = sourceRenderers_[overlayRenderers_.Count];
            overlayRenderers_.Add(CreateOverlayRenderer(source, overlayRenderers_.Count));
        }
    }

    private SpriteRenderer CreateOverlayRenderer(SpriteRenderer source, int index)
    {
        GameObject overlayObj = new GameObject($"FlashOverlay_{index}");
        overlayObj.transform.SetParent(source.transform, false);
        overlayObj.transform.localPosition = Vector3.zero;
        overlayObj.transform.localRotation = Quaternion.identity;
        overlayObj.transform.localScale = Vector3.one;

        SpriteRenderer sr = overlayObj.AddComponent<SpriteRenderer>();
        sr.enabled = false;
        sr.color = new Color(1f, 1f, 1f, 0f);

        return sr;
    }

    private void EnableOverlay(bool enable)
    {
        if (enable)
        {
            SyncOverlaySprites();

            for (int i = 0; i < overlayRenderers_.Count; i++)
            {
                if (i >= sourceRenderers_.Count) break;
                if (overlayRenderers_[i] == null) continue;

                overlayRenderers_[i].enabled = true;
            }
        }
        else
        {
            for (int i = 0; i < overlayRenderers_.Count; i++)
            {
                if (overlayRenderers_[i] == null) continue;
                overlayRenderers_[i].enabled = false;
                overlayRenderers_[i].color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private void SyncOverlaySprites()
    {
        for (int i = 0; i < sourceRenderers_.Count; i++)
        {
            SpriteRenderer source = sourceRenderers_[i];
            SpriteRenderer overlay = overlayRenderers_[i];

            if (source == null || overlay == null) continue;

            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            overlay.drawMode = source.drawMode;
            overlay.size = source.size;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + sortingOrderOffset_;

            // 핵심: 원본보다 살짝 크게 만들어 가장자리에서 빛나는 느낌 주기
            overlay.transform.position = source.transform.position;
            overlay.transform.rotation = source.transform.rotation;
            overlay.transform.localScale = Vector3.one * scaleMultiplier_;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (savePointRenderer_ == null)
            savePointRenderer_ = GetComponent<SpriteRenderer>();
    }
#endif
}