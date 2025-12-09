using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject howToPlayPanel;

    [Header("Main Menu Buttons")]
    public Button startGameButton;
    public Button howToPlayButton;
    public Button quitButton;

    [Header("How To Play Panel Buttons")]
    public Button closeHowToPlayButton;

    void Start()
    {
        ShowMainMenu();

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(ShowHowToPlayPanel);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (closeHowToPlayButton != null)
            closeHowToPlayButton.onClick.AddListener(ShowMainMenu);
    }

    void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    void ShowHowToPlayPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    void StartGame()
    {
        SceneManager.LoadScene("Level1");
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
