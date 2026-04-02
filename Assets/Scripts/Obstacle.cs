using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Obstacle: Vật cản trên đường — Player chạm vào bị giảm tốc độ trong vài giây.
/// Obstacle không bị xóa khi Player chạm (chỉ apply slow rồi tiếp tục tồn tại).
/// </summary>
[RequireComponent(typeof(Collider))] // Bắt buộc có Collider để phát hiện va chạm
public class Obstacle : MonoBehaviour
{
    // ─── Cài đặt ────────────────────────────────────────────────
    [Header("Settings")]

    // Hệ số giảm tốc khi Player va chạm. 0.5 = còn 50% tốc độ gốc.
    // Có thể chỉnh trong Inspector từ 0 (đứng yên) đến 1 (không giảm).
    public float slowMultiplier = 0.5f;

    // Thời gian (giây) hiệu ứng giảm tốc kéo dài trước khi Player phục hồi tốc độ.
    public float slowDuration = 2f;

    // ─── Hiệu ứng hình ảnh ──────────────────────────────────────
    [Header("VFX")]

    // Prefab particle (vd: tia lửa, khói) tạo ra khi Player đâm vào obstacle.
    // Để trống nếu không cần hiệu ứng.
    public GameObject hitParticlePrefab;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    // Cờ chống chạm 2 lần trong cùng 1 frame (giống Coffee và CodeCommit)
    private bool _hit = false;

    // Chạy khi GameObject lấy ra từ Object Pool để dùng lại
    private void OnEnable()
    {
        _hit = false;
    }

    // Chạy khi GameObject kích hoạt — đảm bảo collider là trigger
    private void Start()
    {
        // isTrigger = true: Collider không chặn vật lý, chỉ phát sự kiện OnTriggerEnter.
        // Nhờ đó Player đi xuyên qua obstacle (không bị chặn đứng) nhưng vẫn nhận slow.
        GetComponent<Collider>().isTrigger = true;
    }

    // Unity gọi khi một Collider khác đi vào vùng trigger của obstacle
    private void OnTriggerEnter(Collider other)
    {
        // Bỏ qua nếu đã chạm rồi
        if (_hit) return;

        // Bỏ qua nếu không phải Player (Bot không bị slow khi chạm obstacle)
        if (!other.CompareTag("Player")) return;

        _hit = true;

        // Lấy PlayerController từ Player để gọi hàm giảm tốc
        PlayerController pc = other.GetComponent<PlayerController>();

        // ?. là null-conditional: nếu pc == null thì không gọi gì cả (tránh crash)
        // ApplyObstacleSlow sẽ bắt đầu coroutine giảm tốc trong PlayerController
        pc?.ApplyObstacleSlow(slowMultiplier, slowDuration);
        
        // ── Hiện popup thông báo Bug ────────────────────────────────
        ScoreUI.Instance?.ShowPopup("Bạn đã gặp Bug!", Color.red);

        // Tạo particle hiệu ứng va chạm tại vị trí obstacle
        if (hitParticlePrefab != null) // Chỉ tạo nếu đã assign prefab
        {
            // Tạo particle tại đây, không xoay
            GameObject fx = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

            // Xóa particle sau 1 giây (hardcode — ngắn hơn của item vì chỉ là va chạm)
            Destroy(fx, 1f);
        }
        
        // ── CẬP NHẬT MỚI: Trả về Object Pool để Error biến mất ────────────────
        ItemManager.Instance?.ReturnToPool(gameObject);
    }
}