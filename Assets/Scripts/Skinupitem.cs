using UnityEngine;

/// <summary>
/// Vật phẩm Nâng cấp (SkinUp): Giúp người chơi tăng tốc độ di chuyển vĩnh viễn
/// và thăng cấp ngoại hình (Skin) lên bậc tiếp theo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SkinUpItem : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Speed Boost")]
    [Tooltip("Hệ số nhân tốc độ vĩnh viễn (ví dụ 1.3 = tăng 30%)")]
    public float speedMultiplier = 1.15f;

    [Header("VFX")]
    [Tooltip("Hiệu ứng hạt khi nâng cấp thành công")]
    public GameObject collectParticlePrefab;

    [Tooltip("Thời gian tồn tại của hiệu ứng hạt")]
    public float particleLifetime = 1.5f;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════

    private static readonly string[] _SKIN_LEVEL_NAMES = { "Intern", "Fresher", "Junior", "Senior Dev" };
    private bool _isCollected = false;
    private Collider _collider;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    /// <summary>
    /// Reset trạng thái khi Item được tái sử dụng từ Pool.
    /// </summary>
    private void OnEnable()
    {
        _isCollected = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCollected) return;
        if (!other.CompareTag(Constants.TAG_PLAYER)) return;

        _isCollected = true;
        HandleUpgrade(other);
    }

    // ═══════════════════════════════════════════════════════════
    //  UPGRADE LOGIC
    // ═══════════════════════════════════════════════════════════

    private void HandleUpgrade(Collider playerCollider)
    {
        PlayerController pc = playerCollider.GetComponent<PlayerController>();
        if (pc != null)
        {
            // Tăng tốc độ vĩnh viễn
            pc.speed *= speedMultiplier;

            // PlayerController sẽ tự phát âm thanh SkinUp trong hàm UpgradeSkin
            pc.UpgradeSkin(1f);

            // Hiển thị thông báo cấp bậc mới
            int newIndex = pc.SkinIndex; 
            string levelName = (newIndex < _SKIN_LEVEL_NAMES.Length) ? _SKIN_LEVEL_NAMES[newIndex] : "God Tier";
            ScoreUI.Instance?.ShowPopup($"LEVEL UP: {levelName}!", Color.green);
        }

        // Thông báo ItemManager để theo dõi số lượng SkinUp đã xuất hiện
        ItemManager.Instance?.OnSkinUpCollected();

        SpawnVFX();

        // Thu hồi về Pool
        ItemManager.Instance?.ReturnToPool(gameObject);
    }

    private void SpawnVFX()
    {
        if (collectParticlePrefab != null)
        {
            GameObject fx = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
            Destroy(fx, particleLifetime);
        }
    }
}