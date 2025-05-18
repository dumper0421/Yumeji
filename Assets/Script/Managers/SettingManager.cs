using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingManager : Singleton<SettingManager>
{
    public Slider MasterSlider;
    public Slider BgmSlider;
    public Slider SfxSlider;
    public AudioMixer audioMixer;

    int selectedIndex = 0;
    Slider[] sliders;
    Color normal = Color.white;
    Color highlighted = Color.yellow;
    protected override void Init()
    {
        sliders = new[] { MasterSlider, BgmSlider, SfxSlider };
        // 믹서에서 초기값 읽어 슬라이더에 세팅
        SetSliderFromMixer("Master", MasterSlider);
        SetSliderFromMixer("Bgm", BgmSlider);
        SetSliderFromMixer("SFX", SfxSlider);
    }

 
    void Update()
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
    }

    void UpdateHighlight()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            var col = sliders[i].colors;
            col.normalColor = i == selectedIndex ? highlighted : normal;
            sliders[i].colors = col;
        }
    }

    public void OnMasterChanged(float v) => SetMixer("Master", v);
    public void OnBgmChanged(float v) => SetMixer("Bgm", v);
    public void OnSfxChanged(float v) => SetMixer("SFX", v);

    void OnSliderChanged(int idx, float v)
    {
        if (idx == 0) OnMasterChanged(v);
        else if (idx == 1) OnBgmChanged(v);
        else OnSfxChanged(v);
    }

    void SetMixer(string param, float linear)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(param, dB);
    }

    void SetSliderFromMixer(string param, Slider slider)
    {
        if (audioMixer.GetFloat(param, out float dB))
            slider.value = Mathf.Pow(10f, dB / 20f);
    }
}

