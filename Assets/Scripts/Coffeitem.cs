using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CoffeeItem: Player nhặt cà phê → tốc độ tăng tạm thời trong vài giây.
/// Không tăng vĩnh viễn như SkinUp — chỉ tăng trong 'duration' giây rồi về lại bình thường.
/// </summary>
[RequireComponent(typeof(Collider))] // Bắt buộc phải có Collider trên GameObject này
public class CoffeeItem : MonoBehaviour
{
    // ─── Cài đặt ────────────────────────────────────────────────
    [Header("Settings")]

    // Hệ số nhân tốc độ khi uống cà phê. 1.5 = tăng 50% tốc độ trong 'duration' giây.
    public float speedMultiplier = 1.5f;

    // Thời gian (giây) hiệu ứng tốc độ kéo dài trước khi về lại bình thường.
    public float duration = 1.5f;

    // ─── Hiệu ứng hình ảnh ──────────────────────────────────────
    [Header("VFX")]

    // Prefab particle phát ra khi nhặt (vd: hiệu ứng cà phê sủi bọt).
    public GameObject collectParticlePrefab;

    // Thời gian particle tồn tại trước khi tự xóa.
    public float particleLifetime = 1f;

    // ─── Âm thanh ───────────────────────────────────────────────
    [Header("Audio")]

    // Âm thanh phát ra khi nhặt cà phê.
    public AudioClip collectSound;

    // ─── Biến nội bộ ────────────────────────────────────────────

    // Cờ chống trigger 2 lần trong cùng 1 frame.
    private bool _collected = false;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    // Chạy khi GameObject được kích hoạt lần đầu
    private void Start()
    {
        // Đảm bảo collider là trigger (phát hiện va chạm, không chặn physics)
        GetComponent<Collider>().isTrigger = true;
    }

    // Unity gọi hàm này khi một Collider khác chạy vào vùng trigger của item này
    private void OnTriggerEnter(Collider other)
    {
        // Nếu đã nhặt rồi → bỏ qua (tránh double trigger)
        if (_collected) return;

        // Chỉ phản ứng với tag "Player", bỏ qua Bot và các object khác
        if (!other.CompareTag("Player")) return;

        // Đánh dấu đã collect
        _collected = true;

        // Lấy component PlayerController từ object Player vừa chạm vào.
        // GetComponent<T>(): tìm component kiểu T trên cùng GameObject với 'other'.
        PlayerController pc = other.GetComponent<PlayerController>();

        // Gọi hàm tăng tốc nếu tìm thấy PlayerController (tránh null error)
        if (pc != null)
        {
            // Truyền hệ số nhân và thời gian vào PlayerController để xử lý coroutine
            pc.ApplySpeedBoost(speedMultiplier, duration);
            
            // ── Hiện popup thông báo ──────────────────────────────
            // Sử dụng mã màu RBG cho màu Nâu (vd: R=0.6, G=0.3, B=0)
            Color brownColor = new Color(0.6f, 0.3f, 0f);
            ScoreUI.Instance?.ShowPopup($"Speed x {speedMultiplier}!", brownColor);
        }

        // Tạo particle hiệu ứng tại vị trí item
        if (collectParticlePrefab != null)
        {
            // Instantiate = tạo bản sao prefab. Quaternion.identity = không xoay (thẳng đứng)
            GameObject fx = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);

            // Đặt hẹn giờ xóa particle sau 'particleLifetime' giây
            Destroy(fx, particleLifetime);
        }

        // Phát âm thanh 3D tại vị trí item (tự tạo AudioSource rồi tự xóa)
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // Trả item về ObjectPool để tái sử dụng (không Destroy thật = tiết kiệm CPU)
        ItemManager.Instance?.ReturnToPool(gameObject);
    }
}