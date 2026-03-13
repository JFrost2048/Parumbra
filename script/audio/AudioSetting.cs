using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSetting : MonoBehaviour
{
    public Slider masterSlider, bgmSlider, sfxSlider;
    public AudioMixer audioMixer;

    private float tempMaster, tempBGM, tempSFX;

    void Start()
    {
        LoadSavedValues(); // 게임 시작 시 저장된 값 적용
        SetupSliderListeners(); // 슬라이더 이동 시 바로 적용
    }

    public void RememberCurrentValues()
    {
        tempMaster = masterSlider.value;
        tempBGM = bgmSlider.value;
        tempSFX = sfxSlider.value;

        Debug.Log($"[임시 저장] Master: {tempMaster}, BGM: {tempBGM}, SFX: {tempSFX}");
    }


    // ✅ 슬라이더 이동 시 즉시 적용
    private void SetupSliderListeners()
    {
        masterSlider.onValueChanged.AddListener((v) => SetVolume("MasterVolume", v));
        bgmSlider.onValueChanged.AddListener((v) => SetVolume("BGMVolume", v));
        sfxSlider.onValueChanged.AddListener((v) => SetVolume("SFXVolume", v));
    }

    // ✅ 확인 버튼: 저장
    public void OnClickConfirm()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
        PlayerPrefs.SetFloat("BGMVolume", bgmSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        PlayerPrefs.Save();
    }

    // ❌ 닫기 버튼: 복원
    public void OnClickCancel()
    {
        Debug.Log($"[복원] → Master: {tempMaster}, BGM: {tempBGM}, SFX: {tempSFX}");

        masterSlider.value = tempMaster;
        bgmSlider.value = tempBGM;
        sfxSlider.value = tempSFX;
    }

    // 🔊 저장된 값 불러오기
    public void LoadSavedValues()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        masterSlider.value = master;
        bgmSlider.value = bgm;
        sfxSlider.value = sfx;

        SetVolume("MasterVolume", master);
        SetVolume("BGMVolume", bgm);
        SetVolume("SFXVolume", sfx);
    }

    // 🔉 볼륨 반영 (Log 변환 후 적용)
    private void SetVolume(string parameter, float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        float db = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(parameter, db);
    }
}
