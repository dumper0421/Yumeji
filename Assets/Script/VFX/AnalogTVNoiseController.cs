using UnityEngine;

public class AnalogTVNoiseController : MonoBehaviour
{
    public enum TVNoisePreset
    {
        Off,         // 0
        Stage1,      // 30
        Stage2,      // 50
        Stage3,      // 70
        ExitHallway  // 90
    }

    [SerializeField] private Material tvMaterial;

    [Range(0f, 100f)]
    [SerializeField] private float strengthPercent = 0f;

    [SerializeField] private float maxNoiseIntensity = 0.12f;
    [SerializeField] private float maxScanlineIntensity = 0.10f;
    [SerializeField] private float maxJitterAmount = 0.0035f;
    [SerializeField] private float maxBurstIntensity = 0.22f;
    [SerializeField] private float maxBrightnessFlicker = 0.04f;
    [SerializeField] private float maxDriftAmount = 0.0020f;

    [SerializeField] private float noiseScale = 320f;
    [SerializeField] private float scanlineDensity = 1.2f;
    [SerializeField] private float jitterBands = 180f;
    [SerializeField] private float jitterSpeed = 24f;
    [SerializeField] private float driftFrequency = 80f;
    [SerializeField] private float burstChance = 0.08f;

    private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

    private static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int ScanlineDensityId = Shader.PropertyToID("_ScanlineDensity");

    private static readonly int JitterAmountId = Shader.PropertyToID("_JitterAmount");
    private static readonly int JitterBandsId = Shader.PropertyToID("_JitterBands");
    private static readonly int JitterSpeedId = Shader.PropertyToID("_JitterSpeed");

    private static readonly int DriftAmountId = Shader.PropertyToID("_DriftAmount");
    private static readonly int DriftFrequencyId = Shader.PropertyToID("_DriftFrequency");

    private static readonly int BurstIntensityId = Shader.PropertyToID("_BurstIntensity");
    private static readonly int BurstChanceId = Shader.PropertyToID("_BurstChance");

    private static readonly int BrightnessFlickerId = Shader.PropertyToID("_BrightnessFlicker");

    private void Awake()
    {
        ApplySettings();
    }

    private void OnValidate()
    {
        ApplySettings();
    }

    /// <summary>
    /// 0~100 값을 받아 효과 강도를 적용
    /// </summary>
    public void SetStrengthPercent(float percent)
    {
        strengthPercent = Mathf.Clamp(percent, 0f, 100f);
        ApplySettings();
    }

    /// <summary>
    /// 효과 완전 끄기
    /// </summary>
    public void SetOff()
    {
        strengthPercent = 0f;
        ApplySettings();
    }

    /// <summary>
    /// 프리셋 적용
    /// </summary>
    public void SetPreset(TVNoisePreset preset)
    {
        switch (preset)
        {
            case TVNoisePreset.Off:
                strengthPercent = 0f;
                break;
            case TVNoisePreset.Stage1:
                strengthPercent = 30f;
                break;
            case TVNoisePreset.Stage2:
                strengthPercent = 50f;
                break;
            case TVNoisePreset.Stage3:
                strengthPercent = 70f;
                break;
            case TVNoisePreset.ExitHallway:
                strengthPercent = 90f;
                break;
        }

        ApplySettings();
    }

    private void ApplySettings()
    {
        if (tvMaterial == null)
            return;

        float t = Mathf.Clamp01(strengthPercent / 100f);

        // 강도 0일 때는 완전 꺼지게
        float noiseIntensity = maxNoiseIntensity * t;
        float scanlineIntensity = maxScanlineIntensity * t;
        float jitterAmount = maxJitterAmount * t;
        float burstIntensity = maxBurstIntensity * t;
        float brightnessFlicker = maxBrightnessFlicker * t;
        float driftAmount = maxDriftAmount * t;

        tvMaterial.SetFloat(NoiseIntensityId, noiseIntensity);
        tvMaterial.SetFloat(NoiseScaleId, noiseScale);

        tvMaterial.SetFloat(ScanlineIntensityId, scanlineIntensity);
        tvMaterial.SetFloat(ScanlineDensityId, scanlineDensity);

        tvMaterial.SetFloat(JitterAmountId, jitterAmount);
        tvMaterial.SetFloat(JitterBandsId, jitterBands);
        tvMaterial.SetFloat(JitterSpeedId, jitterSpeed);

        tvMaterial.SetFloat(DriftAmountId, driftAmount);
        tvMaterial.SetFloat(DriftFrequencyId, driftFrequency);

        tvMaterial.SetFloat(BurstIntensityId, burstIntensity);
        tvMaterial.SetFloat(BurstChanceId, burstChance * t);
        // 강도 낮을 때 버스트도 덜 나오게

        tvMaterial.SetFloat(BrightnessFlickerId, brightnessFlicker);
    }

    public float GetCurrentStrengthPercent()
    {
        return strengthPercent;
    }
}