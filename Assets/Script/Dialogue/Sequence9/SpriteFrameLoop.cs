using UnityEngine;

/// <summary>
/// 스프라이트 프레임을 처음부터 한 번 재생한 뒤, _loopFrom 이후 프레임만 반복한다.
/// 켜질 때마다 처음부터 재생한다. 이미 나타나 있던 상태로 복원할 때는 JumpToLoop를 부른다.
/// (9-2 스크린 검은 문: 8프레임 중 앞 4프레임에서 문이 생기고 뒤 4프레임을 반복)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameLoop : MonoBehaviour
{
    [SerializeField]
    private Sprite[] _frames;

    [Tooltip("한 바퀴 재생 후 되돌아갈 프레임 번호")]
    [SerializeField]
    private int _loopFrom = 4;

    [SerializeField]
    private float _frameSeconds = 0.12f;

    private SpriteRenderer _sr;
    private int _index;
    private float _timer;

    private void OnEnable()
    {
        _sr = GetComponent<SpriteRenderer>();
        _index = 0;
        _timer = 0f;
        ApplyFrame();
    }

    public void JumpToLoop()
    {
        if (_frames == null || _frames.Length == 0)
            return;

        _index = Mathf.Clamp(_loopFrom, 0, _frames.Length - 1);
        _timer = 0f;
        ApplyFrame();
    }

    private void Update()
    {
        if (_frames == null || _frames.Length == 0 || _frameSeconds <= 0f)
            return;

        _timer += Time.deltaTime;

        while (_timer >= _frameSeconds)
        {
            _timer -= _frameSeconds;
            _index++;

            if (_index >= _frames.Length)
                _index = Mathf.Clamp(_loopFrom, 0, _frames.Length - 1);
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (_sr != null && _frames != null && _frames.Length > 0 && _frames[_index] != null)
            _sr.sprite = _frames[_index];
    }
}
