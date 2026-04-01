using UnityEngine;
using UnityEngine.SceneManagement; // Cần dùng để chuyển Scene

/// <summary>
/// MainMenuManager: Script quản lý Scene màn hình chính (Start Menu)
/// Gắn script này vào một GameObject trống (vd: MainMenuController) trong Scene Màn Hình Chờ.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // Cài đặt tên Scene chính xác xác của game (vd: "PlayerSence_Test")
    [Header("Game Scene Name")]
    public string gameSceneName = "PlayerSence";

    /// <summary>
    /// Gắn hàm này vào sự kiện OnClick() của nút PLAY / START
    /// </summary>
    public void PlayGame()
    {
        // Đảm bảo timeScale = 1 trước khi load phòng trường hợp từ scene GameOver nhảy qua
        Time.timeScale = 1f;

        // Load scene game
        SceneManager.LoadScene(gameSceneName);
        Debug.Log("Loading Scene: " + gameSceneName);
    }

    /// <summary>
    /// Gắn hàm này vào sự kiện OnClick() của nút QUIT / EXIT (nếu có)
    /// </summary>
    public void QuitGame()
    {
        // Hàm Quit chỉ hoạt động khi game đã Build ra máy thật (PC, Android, iOS)
        // Khi chạy trong Unity Editor nó sẽ không tự tắt, ta in ra log để biết
        Debug.Log("Game Exited!");
        Application.Quit();
    }
}
