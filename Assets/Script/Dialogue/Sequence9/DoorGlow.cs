using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 빛이 새어 나오는 문. 후광 / 바닥 번짐 스프라이트의 알파와 2D 조명 세기를 숨 쉬듯 천천히 흔든다.
/// (9-2 암실 선반 자리의 문)
/// </summary>
public class DoorGlow : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer[] _glows;

    [SerializeField]
    private Light2D _light;

    [Tooltip("밝기 변화 폭 (0.25 = 기준의 ±25%)")]
    [SerializeField]
    private float _pulse = 0.25f;

    [Tooltip("1초에 몇 라디안씩 도는지. 작을수록 느리게 숨 쉰다")]
    [SerializeField]
    private float _speed = 1.3f;

    private float[] _baseAlpha;
    private float _baseIntensity;

    private void Awake()
    {
        _baseAlpha = new float[_glows != null ? _glows.Length : 0];
        for (int i = 0; i < _baseAlpha.Length; i++)
            _baseAlpha[i] = _glows[i] != null ? _glows[i].color.a : 0f;

        if (_light != null)
            _baseIntensity = _light.intensity;
    }

    private void Update()
    {
        float t = Time.time * _speed;

        // 느린 사인 + 약한 잡음: 일정하게 깜빡이지 않고 빛이 일렁이는 느낌
        float k = Mathf.Sin(t) * 0.7f + (Mathf.PerlinNoise(t * 0.6f, 0.37f) - 0.5f) * 0.6f;
        float scale = 1f + _pulse * k;

        for (int i = 0; i < _baseAlpha.Length; i++)
        {
            if (_glows[i] == null)
                continue;

            Color c = _glows[i].color;
            c.a = Mathf.Clamp01(_baseAlpha[i] * scale);
            _glows[i].color = c;
        }

        if (_light != null)
            _light.intensity = _baseIntensity * scale;
    }
}
