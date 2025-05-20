using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingManager : Singleton<SettingManager>
{
    public KeyboardOnlySlider MasterSlider;
    public KeyboardOnlySlider BgmSlider;
    public KeyboardOnlySlider SfxSlider;
    public AudioMixer audioMixer;

    private KeyboardOnlySlider[] sliders;
    private int selectedIndex = 0;
    private Color normal = Color.white;
    private Color highlighted = Color.yellow;

    private const string KEY_MASTER = "Master";
    private const string KEY_BGM = "BGM";
    private const string KEY_SFX = "SFX";

    protected override void Init()
    {
        DontDestroyOnLoad(gameObject);
        sliders = new[] { MasterSlider, BgmSlider, SfxSlider };

        // 슬라이더 값 로드 및 믹서에 적용
        LoadSettings();

        // 슬라이더 이벤트 연결
        MasterSlider.onValueChanged.AddListener(OnMasterChanged);
        BgmSlider.onValueChanged.AddListener(OnBgmChanged);
        SfxSlider.onValueChanged.AddListener(OnSfxChanged);

        UpdateHighlight();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(KEY_MASTER)) MasterSlider.value = PlayerPrefs.GetFloat(KEY_MASTER);
        if (PlayerPrefs.HasKey(KEY_BGM)) BgmSlider.value = PlayerPrefs.GetFloat(KEY_BGM);
        if (PlayerPrefs.HasKey(KEY_SFX)) SfxSlider.value = PlayerPrefs.GetFloat(KEY_SFX);

        // Mixer에도 반영
        SetMixer("Master", MasterSlider.value);
        SetMixer("BGM", BgmSlider.value);
        SetMixer("SFX", SfxSlider.value);
    }

    private void OnMasterChanged(float v)
    {
        SetMixer("Master", v);
        PlayerPrefs.SetFloat(KEY_MASTER, v);
        PlayerPrefs.Save();
    }
    private void OnBgmChanged(float v)
    {
        SetMixer("BGM", v);
        PlayerPrefs.SetFloat(KEY_BGM, v);
        PlayerPrefs.Save();
    }
    private void OnSfxChanged(float v)
    {
        SetMixer("SFX", v);
        PlayerPrefs.SetFloat(KEY_SFX, v);
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex + sliders.Length - 1) % sliders.Length;
            UpdateHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % sliders.Length;
            UpdateHighlight();
        }

        float dir = 0;
        if (Input.GetKey(KeyCode.RightArrow)) dir = +1;
        else if (Input.GetKey(KeyCode.LeftArrow)) dir = -1;

        if (dir != 0)
        {
            var s = sliders[selectedIndex];
            s.value = Mathf.Clamp01(s.value + dir * Time.deltaTime);
            OnSliderChanged(selectedIndex, s.value);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            var col = sliders[i].colors;
            col.normalColor = i == selectedIndex ? highlighted : normal;
            sliders[i].colors = col;
        }
    }

    private void SetMixer(string param, float linear)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(param, dB);
    }
    void OnSliderChanged(int idx, float v)
    {
        if (idx == 0) OnMasterChanged(v);
        else if (idx == 1) OnBgmChanged(v);
        else OnSfxChanged(v);
    }
}


