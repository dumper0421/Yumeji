using UnityEngine;
using UnityEngine.UI;

public class Sequence4Scene2ScriptPagePreviewController : MonoBehaviour
{
    [Header("Inventory")]
    public InventoryManager inventoryManager;

    [Header("Preview UI")]
    public GameObject previewPanel;
    public Image previewImage;
    public Sprite scriptPageSprite;

    [Header("Target")]
    public string targetItemName = "각본의 한 페이지";

    private bool isPreviewOpen = false;

    private void Start()
    {
        if (previewPanel != null)
            previewPanel.SetActive(false);
    }

    private void Update()
    {
        if (inventoryManager == null) return;

        // 인벤토리가 닫혀 있으면 미리보기 강제 종료
        if (!IsInventoryOpen())
        {
            if (isPreviewOpen)
                ClosePreview();
            return;
        }

        // Space 키만 사용
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        // 이미 열려 있으면 닫기
        if (isPreviewOpen)
        {
            ClosePreview();
            return;
        }

        // 각본의 한 페이지 선택 중일 때만 열기
        if (IsSelectedTargetItem())
        {
            OpenPreview();
        }
    }

    private bool IsInventoryOpen()
    {
        return inventoryManager.BackGround != null
            && inventoryManager.BackGround.activeSelf;
    }

    private bool IsSelectedTargetItem()
    {
        if (inventoryManager._slots == null || inventoryManager._slots.Count == 0)
            return false;

        int index = inventoryManager.CurrentSelectIndex;
        if (index < 0 || index >= inventoryManager._slots.Count)
            return false;

        ItemSlot currentSlot = inventoryManager._slots[index];
        if (currentSlot == null || currentSlot.IsEmpty)
            return false;

        ItemData itemData = currentSlot.GetItemData();
        if (itemData == null) return false;

        return itemData.ItemName == targetItemName;
    }

    private void OpenPreview()
    {
        if (previewPanel == null || previewImage == null || scriptPageSprite == null)
            return;

        previewImage.sprite = scriptPageSprite;
        previewPanel.SetActive(true);
        isPreviewOpen = true;
    }

    private void ClosePreview()
    {
        if (previewPanel != null)
            previewPanel.SetActive(false);

        isPreviewOpen = false;
    }
}