using UnityEngine;

/// <summary>
/// Vật phẩm Cà phê (Boost): Giúp người chơi tăng tốc độ di chuyển tạm thời.
/// Hiệu ứng sẽ biến mất sau một khoảng thời gian quy định.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoffeeItem : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Settings")]
    [Tooltip("Hệ số nhân tốc độ (ví dụ 1.5 = tăng 50%)")]
    public float speedMultiplier = 1.5f;

    [Tooltip("Thời gian hiệu ứng kéo dài (giây)")]
    public float duration = 1.5f;

    [Header("VFX")]
    [Tooltip("Hiệu ứng hạt khi nhặt cà phê")]
    public GameObject collectParticlePrefab;

    [Tooltip("Thời gian tồn tại của hiệu ứng hạt")]
    public float particleLifetime = 1f;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════

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
        HandleCollection(other);
    }

    // ═══════════════════════════════════════════════════════════
    //  COLLECT LOGIC
    // ═══════════════════════════════════════════════════════════

    private void HandleCollection(Collider playerCollider)
    {
        PlayerController pc = playerCollider.GetComponent<PlayerController>();
        if (pc != null)
        {
            // PlayerController sẽ tự phát âm thanh cà phê trong hàm ApplySpeedBoost
            pc.ApplySpeedBoost(speedMultiplier, duration);
            
            // Hiển thị Popup thông báo
            Color coffeeBrown = new Color(0.6f, 0.3f, 0f);
            ScoreUI.Instance?.ShowPopup($"Speed x {speedMultiplier}!", coffeeBrown);
        }

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