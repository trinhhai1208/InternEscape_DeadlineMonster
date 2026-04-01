using Cinemachine;
using System.Collections;
using System.Collections.Generic;
// TMPro: TextMeshPro — thư viện text đẹp hơn Unity UI Text mặc định, cần import qua Package Manager
using TMPro;
using UnityEngine;
// Thêm thư viện quản lý màn chơi (Scene) để dùng hàm Reload Scene
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager: Singleton quản lý trung tâm — điểm số, trạng thái game, SkinUp tracking.
/// Singleton pattern: chỉ có 1 instance duy nhất, truy cập từ mọi nơi qua GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════

    // static: biến thuộc về class, không thuộc về instance cụ thể nào.
    // Instance có thể truy cập từ bất kỳ đâu: GameManager.Instance.AddScore(10).
    // { get; private set; }: đọc được từ ngoài (public get), nhưng chỉ class này mới ghi được.
    public static GameManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════
    //  UI REFERENCES
    // ═══════════════════════════════════════════════════════════

    [Header("UI")]
    // Text hiện trạng thái: "Game Over!", "You Win!" v.v.
    public TMP_Text statusText;

    [Header("Menu Panels")]
    // Panel chứa nút Restart và dòng chữ Game Over khi hết game
    public GameObject gameOverPanel;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═══════════════════════════════════════════════════════════

    [Header("Cinematic Intro Settings")]
    public Transform playerTransform;
    public Transform bossTransform;
    public CinemachineCameraSetup cameraSetup;
    
    // Cờ báo hiệu đánh dấu khi nào 2s Intro quay Cutscene xong mới thả cho 2 bên chạy
    public bool isGamePlaying = false;

    // Điểm số hiện tại của Player. Tăng khi nhặt CodeCommit.
    private int score = 0;

    // Cờ đánh dấu Player đã nhặt ít nhất 1 SkinUp chưa
    private bool hasSkinUp = false;

    // Hệ số speed multiplier từ SkinUp — lưu để apply ở endgame nếu cần
    private float skinUpSpeedMultiplier = 1f;

    // Level skin hiện tại (0 = Intern, 1 = Fresher, 2 = Junior, 3 = Senior)
    private int currentSkinLevel = 0;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    // Awake(): chạy TRƯỚC Start(), dùng để setup Singleton
    void Awake()
    {
        // Kiểm tra nếu đã có Instance khác tồn tại (vd: khi load scene mới)
        if (Instance != null && Instance != this)
        {
            // Xóa bản sao thừa để chỉ giữ lại Instance gốc
            Destroy(gameObject);
            return; // Thoát để không chạy tiếp
        }

        // Gán chính mình làm Instance toàn cục
        Instance = this;
    }

    // Start() chạy sau Awake() — khởi tạo UI ban đầu
    void Start()
    {
        // Hiển thị điểm ban đầu (= 0) lên UI ngay khi game bắt đầu
        UpdateScoreUI();

        // Đảm bảo ẩn màn hình GameOver
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Bắt buộc nhịp tg phải = 1 để cho Cinemachine Camera hoạt động mượt
        Time.timeScale = 1f;

        // Bắt đầu chạy Nháy góc quay 2s Intro Căng Thẳng
        StartCoroutine(IntroSequence());
    }

    private System.Collections.IEnumerator IntroSequence()
    {
        // Chặn không cho Player và Boss Nhúc nhích
        isGamePlaying = false;

        // Đợi 1 nhịp (frame) để script Cinemachine tự Reset mục tiêu về Player xong xuôi, sau đó mình mới can thiệp
        yield return new WaitForEndOfFrame();

        // 1. Máy quay phim hành động quét PLAYER (Độ dài 3s)
        if (cameraSetup != null && playerTransform != null)
        {
            cameraSetup.StartCinematicPan(playerTransform, 3.0f);
        }
        yield return new WaitForSeconds(3.0f);

        // 2. Chuyển cảnh đột ngột (Hard Cut): Quét BOSS (Độ dài 3s)
        if (cameraSetup != null && bossTransform != null)
        {
            cameraSetup.StartCinematicPan(bossTransform, 3.0f);
        }
        yield return new WaitForSeconds(3.0f);

        // 3. Giật về lại sau lưng Player như cũ (chờ 1.5s nghỉ để lấy hồn)
        if (cameraSetup != null && playerTransform != null)
        {
            cameraSetup.ResetToPlayer(playerTransform);
        }
        yield return new WaitForSeconds(1.5f);

        // 3. Chính Thức Thả Xích Khai Cuộc Bỏ Trốn
        isGamePlaying = true;
        Debug.Log("GO! Bắt đầu chạy đua deadline!");
    }

    // ═══════════════════════════════════════════════════════════
    //  SCORE
    // ═══════════════════════════════════════════════════════════

    // Gọi từ CodeCommit khi Player nhặt item — cộng điểm
    public void AddScore(int amount)
    {
        // += : cộng dồn vào điểm hiện tại
        score += amount;

        // Cập nhật text UI ngay sau khi điểm thay đổi
        UpdateScoreUI();
    }

    // Hàm private nội bộ — cập nhật UI text với điểm hiện tại
    private void UpdateScoreUI()
    {
        // Nhờ ScoreUI quản lý hiển thị
        ScoreUI.Instance?.UpdateScore(score);
    }

    // ═══════════════════════════════════════════════════════════
    //  SKIN UP TRACKING
    // ═══════════════════════════════════════════════════════════

    // Gọi từ PlayerController.UpgradeSkin() khi Player nhặt SkinUp
    public void OnSkinUpCollected(int skinLevel, float speedMultiplier)
    {
        // Đánh dấu đã có SkinUp (dùng để GetEndgameSpeedMultiplier)
        hasSkinUp = true;

        // Lưu level skin mới nhất
        currentSkinLevel = skinLevel;

        // Lưu hệ số speed để apply sau (nếu cần endgame bonus)
        skinUpSpeedMultiplier = speedMultiplier;

        // Log để debug — không hiện trên UI
        Debug.Log("[GameManager] SkinUp level=" + skinLevel + " mult=" + speedMultiplier);
    }

    // Trả về hệ số speed để PlayerController apply endgame bonus
    public float GetEndgameSpeedMultiplier()
    {
        // Toán tử 3 ngôi (ternary): nếu hasSkinUp = true → trả multiplier, ngược lại → 1f (không đổi)
        return hasSkinUp ? skinUpSpeedMultiplier : 1f;
    }

    // ═══════════════════════════════════════════════════════════
    //  GAME STATE
    // ═══════════════════════════════════════════════════════════

    // Gọi khi game kết thúc (thua hoặc hết time)
    // isWin = true nếu Player thắng, false nếu thua
    public void GameOver(bool isWin = false)
    {
        // Dừng spawn item mới (hàm rỗng hiện tại — có thể mở rộng sau)
        ItemManager.Instance?.StopSpawning();

        // Kích hoạt panel Game Over khi thua/thắng
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Hiện thông báo Game Over kèm điểm cuối
        if (statusText != null)
        {
            if (isWin) statusText.text = "YOU WIN! \nScore: " + score;
            else statusText.text = "GAME OVER! DEADLINE WINS YOU";
        }

        // Tự động dừng mọi thứ khi Game Over
        Time.timeScale = 0f;

        Debug.Log("[GameManager] GAME OVER - Score: " + score);
    }

    // Gắn hàm này vào sự kiện OnClick của nút [Restart]
    public void RestartGame()
    {
        Time.timeScale = 1f;

        // ✅ Cleanup camera TRƯỚC khi load scene
        if (cameraSetup != null)
        {
            cameraSetup.CleanupBeforeRestart();
        }

        // Đợi 1 frame để Destroy() bossCam xử lý xong rồi mới load scene
        StartCoroutine(RestartAfterCleanup());
    }

    private IEnumerator RestartAfterCleanup()
    {
        yield return null; // 1 frame để Destroy() flush xong

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}