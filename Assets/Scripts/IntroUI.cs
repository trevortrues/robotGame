using UnityEngine;

public class IntroUI : MonoBehaviour
{
    public GameObject introPanel;

    void Start()
    {
        introPanel.SetActive(true); // Show on scene start
        Time.timeScale = 0f;        // Pause the game
    }

    public void CloseIntro()
    {
        introPanel.SetActive(false); // Hide UI
        Time.timeScale = 1f;         // Resume game
    }
}
