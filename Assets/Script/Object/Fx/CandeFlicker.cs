using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CandleFlicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _flame;
    [SerializeField] private Light2D _light2D;

    [Header("Flame Motion")]
    [SerializeField] private float _motionSpeed = 4f;
    [SerializeField] private float _rotationAmount = 3f;
    [SerializeField] private float _positionAmount = 0.03f;
    [SerializeField] private float _scaleAmount = 0.08f;

    [Header("Light Flicker")]
    [SerializeField] private float _lightSpeed = 3f;
    [SerializeField] private float _baseIntensity = 1.2f;
    [SerializeField] private float _intensityRange = 0.25f;
    [SerializeField] private float _baseRadius = 1.8f;
    [SerializeField] private float _radiusRange = 0.15f;

    private Vector3 _startLocalPosition;
    private Vector3 _startLocalScale;
    private Quaternion _startLocalRotation;

    public Transform Flame => _flame;
    public Light2D Light2D => _light2D;

    private void Awake()
    {
        if (_flame == null)
            _flame = transform;

        _startLocalPosition = _flame.localPosition;
        _startLocalScale = _flame.localScale;
        _startLocalRotation = _flame.localRotation;
    }

    private void Update()
    {
        UpdateFlame();
        UpdateLight();
    }

    private void UpdateFlame()
    {
        float _time = Time.time * _motionSpeed;

        float _rotationNoise = Mathf.PerlinNoise(_time, 0f) - 0.5f;
        float _positionNoise = Mathf.PerlinNoise(0f, _time) - 0.5f;
        float _scaleNoise = Mathf.PerlinNoise(_time, _time) - 0.5f;

        float _rotationZ = _rotationNoise * _rotationAmount * 2f;
        _flame.localRotation = _startLocalRotation * Quaternion.Euler(0f, 0f, _rotationZ);

        float _offsetX = _positionNoise * _positionAmount * 2f;
        _flame.localPosition = _startLocalPosition + new Vector3(_offsetX, 0f, 0f);

        float _scaleX = _startLocalScale.x + _scaleNoise * _scaleAmount;
        float _scaleY = _startLocalScale.y + Mathf.Abs(_rotationNoise) * _scaleAmount;

        _flame.localScale = new Vector3(_scaleX, _scaleY, _startLocalScale.z);
    }

    private void UpdateLight()
    {
        if (_light2D == null)
            return;

        float _time = Time.time * _lightSpeed;

        float _intensityNoise = Mathf.PerlinNoise(_time, 1.37f);
        float _radiusNoise = Mathf.PerlinNoise(2.91f, _time);

        _light2D.intensity = _baseIntensity + (_intensityNoise - 0.5f) * 2f * _intensityRange;
        _light2D.pointLightOuterRadius = _baseRadius + (_radiusNoise - 0.5f) * 2f * _radiusRange;
    }

    public void SetLightEnabled(bool enabled)
    {
        if (_light2D == null)
            return;

        _light2D.enabled = enabled;
    }

    public void ResetFlicker()
    {
        if (_flame != null)
        {
            _flame.localPosition = _startLocalPosition;
            _flame.localScale = _startLocalScale;
            _flame.localRotation = _startLocalRotation;
        }

        if (_light2D != null)
        {
            _light2D.intensity = _baseIntensity;
            _light2D.pointLightOuterRadius = _baseRadius;
        }
    }
}