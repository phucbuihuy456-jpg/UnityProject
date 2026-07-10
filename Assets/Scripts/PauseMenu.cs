using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện cần thiết để chuyển Scene

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo Object cửa sổ Pause Panel vào đây")]
    public GameObject pauseMenuPanel;

    [Header("Settings")]
    [Tooltip("Tên của Scene Main Menu (nhớ gõ đúng từng chữ cái)")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Đảm bảo cửa sổ Pause tắt khi mới vào game và thời gian chạy bình thường (1f)
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // Lắng nghe phím ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // Hàm gọi khi muốn tạm dừng
    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true); // Bật giao diện
        Time.timeScale = 0f;            // Đóng băng thời gian
        isPaused = true;
    }

    // Hàm gọi khi nhấn nút Continue hoặc bấm Esc lần nữa
    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false); // Tắt giao diện
        Time.timeScale = 1f;             // Thời gian trôi bình thường
        isPaused = false;
    }

    // Hàm gọi khi nhấn nút Restart trong panel "You died".
    // Tải lại màn chơi hiện tại -> người chơi quay về điểm xuất phát của màn.
    public void RestartLevel()
    {
        // Chơi lại từ đầu màn: reset máu & flame về mặc định.
        GameProgress.Clear();
        // Phải trả thời gian về 1 trước khi load lại, nếu không scene mới sẽ bị đơ.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Hàm gọi khi nhấn nút Back to menu
    public void BackToMenu()
    {
        // Cực kỳ quan trọng: Phải đưa thời gian về 1 trước khi đổi Scene.
        // Nếu không, khi sang Main Menu, mọi thứ (bao gồm cả animation, click nút) sẽ bị đơ.
        Time.timeScale = 1f;

        // Load Scene Menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
}