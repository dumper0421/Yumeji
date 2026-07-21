using UnityEngine;

/// <summary>
/// 플레이어의 '보이는 모습'만 위아래로 밀어주는 컴포넌트.
///
/// 이 게임은 SpriteRenderer와 BoxCollider2D가 같은 오브젝트에 있어서
/// 트랜스폼을 옮기면 충돌 판정까지 함께 움직인다.
/// 그래서 겉모습 전용 스프라이트(분신)를 자식으로 하나 만들어 그것만 옮기고,
/// 실제 플레이어 오브젝트는 원래 자리에 그대로 둔다.
///
/// 분신은 매 프레임 원본의 스프라이트를 그대로 따라 하므로 애니메이션도 정상 동작한다.
/// StairVisualStep이 자동으로 붙여주므로 직접 붙일 필요는 없다.
/// </summary>
[DefaultExecutionOrder(100)] // 이동 처리가 끝난 뒤에 보정하도록
public class PlayerVisualOffset : MonoBehaviour
{
    [Tooltip("보정 위치로 따라가는 속도. 클수록 즉각적")]
    [SerializeField] private float _smoothSpeed = 14f;

    private SpriteRenderer _source;
    private SpriteRenderer _proxy;
    private Transform _proxyTransform;

    private float _targetOffsetY;
    private float _currentOffsetY;

    /// <summary>플레이어에게 이 컴포넌트가 없으면 붙여서 돌려준다.</summary>
    public static PlayerVisualOffset GetOrCreate(GameObject player)
    {
        if (player == null) return null;

        var offset = player.GetComponent<PlayerVisualOffset>();
        if (offset == null)
            offset = player.AddComponent<PlayerVisualOffset>();

        return offset;
    }

    private void Awake()
    {
        _source = GetComponent<SpriteRenderer>();
    }

    /// <summary>이 프레임에 적용할 세로 보정값을 지정한다.</summary>
    public void SetTargetOffset(float offsetY)
    {
        _targetOffsetY = offsetY;
    }

    /// <summary>보정을 풀고 원래 모습으로 되돌린다.</summary>
    public void ClearOffset()
    {
        _targetOffsetY = 0f;
    }

    private void LateUpdate()
    {
        if (_source == null) return;

        _currentOffsetY = Mathf.Lerp(
            _currentOffsetY,
            _targetOffsetY,
            1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime));

        bool needsProxy = Mathf.Abs(_currentOffsetY) > 0.001f;

        if (!needsProxy)
        {
            // 보정이 없으면 원본을 그대로 보여준다
            _source.enabled = true;
            if (_proxy != null)
                _proxy.enabled = false;
            return;
        }

        EnsureProxy();

        // 원본은 숨기고 분신이 대신 그려진다
        _source.enabled = false;
        _proxy.enabled = true;

        CopyRendererState();
        _proxyTransform.localPosition = new Vector3(0f, _currentOffsetY, 0f);
    }

    private void EnsureProxy()
    {
        if (_proxy != null) return;

        var obj = new GameObject("VisualProxy");
        obj.transform.SetParent(transform, false);
        obj.layer = gameObject.layer;

        _proxy = obj.AddComponent<SpriteRenderer>();
        _proxyTransform = obj.transform;
    }

    /// <summary>애니메이션이 바꾸는 값들을 매 프레임 분신에 복사한다.</summary>
    private void CopyRendererState()
    {
        _proxy.sprite = _source.sprite;
        _proxy.color = _source.color;
        _proxy.flipX = _source.flipX;
        _proxy.flipY = _source.flipY;
        _proxy.material = _source.sharedMaterial;
        _proxy.sortingLayerID = _source.sortingLayerID;
        _proxy.sortingOrder = _source.sortingOrder;
        _proxy.maskInteraction = _source.maskInteraction;
        _proxy.drawMode = _source.drawMode;

        if (_proxy.gameObject.layer != gameObject.layer)
            _proxy.gameObject.layer = gameObject.layer;
    }

    private void OnDisable()
    {
        // 꺼질 때 원래 모습으로 복구
        if (_source != null) _source.enabled = true;
        if (_proxy != null) _proxy.enabled = false;

        _currentOffsetY = 0f;
        _targetOffsetY = 0f;
    }
}
