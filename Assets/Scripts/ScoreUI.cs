using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Quản lý toàn bộ giao diện người dùng (UI) trong quá trình chơi:
/// - Hiển thị điểm số hiện tại và kỷ lục (Best Score).
/// - Hiển thị các thông báo Popup tạm thời (Coffee, SkinUp, Bug).
/// </summary>
public class ScoreUI : MonoBehaviour
{
    /// <summary>
    /// Bản thực thi duy nhất của ScoreUI.
    /// </summary>
    public static ScoreUI Instance { get; private set; }

    [Header("Score UI")]
    [Tooltip("Text hiển thị điểm số hiện tại")]
    public TMP_Text scoreText;

    [Tooltip("Text hiển thị điểm kỷ lục (Tùy chọn)")]
    public TMP_Text highScoreText;

    [Header("Popup UI")]
    [Tooltip("Panel nền của các thông báo Popup")]
    public GameObject popupPanel;

    [Tooltip("Text nằm trong Popup")]
    public TMP_Text popupText;

    [Tooltip("Thời gian tồn tại của thông báo Popup")]
    public float popupDuration = 1.5f;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════

    private Coroutine _activePopup;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

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
        if (popupPanel != null) popupPanel.SetActive(false);
        
        UpdateScore(0);
        UpdateHighScore();
    }

    // ═══════════════════════════════════════════════════════════
    //  SCORE DISPLAY
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật điểm số hiện tại lên màn hình.
    /// </summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = "Code Commit: " + score;
    }

    /// <summary>
    /// Cập nhật điểm kỷ lục từ PlayerPrefs lên màn hình.
    /// </summary>
    private void UpdateHighScore()
    {
        if (highScoreText != null)
        {
            int best = PlayerPrefs.GetInt(Constants.GetHighScoreKey(), 0);
            highScoreText.text = "Best: " + best;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  POPUP NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Hiển thị thông báo ngắn gọn giữa màn hình với màu sắc tương ứng.
    /// </summary>
    public void ShowPopup(string message, Color textColor)
    {
        // Dừng popup cũ nếu đang hiển thị
        if (_activePopup != null) StopCoroutine(_activePopup);
        _activePopup = StartCoroutine(PopupRoutine(message, textColor));
    }

    private IEnumerator PopupRoutine(string message, Color textColor)
    {
        if (popupText != null) 
        {
            popupText.text = message;
            popupText.color = textColor;
        }

        if (popupPanel != null) popupPanel.SetActive(true);

        yield return new WaitForSeconds(popupDuration);

        if (popupPanel != null) popupPanel.SetActive(false);
        _activePopup = null;
    }
}

