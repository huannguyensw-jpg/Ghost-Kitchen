using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;

    private void Start()
    {
        // Khi bắt đầu game, ẩn Panel Settings
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Gọi bằng nút Settings
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // Gọi bằng nút X / Close
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Có thể dùng chung cho một nút Toggle
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}