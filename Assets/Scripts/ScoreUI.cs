using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ScoreUI: Quản lý toàn bộ UI trong game.
/// - Hiện điểm (chỉ CodeCommit cộng +10)
/// - Hiện Popup thông báo 1.5 giây khi chạm vào Coffee, SkinUp, Bug
/// </summary>
public class ScoreUI : MonoBehaviour
{
    // Singleton để các script khác dễ dàng truy cập (vd: ScoreUI.Instance.ShowPopup(...))
    public static ScoreUI Instance { get; private set; }

    [Header("── Score UI ──────────────────────")]
    // Kéo Text chứa điểm (TextMeshPro) vào đây
    public TMP_Text scoreText;

    [Header("── Popup UI ──────────────────────")]
    // Kéo Panel/Background của popup vào đây (để bật/tắt)
    public GameObject popupPanel;

    // Kéo Text nằm TRONG popup vào đây (để đổi nội dung chữ)
    public TMP_Text popupText;

    // Thời gian hiển thị popup (mặc định 1.5 giây)
    public float popupDuration = 1.5f;

    // Biến lưu Coroutine hiện tại để có thể tắt ngay lập tức nếu player ăn liên tiếp 2 item
    private Coroutine _activePopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ẩn popup ngay khi mới vào game
        if (popupPanel != null) popupPanel.SetActive(false);

        // Hiển thị điểm ban đầu là 0
        UpdateScore(0);
    }

    // ─── Score ──────────────────────────────────────────────

    /// <summary>
    /// GameManager sẽ gọi hàm này để cập nhật số điểm trên màn hình
    /// </summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = "Code Commit: " + score;
    }

    // ─── Popup ──────────────────────────────────────────────

    /// <summary>
    /// Hàm gọi từ Item/Obstacle để hiện nội dung lên giữa màn hình
    /// Truyền thêm màu để đổi màu chữ cho từng loại item
    /// </summary>
    public void ShowPopup(string message, Color textColor)
    {
        // 1. Dừng popup trước đó (nếu có) để tránh lỗi đè lấn lộn
        if (_activePopup != null) StopCoroutine(_activePopup);

        // 2. Chạy popup mới
        _activePopup = StartCoroutine(PopupRoutine(message, textColor));
    }

    private IEnumerator PopupRoutine(string message, Color textColor)
    {
        // Gán chữ và đổi màu Text
        if (popupText != null) 
        {
            popupText.text = message;
            popupText.color = textColor;
        }

        // Bật panel lên cho người chơi thấy
        if (popupPanel != null) popupPanel.SetActive(true);

        // Đợi 1 thời gian đúng bằng popupDuration (1.5 giây)
        yield return new WaitForSeconds(popupDuration);

        // Tắt panel đi
        if (popupPanel != null) popupPanel.SetActive(false);
        _activePopup = null;
    }
}
