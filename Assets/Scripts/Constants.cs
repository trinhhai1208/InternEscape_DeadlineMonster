using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý toàn bộ hằng số (Tags, Layers, Keys) để tránh lỗi chính tả string.
/// </summary>
public static class Constants
{
    // 🏷️ TAGS
    public const string TAG_PLAYER = "Player";
    public const string TAG_FINISH = "Finish";
    public const string TAG_OBSTACLE = "Obstacle";

    // 💾 PLAYER PREFS KEYS (Sử dụng Prefix để tạo Key động theo màn)
    private const string KEY_HIGH_SCORE_PREFIX = "HighScore_DeadlineMonster";

    /// <summary>
    /// Tạo Key lưu trữ Điểm cao dựa trên tên của màn chơi hiện tại.
    /// Ví dụ: "HighScore_DeadlineMonster_Map1"
    /// </summary>
    public static string GetHighScoreKey()
    {
        return $"{KEY_HIGH_SCORE_PREFIX}_{SceneManager.GetActiveScene().name}";
    }

    // 🎮 ANIMATOR PARAMETERS (Hashes để tối ưu hiệu năng)
    public static readonly int ANIM_SPEED = Animator.StringToHash("Speed");
    public static readonly int ANIM_IS_SLOWED = Animator.StringToHash("IsSlowed");
}

