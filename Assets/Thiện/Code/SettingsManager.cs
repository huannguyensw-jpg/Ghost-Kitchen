using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio Settings")]
    public AudioMixer mainMixer;
    public string masterVolumeParam = "MasterVolume";
    public Slider masterVolumeSlider;

    [Header("Brightness Settings")]
    public Slider brightnessSlider;
    public Light directionalLight; // Tuỳ chọn: Ánh sáng Mặt Trời/Đèn chính trong Scene

    // Keys lưu trữ PlayerPrefs
    private const string MASTER_VOL_KEY = "MasterVolume";
    private const string BRIGHTNESS_KEY = "GameBrightness";

    private void Awake()
    {
        // Khởi tạo Singleton để dùng ở mọi Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadAndApplySettings();
        SetupUIEvents();
    }

    private void OnEnable()
    {
        // Khi load sang Scene mới, tìm lại UI Slider nếu có
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Tự động tìm lại Directional Light của Scene mới
        if (directionalLight == null)
        {
            Light mainLight = RenderSettings.sun;
            if (mainLight != null)
            {
                directionalLight = mainLight;
            }
            else
            {
                Light foundLight = FindObjectOfType<Light>();
                if (foundLight != null && foundLight.type == LightType.Directional)
                {
                    directionalLight = foundLight;
                }
            }
        }

        // Áp dụng lại cài đặt Ánh sáng cho Scene mới
        float savedBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);
        SetBrightness(savedBrightness);
    }

    // =========================================================
    // LOAD & APPLY CÀI ĐẶT
    // =========================================================
    public void LoadAndApplySettings()
    {
        float savedVolume = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 0.75f);
        float savedBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);

        SetVolume(savedVolume);
        SetBrightness(savedBrightness);

        if (masterVolumeSlider != null) masterVolumeSlider.value = savedVolume;
        if (brightnessSlider != null) brightnessSlider.value = savedBrightness;
    }

    private void SetupUIEvents()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }

    // =========================================================
    // HÀM ĐIỀU CHỈNH ÂM THANH
    // =========================================================
    public void SetVolume(float value)
    {
        // Quy đổi giá trị Slider (0.0001 -> 1.0) sang Decibel (-80dB -> 0dB)
        float dbValue = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        if (mainMixer != null)
        {
            mainMixer.SetFloat(masterVolumeParam, dbValue);
        }

        PlayerPrefs.SetFloat(MASTER_VOL_KEY, value);
        PlayerPrefs.Save();
    }

    // =========================================================
    // HÀM ĐIỀU CHỈNH ÁNH SÁNG
    // =========================================================
    public void SetBrightness(float value)
    {
        // Clamp value từ 0.05 (tối) đến 1.5 (sáng)
        float brightnessFactor = Mathf.Clamp(value, 0.05f, 1.5f);

        // 1. Chỉnh Ambient Intensity (Có tác dụng cả khi dùng Skybox)
        RenderSettings.ambientIntensity = brightnessFactor;

        // 2. Chỉnh Ambient Color (Có tác dụng khi dùng Color/Flat)
        RenderSettings.ambientLight = Color.white * brightnessFactor;

        // 3. Chỉnh Cường độ đèn mặt trời / Directional Light trong Scene
        if (directionalLight != null)
        {
            directionalLight.intensity = brightnessFactor;
        }

        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, value);
        PlayerPrefs.Save();
    }

    // =========================================================
    // GÁN UI SLIDER TỪ CÁC SCENE (MENU/GAME)
    // =========================================================
    public void BindUI(Slider volSlider, Slider brightSlider)
    {
        masterVolumeSlider = volSlider;
        brightnessSlider = brightSlider;

        float savedVolume = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 0.75f);
        float savedBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = savedVolume;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }
}