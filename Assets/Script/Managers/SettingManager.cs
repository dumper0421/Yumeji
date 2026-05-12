// SettingManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : Singleton<SettingManager>
{
    public KeyboardOnlySlider MasterSlider;
    public KeyboardOnlySlider BgmSlider;
    public KeyboardOnlySlider SfxSlider;
    public GameObject BackGround;

    private KeyboardOnlySlider[] sliders;
    private int selectedIndex = 0;
    private Color normal = Color.white;
    private Color highlighted = Color.yellow;

    protected override void Init()
    {
      //  DontDestroyOnLoad(gameObject);
        sliders = new[] { MasterSlider, BgmSlider, SfxSlider };

        // 저장된 설정 로드 및 오디오 매니저에 적용

        // 슬라이더 이벤트 연결
        MasterSlider.onValueChanged.AddListener(v => SoundManager.Instance.SetMasterVolume(v));
        MasterSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("Master", v));
        BgmSlider.onValueChanged.AddListener(v => SoundManager.Instance.SetBGMVolume(v));
        BgmSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("BGM", v));
        SfxSlider.onValueChanged.AddListener(v => SoundManager.Instance.SetSFXVolume(v));
        SfxSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("SFX", v));

        UpdateHighlight();

        if (BackGround == null)
            BackGround = transform.GetChild(0).gameObject;
    }

    public void Start()
    {
        LoadSettings();
        IsDontDestroyOnLoad = false;
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("Master", MasterSlider.value);
        float bgm = PlayerPrefs.GetFloat("BGM", BgmSlider.value);
        float sfx = PlayerPrefs.GetFloat("SFX", SfxSlider.value);

        MasterSlider.value = master;
        BgmSlider.value = bgm;
        SfxSlider.value = sfx;

        SoundManager.Instance.SetMasterVolume(master, save: false);
        SoundManager.Instance.SetBGMVolume(bgm, save: false);
        SoundManager.Instance.SetSFXVolume(sfx, save: false);
    }

    private void Update()
    {

        if (!BackGround.gameObject.activeSelf) return;

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
        if (Input.GetKey(KeyCode.RightArrow)) 
            dir = +1;
        else if (Input.GetKey(KeyCode.LeftArrow)) 
            dir = -1;

        if (dir != 0)
        {
            var s = sliders[selectedIndex];
            s.value = Mathf.Clamp01(s.value + dir * 0.002f);
            // 슬라이더 onValueChanged가 호출되어 SoundManager와 PlayerPrefs에 자동 저장/적용됨
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            transform.GetChild(0).gameObject.SetActive(false);
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
}
