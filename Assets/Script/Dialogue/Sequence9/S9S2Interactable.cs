using UnityEngine;

/// <summary>
/// 시퀀스9 씬2의 조사 오브젝트 (암실 사진 벽, 집 전화기, 복도 권총).
/// 결과는 컨트롤러가 처리하고, 여기서는 반짝임 표시만 들고 있는다.
/// </summary>
public class S9S2Interactable : InspectableObject
{
    public enum InteractKind
    {
        PhotoWall, // 루나 얼굴 일러스트 팝업
        Telephone, // 402호로 오라는 전화
        Pistol,    // 권총 습득
    }

    [SerializeField]
    private Sequence9Scene2DialogueController _controller;

    public InteractKind Kind;

    [Tooltip("조사 가능 표시 (VFX_interactionlight). 조사가 끝나면 끈다.")]
    [SerializeField]
    private GameObject _sparkle;

    protected override void OnInspect()
    {
        // 한 번만 쓸지는 컨트롤러가 상태로 판단한다
        hasBeenInspected = false;

        if (_controller != null)
            _controller.OnInteract(this);
    }

    public void SetSparkle(bool on)
    {
        if (_sparkle != null)
            _sparkle.SetActive(on);
    }
}
