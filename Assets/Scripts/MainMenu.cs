using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject howToPlayPanel;
    public void LoadGame()
    {
        GameProgress.Clear(); // Bắt đầu game mới: reset máu & flame về mặc định
        SceneManager.LoadScene("PlayScreen");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void OpenHowToPlay()
    {
        howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel.SetActive(false);
    }
}
