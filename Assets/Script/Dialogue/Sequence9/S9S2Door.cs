using TMPro;
using UnityEngine;

/// <summary>
/// 시퀀스9 씬2의 문. 조사(Space)하면 컨트롤러에 알린다.
/// 이동 자체는 컨트롤러가 진행 상태를 보고 결정한다. (같은 복도 문이라도 회차마다 결과가 다르다)
///
/// 문은 벽 칸에 있어서 하루가 밟고 지나갈 수 없다. 그래서 접촉이 아니라 조사로 발동한다.
/// 레이어는 NoPassing(6)이어야 InteractionSystem의 레이캐스트에 잡힌다.
/// </summary>
public class S9S2Door : InspectableObject
{
    public enum DoorKind
    {
        DarkroomFromLobby, // 사진관 로비 → 암실
        ShelfToHouse,      // 암실 선반 문 → 변형된 집
        Wardrobe,          // 집 옷장 → 영화관 로비
        CinemaToScreen,    // 영화관 로비 오른쪽 위 문 → 스크린 공간
        ScreenDoor,        // 스크린 하단 검은 문 → 반복되는 복도
        Corridor,          // 반복되는 복도의 401/403/404/405 (→ 402)
    }

    [SerializeField]
    private Sequence9Scene2DialogueController _controller;

    public DoorKind Kind = DoorKind.Corridor;

    [Tooltip("복도 문일 때 왼쪽부터 0, 1, 2, 3. 케이는 2번(세 번째) 문에서 나온다.")]
    public int CorridorIndex;

    [Tooltip("복도 문의 처음 호수")]
    public string OriginalPlate = "401";

    [Header("표시 (임시 리소스)")]
    [Tooltip("문패 텍스트. 문패 스프라이트가 나오면 교체할 것.")]
    [SerializeField]
    private TMP_Text _plate;

    [Tooltip("열린 상태로 보일 오브젝트 (검은 문틈 등). 닫혀 있으면 꺼둔다.")]
    [SerializeField]
    private GameObject _openVisual;

    [Tooltip("문 자체가 처음부터 안 보여야 하는 경우 (스크린 문). 꺼두면 조사도 안 된다.")]
    [SerializeField]
    private GameObject _doorVisual;

    protected override void OnInspect()
    {
        // 여러 번 조사할 수 있어야 하므로 플래그는 바로 되돌린다
        hasBeenInspected = false;

        if (_controller != null)
            _controller.OnDoorInspected(this);
    }

    public void SetPlate(string text)
    {
        if (_plate != null)
            _plate.text = text;
    }

    public void SetOpen(bool open)
    {
        if (_openVisual != null)
            _openVisual.SetActive(open);
    }

    /// <summary>문을 아예 숨긴다. 콜라이더까지 꺼서 조사도 막힌다.</summary>
    public void SetPresent(bool present)
    {
        if (_doorVisual != null)
            _doorVisual.SetActive(present);

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = present;
    }
}
