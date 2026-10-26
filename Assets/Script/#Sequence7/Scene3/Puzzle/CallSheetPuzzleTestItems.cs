using System.Collections;
using UnityEngine;

public class CallSheetPuzzleTestItems : MonoBehaviour
{
    [Header("Test Call Sheet Items")]
    [SerializeField] private ItemData castingItem;
    [SerializeField] private ItemData lastShootingItem;
    [SerializeField] private ItemData singleScreeningItem;

    [Header("Test")]
    [SerializeField] private bool giveItemsOnStart = true;


    private IEnumerator Start()
    {
        if (!giveItemsOnStart)
            yield break;

        // Manager들이 초기화될 시간을 한 프레임 기다림
        yield return null;

        GiveAllCallSheets();
    }


    [ContextMenu("TEST - Give All Call Sheets")]
    public void GiveAllCallSheets()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[CallSheet Test] Play 모드에서 사용하세요."
            );

            return;
        }


        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning(
                "[CallSheet Test] InventoryManager가 없습니다."
            );

            return;
        }


        GiveItemIfNeeded(castingItem);
        GiveItemIfNeeded(lastShootingItem);
        GiveItemIfNeeded(singleScreeningItem);


        Debug.Log(
            "[CallSheet Test] 콜시트 3종 테스트 지급 완료"
        );
    }


    private void GiveItemIfNeeded(ItemData item)
    {
        if (item == null)
            return;


        // 이미 가지고 있으면 중복 지급하지 않음
        if (InventoryManager.Instance.HasItem(item))
        {
            Debug.Log(
                "[CallSheet Test] 이미 보유 중: "
                + item.ItemName
            );

            return;
        }


        InventoryManager.Instance.AddItem(item);


        Debug.Log(
            "[CallSheet Test] 지급: "
            + item.ItemName
        );
    }
}