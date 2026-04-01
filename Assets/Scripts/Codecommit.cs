// Thư viện cơ bản của C# cho Unity — cần cho Coroutine và các collection chuẩn
using System.Collections;
using System.Collections.Generic;

// Thư viện Unity Engine — cần cho MonoBehaviour, GameObject, Collider, v.v.
using UnityEngine;

/// <summary>
/// CodeCommit: Item mà Player chạm vào để nhận điểm.
/// Khi Player va chạm → cộng điểm, phát hiệu ứng âm thanh + particle, rồi trả về pool.
/// </summary>

// Bắt buộc phải có component Collider trên cùng GameObject này.
// Nếu chưa có, Unity sẽ tự thêm vào khi gắn script.
[RequireComponent(typeof(Collider))]
public class CodeCommit : MonoBehaviour
{
    // ─── Cài đặt ────────────────────────────────────────────────
    // [Header] tạo tiêu đề nhóm trong Inspector cho dễ nhìn
    [Header("Settings")]

    // Số điểm cộng vào khi Player nhặt item này. Có thể chỉnh trong Inspector.
    public int scoreValue = 10;

    // ─── Hiệu ứng hình ảnh (VFX) ────────────────────────────────
    [Header("VFX")]

    // Prefab particle (hiệu ứng hạt) sẽ được tạo ra tại vị trí item khi nhặt.
    // Kéo prefab particle vàng vào đây trong Inspector.
    public GameObject collectParticlePrefab;

    // Thời gian tồn tại (giây) của particle trước khi tự động bị xóa.
    public float particleLifetime = 1.5f;

    // ─── Âm thanh (SFX) ─────────────────────────────────────────
    [Header("Audio")]

    // Âm thanh phát ra khi Player nhặt item. Kéo file audio vào đây trong Inspector.
    public AudioClip collectSound;

    // ─── Biến nội bộ ────────────────────────────────────────────

    // Cờ kiểm tra: đã được nhặt chưa? Tránh bị kích hoạt 2 lần trong cùng 1 frame.
    private bool _collected = false;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    // Start() chạy 1 lần duy nhất ngay khi GameObject này được kích hoạt (SetActive true)
    private void Start()
    {
        // Lấy component Collider trên cùng GameObject và set thành trigger.
        // isTrigger = true: collider chỉ phát hiện va chạm (không chặn vật lý),
        // khi đó Unity gọi OnTriggerEnter thay vì OnCollisionEnter.
        GetComponent<Collider>().isTrigger = true;
    }

    // OnTriggerEnter được Unity tự gọi khi một Collider khác đi vào vùng trigger này.
    // Tham số 'other': Collider của object vừa chạm vào.
    private void OnTriggerEnter(Collider other)
    {
        // Nếu item đã được nhặt rồi → thoát ngay, không xử lý tiếp để tránh trùng lặp.
        if (_collected) return;

        // CompareTag: kiểm tra tag của object chạm vào. Nhanh hơn dùng .tag == "Player".
        // Nếu không phải Player → bỏ qua (Bot, NPC, v.v. sẽ không nhặt được item).
        if (!other.CompareTag("Player")) return;

        // Đánh dấu đã collected để block các lần trigger tiếp theo
        _collected = true;

        // Gọi hàm xử lý việc nhặt item
        Collect();
    }

    // Hàm xử lý toàn bộ logic khi Player nhặt item
    private void Collect()
    {
        // Gọi AddScore trên GameManager singleton để cộng điểm.
        // ?. là null-conditional operator: nếu GameManager.Instance == null thì bỏ qua,
        // không gây lỗi NullReferenceException.
        GameManager.Instance?.AddScore(scoreValue);

        // Tạo hiệu ứng particle tại vị trí của item này
        if (collectParticlePrefab != null) // Chỉ tạo nếu đã kéo prefab vào Inspector
        {
            // Instantiate: tạo 1 bản sao của prefab particle tại vị trí item,
            // với góc xoay mặc định (Quaternion.identity = không xoay).
            GameObject fx = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);

            // Tự động xóa particle sau 'particleLifetime' giây để không tốn bộ nhớ.
            Destroy(fx, particleLifetime);
        }

        // Phát âm thanh tại vị trí item trong không gian 3D (có pan trái/phải theo vị trí).
        // PlayClipAtPoint tự tạo AudioSource tạm, phát xong tự xóa.
        if (collectSound != null) // Chỉ phát nếu đã kéo audio clip vào Inspector
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // Trả gameObject này về ObjectPool thay vì Destroy().
        // Pool sẽ SetActive(false) và cất vào Queue để tái sử dụng lần sau.
        // ?. tránh lỗi nếu ItemManager chưa được tạo.
        ItemManager.Instance?.ReturnToPool(gameObject);
    }
}