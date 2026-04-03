using UnityEngine;

/// <summary>
/// Chướng ngại vật (Obstacle): Đại diện cho các "Bug" trên đường chạy.
/// Khi va chạm, người chơi sẽ bị giảm tốc độ di chuyển trong một khoảng thời gian.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Settings")]
    [Tooltip("Hệ số giảm tốc (ví dụ 0.5 = giảm 50% tốc độ gốc)")]
    public float slowMultiplier = 0.5f;

    [Tooltip("Thời gian hiệu ứng giảm tốc kéo dài (giây)")]
    public float slowDuration = 2f;

    [Header("VFX")]
    [Tooltip("Hiệu ứng hạt khi va chạm với Bug")]
    public GameObject hitParticlePrefab;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════

    private bool _isHit = false;
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
    /// Reset trạng thái va chạm khi được lấy ra từ Pool.
    /// </summary>
    private void OnEnable()
    {
        _isHit = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isHit) return;
        if (!other.CompareTag(Constants.TAG_PLAYER)) return;

        _isHit = true;
        HandleHit(other);
    }

    // ═══════════════════════════════════════════════════════════
    //  HIT LOGIC
    // ═══════════════════════════════════════════════════════════

    private void HandleHit(Collider playerCollider)
    {
        PlayerController pc = playerCollider.GetComponent<PlayerController>();
        
        // PlayerController sẽ tự phát âm thanh Bug trong hàm ApplyObstacleSlow
        pc?.ApplyObstacleSlow(slowMultiplier, slowDuration);
        
        // Hiển thị Popup thông báo
        ScoreUI.Instance?.ShowPopup("Bạn đã gặp Bug!", Color.red);

        SpawnVFX();
        
        // Thu hồi về Pool để tái sử dụng
        ItemManager.Instance?.ReturnToPool(gameObject);
    }

    private void SpawnVFX()
    {
        if (hitParticlePrefab != null)
        {
            GameObject fx = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1f);
        }
    }
}