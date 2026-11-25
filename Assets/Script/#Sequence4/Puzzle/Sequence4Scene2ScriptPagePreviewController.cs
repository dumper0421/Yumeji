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
    private bool wasInventoryOpen = false;

    private void Start()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        // 미리보기 UI가 연결되지 않은 씬(예: 4-2 퍼즐 해결 후 씬)에서는 4-2 퍼즐 씬과 같은 모양으로 직접 만든다
        if ((previewPanel == null || previewImage == null) && inventoryManager != null)
            CreatePreviewUI(inventoryManager.transform);

        if (previewPanel != null)
            previewPanel.SetActive(false);
    }

    /// <summary>4-2 퍼즐 씬의 Previewpanel과 같은 구성: 화면 전체 반투명 흰 판 + 가운데 700x900 이미지.</summary>
    private void CreatePreviewUI(Transform canvasRoot)
    {
        var panel = new GameObject("Previewpanel", typeof(RectTransform), typeof(Image));
        var panelRect = (RectTransform)panel.transform;
        panelRect.SetParent(canvasRoot, false);
        panelRect.SetAsLastSibling();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.392f);

        var image = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
        var imageRect = (RectTransform)image.transform;
        imageRect.SetParent(panelRect, false);
        imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(700f, 900f);

        previewPanel = panel;
        previewImage = image.GetComponent<Image>();
        previewImage.preserveAspect = true;
    }

    private void Update()
    {
        if (inventoryManager == null) return;

        bool isOpenNow = IsInventoryOpen();

        // 인벤토리가 닫혀 있으면 미리보기 강제 종료
        if (!isOpenNow)
        {
            if (isPreviewOpen)
                ClosePreview();
            wasInventoryOpen = false;
            return;
        }

        // 인벤토리를 여는 데 쓴 키 입력(Enter/Space)이 같은 프레임에 그대로
        // 남아있어 미리보기까지 열어버리는 것을 방지: 방금 열린 프레임은 무시.
        bool justOpened = !wasInventoryOpen;
        wasInventoryOpen = true;
        if (justOpened) return;

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