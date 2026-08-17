using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Thêm namespace này

public class SceneController : MonoBehaviour
{
    [Header("Optional Key Binds")]
    public Button targetButton;

    [Tooltip("Phím tắt bàn phím (dạng phím của Input System mới, ví dụ: Key.Space, Key.R, Key.Escape)")]
    public Key shortcutKey = Key.None;

    void Update()
    {
        // Kiểm tra phím bấm theo Input System mới
        if (shortcutKey != Key.None && Keyboard.current != null && Keyboard.current[shortcutKey].wasPressedThisFrame)
        {
            if (targetButton != null && targetButton.interactable)
            {
                targetButton.onClick.Invoke();
            }
            else
            {
                RestartCurrentScene();
            }
        }
    }

    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ResolveSceneName(sceneName));
    }

    public static string ResolveSceneName(string sceneName)
    {
        switch (sceneName)
        {
            case "Introduction":
                return "Introductionnhat";

            case "Maingame":
                return "Maingameminhnhat";

            case "Night":
                return "Nightnhat";

            default:
                return sceneName;
        }
    }

    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Đã gán lệnh thoát game!");
        Application.Quit();
    }

    
}
