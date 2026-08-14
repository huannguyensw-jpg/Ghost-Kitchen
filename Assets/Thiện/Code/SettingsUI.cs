using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider volumeSlider;
    public Slider brightnessSlider;

    private void Start()
    {
        // Tự động kết nối Slider của Menu này vào SettingsManager
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.BindUI(volumeSlider, brightnessSlider);
        }
    }
}