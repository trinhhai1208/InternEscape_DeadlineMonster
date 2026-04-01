using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SkinUpItem: Khi Player nhặt → tốc độ tăng vĩnh viễn x1.3 và đổi sang skin tiếp theo.
/// Khác với Coffee (tạm thời), SkinUp tăng tốc mãi đến cuối game.
/// </summary>
[RequireComponent(typeof(Collider))] // Bắt buộc có Collider để detect va chạm
public class SkinUpItem : MonoBehaviour
{
    // ─── Speed Boost ─────────────────────────────────────────────
    [Header("Speed Boost")]

    // [Tooltip]: text giải thích hiện khi hover chuột vào field trong Inspector
    [Tooltip("Nhân tốc độ player khi collect. Mặc định 1.3 = tăng 30%")]

    // Hệ số nhân tốc độ vĩnh viễn. 1.3 = tăng 30%. Có thể chỉnh trong Inspector.
    public float speedMultiplier = 1.3f;

    // ─── Hiệu ứng hình ảnh ──────────────────────────────────────
    [Header("VFX")]

    // Prefab particle hiệu ứng khi nhặt (vd: ánh sáng, tia sét nhỏ).
    public GameObject collectParticlePrefab;

    // Thời gian particle tồn tại (giây) trước khi tự xóa.
    public float particleLifetime = 1.5f;

    // ─── Âm thanh ───────────────────────────────────────────────
    [Header("Audio")]

    // Âm thanh phát khi nhặt SkinUp (vd: tiếng "level up").
    public AudioClip collectSound;

    // ─── Biến nội bộ ────────────────────────────────────────────

    // Tên level ứng với index của Skin (0: Intern, 1: Fresher, v.v.)
    private static readonly string[] skinLevelNames = { "Intern", "Fresher", "Junior", "Senior Dev" };

    // Cờ tránh trigger 2 lần. Khác CodeCommit dùng _collected (với _) để phân biệt style.
    private bool collected = false;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    // Chạy khi GameObject được kích hoạt
    private void Start()
    {
        // Set trigger để phát hiện va chạm mà không chặn vật lý
        GetComponent<Collider>().isTrigger = true;
    }

    // Unity gọi khi Player (hoặc bất kỳ object nào) đi vào vùng trigger
    private void OnTriggerEnter(Collider other)
    {
        // Nếu đã collect rồi → thoát (tránh double trigger trong cùng frame)
        if (collected) return;

        // Chỉ Player mới được nhặt, bỏ qua Bot và object khác
        if (!other.CompareTag("Player")) return;

        // Đánh dấu đã collect để block các trigger tiếp theo
        collected = true;

        // Lấy PlayerController từ Player object
        PlayerController pc = other.GetComponent<PlayerController>();

        if (pc != null) // Bảo vệ null: chỉ xử lý nếu tìm thấy PlayerController
        {
            // Nhân trực tiếp vào pc.speed — tăng vĩnh viễn (không phải coroutine có giới hạn).
            // VD: speed = 10, speedMultiplier = 1.3 → speed mới = 13.
            pc.speed *= speedMultiplier;

            // $"" là string interpolation của C# — chèn giá trị biến vào chuỗi.
            // In log để debug: xem tốc độ mới là bao nhiêu sau khi nhặt.
            Debug.Log($"[SkinUp] Player speed → {pc.speed}");

            // Gọi UpgradeSkin để đổi visual model sang skin tiếp theo.
            // Truyền 1f (hệ số = 1, không đổi) để GameManager không lưu thêm speed multiplier.
            pc.UpgradeSkin(1f);

            // ── Hiện popup theo Level ────────────────────────────────
            int newIndex = pc.SkinIndex; 
            string levelName = (newIndex < skinLevelNames.Length) ? skinLevelNames[newIndex] : "God Tier";
            ScoreUI.Instance?.ShowPopup($"LEVEL UP: {levelName}!", Color.green);
        }

        // Thông báo cho ItemManager biết có 1 SkinUp đã được collect
        // (để tracking số lần SkinUp đã dùng)
        ItemManager.Instance?.OnSkinUpCollected();

        // Tạo hiệu ứng particle tại vị trí item
        if (collectParticlePrefab != null)
        {
            // Tạo bản sao prefab particle tại đây, không xoay
            GameObject fx = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);

            // Đặt timer tự xóa sau particleLifetime giây
            Destroy(fx, particleLifetime);
        }

        // Phát âm thanh 3D tại vị trí item trong không gian
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // Trả gameObject về pool để tái sử dụng thay vì bị Destroy hẳn
        ItemManager.Instance?.ReturnToPool(gameObject);
    }
}