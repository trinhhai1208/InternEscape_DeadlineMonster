using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý trung tâm của toàn bộ trò chơi: điểm số, kỷ lục (High Score), 
/// trạng thái game (Intro/Playing/GameOver) và chuyển đổi màn chơi.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Bản thực thi duy nhất của GameManager trong Scene.
    /// </summary>
    public static GameManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════
    //  UI REFERENCES
    // ═══════════════════════════════════════════════════════════

    [Header("UI References")]
    [Tooltip("Text hiển thị thông báo trạng thái thắng/thua")]
    public TMP_Text statusText;
    
    [Tooltip("Text hiển thị riêng khi thắng (tùy chọn)")]
    public TMP_Text winStatusText;

    [Header("Menu Panels")]
    [Tooltip("Panel hiện ra khi người chơi thua cuộc")]
    public GameObject gameOverPanel;
    
    [Tooltip("Panel hiện ra khi người chơi về đích")]
    public GameObject winGamePanel;

    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector (Giữ nguyên tên cũ)
    // ═══════════════════════════════════════════════════════════

    [Header("Cinematic Intro Settings")]
    public Transform playerTransform;
    public Transform bossTransform;
    public CinemachineCameraSetup cameraSetup;
    
    [Tooltip("Cờ báo hiệu game đang trong quá trình chạy (sau Intro)")]
    public bool isGamePlaying = false;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═══════════════════════════════════════════════════════════

    private int _score = 0;
    private int _highScore = 0;
    private bool _hasSkinUp = false;
    private float _skinUpSpeedMultiplier = 1f;
    private int _currentSkinLevel = 0;

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
        LoadHighScore();
        UpdateScoreUI();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winGamePanel != null) winGamePanel.SetActive(false);

        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayInGameMusic();
        }

        StartCoroutine(IntroSequence());
    }

    // ═══════════════════════════════════════════════════════════
    //  HIGH SCORE & SAVE LOGIC
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tải điểm kỷ lục từ bộ nhớ máy (PlayerPrefs).
    /// </summary>
    private void LoadHighScore()
    {
        _highScore = PlayerPrefs.GetInt(Constants.GetHighScoreKey(), 0);
        Debug.Log("[GameManager] High Score Loaded: " + _highScore);
    }

    /// <summary>
    /// Kiểm tra và lưu kỷ lục mới nếu điểm hiện tại cao hơn điểm cũ.
    /// </summary>
    private void CheckAndSaveHighScore()
    {
        if (_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt(Constants.GetHighScoreKey(), _highScore);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] New High Score Saved: " + _highScore);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  CINEMATIC INTRO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Coroutine xử lý đoạn cắt cảnh Intro trước khi vào game.
    /// </summary>
    private IEnumerator IntroSequence()
    {
        isGamePlaying = false;
        yield return new WaitForEndOfFrame();

        // 1. Quét nhân vật chính (Player)
        if (cameraSetup != null && playerTransform != null)
            cameraSetup.StartCinematicPan(playerTransform, 3.0f);
        yield return new WaitForSeconds(3.0f);

        // 2. Quét kẻ địch (Boss)
        if (cameraSetup != null && bossTransform != null)
            cameraSetup.StartCinematicPan(bossTransform, 3.0f);
        yield return new WaitForSeconds(3.0f);

        // 3. Trả Camera về góc nhìn thứ 3 bám theo Player
        if (cameraSetup != null && playerTransform != null)
            cameraSetup.ResetToPlayer(playerTransform);
        yield return new WaitForSeconds(1.5f);

        isGamePlaying = true;
        Debug.Log("[GameManager] Game Start!");
    }

    // ═══════════════════════════════════════════════════════════
    //  SCORE MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cộng điểm cho người chơi khi thu hoạch vật phẩm.
    /// </summary>
    public void AddScore(int amount)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayItemCodeCommit();
        _score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        ScoreUI.Instance?.UpdateScore(_score);
    }

    // ═══════════════════════════════════════════════════════════
    //  SKIN PROGRESSION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Ghi nhận khi nhân vật nhặt được skin mới.
    /// </summary>
    public void OnSkinUpCollected(int skinLevel, float speedMultiplier)
    {
        _hasSkinUp = true;
        _currentSkinLevel = skinLevel;
        _skinUpSpeedMultiplier = speedMultiplier;
        Debug.Log("[GameManager] SkinUp: Level " + skinLevel);
    }

    /// <summary>
    /// Lấy hệ số nhân tốc độ từ các skin đã nhặt được.
    /// </summary>
    public float GetEndgameSpeedMultiplier()
    {
        return _hasSkinUp ? _skinUpSpeedMultiplier : 1f;
    }

    // ═══════════════════════════════════════════════════════════
    //  GAME STATE CONTROL
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Dừng trò chơi và hiển thị kết quả Thắng/Thua.
    /// </summary>
    public void GameOver(bool isWin = false)
    {
        ItemManager.Instance?.StopSpawning();
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();

        // Kiểm tra và lưu điểm kỷ lục ngay khi kết thúc
        CheckAndSaveHighScore();

        if (isWin)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin();
            if (winGamePanel != null) winGamePanel.SetActive(true);
            
            string winMsg = $"YOU WIN!\nScore: {_score}\nBest: {_highScore}";
            if (winStatusText != null) winStatusText.text = winMsg;
            else if (statusText != null) statusText.text = winMsg;
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLose();
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            
            if (statusText != null) 
                statusText.text = $"GAME OVER!\nDEADLINE CAUGHT YOU!\nScore: {_score}\nBest: {_highScore}";
        }

        Time.timeScale = 0f;
        Debug.Log("[GameManager] Game Over. Final Score: " + _score);
    }

    /// <summary>
    /// Làm mới và chơi lại màn hiện tại.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (cameraSetup != null) cameraSetup.CleanupBeforeRestart();
        StartCoroutine(RestartAfterCleanup());
    }

    private IEnumerator RestartAfterCleanup()
    {
        yield return null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Thoát khỏi màn chơi hiện tại và quay về Main Menu.
    /// </summary>
    public void GoToMainMenu(string menuSceneName)
    {
        Time.timeScale = 1f;
        if (cameraSetup != null) cameraSetup.CleanupBeforeRestart();
        MainMenuManager.jumpToMapSelection = true;
        StartCoroutine(ReturnToMenuAfterCleanup(menuSceneName));
    }

    private IEnumerator ReturnToMenuAfterCleanup(string menuSceneName)
    {
        yield return null;
        SceneManager.LoadScene(menuSceneName);
    }
}
