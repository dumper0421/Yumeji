using UnityEngine;

public class ChangeSprite : MonoBehaviour, IFlashable
{
    [Header("Sprite Change Settings")]
    [SerializeField] private Sprite takenSprite;  // 촬영 후 바꿀 스프라이트

    private SpriteRenderer spriteRenderer;
    private bool hasTaken = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnPhotoTaken(bool isEnhanced)
    {
        if (hasTaken) return;      // 이미 촬영된 상태면 무시
        hasTaken = true;

        // 스프라이트 영구 교체
        spriteRenderer.sprite = takenSprite;
    }
}