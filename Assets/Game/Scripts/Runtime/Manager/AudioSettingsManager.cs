using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Mixer References")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string bgmVolumeParam = "BGMVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string uiVolumeParam = "UIVolume";

    [Header("UI References")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    private const float MIN_VOLUME_DB = -80f;
    private const float MAX_VOLUME_DB = 0f;
    private const string VOLUME_PREFIX = "Volume_";

    private void Awake()
    {
        ServiceLocator.Register(this);
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        // Load saved values or set defaults
        float savedBGM = PlayerPrefs.GetFloat(VOLUME_PREFIX + bgmVolumeParam, 1f);
        float savedSFX = PlayerPrefs.GetFloat(VOLUME_PREFIX + sfxVolumeParam, 1f);
        float savedUI = PlayerPrefs.GetFloat(VOLUME_PREFIX + uiVolumeParam, 1f);

        // Set slider values
        if (bgmSlider != null)
        {
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            SetBGMVolume(savedBGM);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            SetSFXVolume(savedSFX);
        }

        if (uiSlider != null)
        {
            uiSlider.value = savedUI;
            uiSlider.onValueChanged.AddListener(SetUIVolume);
            SetUIVolume(savedUI);
        }
        // Register callbacks
        RegisterSliderCallbacks();
    }
    void RegisterSliderCallbacks()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiSlider != null) uiSlider.onValueChanged.AddListener(SetUIVolume);
    }


    public void SetBGMVolume(float linearValue)
    {
        float dbValue = ConvertToDecibel(linearValue);
        masterMixer.SetFloat(bgmVolumeParam, dbValue);
        PlayerPrefs.SetFloat(VOLUME_PREFIX + bgmVolumeParam, linearValue);
    }

    public void SetSFXVolume(float linearValue)
    {
        float dbValue = ConvertToDecibel(linearValue);
        masterMixer.SetFloat(sfxVolumeParam, dbValue);
        PlayerPrefs.SetFloat(VOLUME_PREFIX + sfxVolumeParam, linearValue);
    }

    public void SetUIVolume(float linearValue)
    {
        float dbValue = ConvertToDecibel(linearValue);
        masterMixer.SetFloat(uiVolumeParam, dbValue);
        PlayerPrefs.SetFloat(VOLUME_PREFIX + uiVolumeParam, linearValue);
    }

    private float ConvertToDecibel(float linear)
    {
        return linear <= 0.0001f ? MIN_VOLUME_DB : Mathf.Log10(linear) * 20f;
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
        if (uiSlider != null) uiSlider.onValueChanged.RemoveListener(SetUIVolume);
        ServiceLocator.Unregister<AudioSettingsManager>();
        // Save settings on exit
        PlayerPrefs.Save();
    }
}