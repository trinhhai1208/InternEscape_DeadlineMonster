using UnityEngine;
using UnityEngine.SceneManagement; // Cần dùng để chuyển Scene

/// <summary>
/// MainMenuManager: Script quản lý Scene màn hình chính (Start Menu)
/// Gắn script này vào một GameObject trống (vd: MainMenuController) trong Scene Màn Hình Chờ.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // Cờ hiệu liên lạc giữa 2 sảnh. Lưu mặc định tĩnh để dù chuyển scene vẫn không bị tẩy trí nhớ
    public static bool jumpToMapSelection = false;

    [Header("UI Màn Hình Chọn Map (Tắt mặc định)")]
    public GameObject mapSelectionPanel;

    public GameObject mainMenuPanel;

    private void Start()
    {
        // Ra lệnh cho hệ thống bật Nhạc Sảnh Chính
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

        // Nắm bắt cờ hiệu khi vừa đi từ Game quay về Menu
        if (jumpToMapSelection)
        {
            jumpToMapSelection = false; // Reset trí nhớ để lần sau không tự bật
            if (mapSelectionPanel != null) mapSelectionPanel.SetActive(true);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Gắn hàm này vào sự kiện OnClick() của nút PLAY / START (Mở bảng)
    /// </summary>
    public void ShowMapSelection()
    {
        if (mapSelectionPanel != null)
        {
            mapSelectionPanel.SetActive(true);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Chưa gán Panel Bảng Chọn Map vào MainMenuManager!");
        }
    }
    /// <summary>
    /// Gắn hàm này vào sự kiện OnClick() của từng Nút Map.
    /// Bạn gõ tên "testmap1", "testmap2" vào ô trống hiện ra dưới nút OnClick.
    /// </summary>
    public void LoadMap(string mapSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mapSceneName);
        Debug.Log("Đang tải map: " + mapSceneName);
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
