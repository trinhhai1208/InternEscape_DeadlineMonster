using UnityEngine;

/// <summary>
/// Vật phẩm Code Commit: Người chơi thu thập để tích lũy điểm số.
/// Tự động trả về Object Pool sau khi được thu thập.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CodeCommit : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Settings")]
    [Tooltip("Số điểm cộng vào khi thu thập")]
    public int scoreValue = 10;

    [Header("VFX")]
    [Tooltip("Hiệu ứng hạt khi thu thập thành công")]
    public GameObject collectParticlePrefab;
    
    [Tooltip("Thời gian tồn tại của hiệu ứng hạt")]
    public float particleLifetime = 1.5f;

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
    /// Reset trạng thái khi Item được lấy ra từ Pool.
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
        Collect();
    }

    // ═══════════════════════════════════════════════════════════
    //  COLLECT LOGIC
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Xử lý logic cộng điểm, tạo hiệu ứng và thu hồi vật phẩm.
    /// </summary>
    private void Collect()
    {
        // GameManager sẽ tự phát âm thanh ItemCodeCommit trong hàm AddScore
        GameManager.Instance?.AddScore(scoreValue);

        SpawnVFX();

        // Thu hồi về Pool để tái sử dụng
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