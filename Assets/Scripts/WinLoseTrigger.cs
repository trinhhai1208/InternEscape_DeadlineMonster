using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WinLoseTrigger: Trigger ở cuối path xác định kết quả game.
/// - Player chạm trước → WIN
/// - Bot chạm trước → LOSE
/// Đặt script này lên một GameObject có Collider ở vị trí đích cuối đường.
/// </summary>
public class WinLoseTrigger : MonoBehaviour
{
    // ─── Unity Lifecycle ─────────────────────────────────────────

    // Start() chạy 1 lần khi GameObject kích hoạt
    private void Start()
    {
        // Lấy Collider trên GameObject này (EndPoint trigger box)
        var col = GetComponent<Collider>();

        // Nếu có Collider → đảm bảo là trigger (phát hiện va chạm, không chặn di chuyển)
        // 'var' = kiểu được tự động suy luận (ở đây là Collider)
        if (col != null) col.isTrigger = true;
    }

    // Unity gọi khi một Collider khác đi vào vùng trigger này
    // Tham số 'other': Collider của object vừa chạm vào EndPoint
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu object chạm vào là Player (về đích trước Bot)
        if (other.CompareTag("Player"))
        {
            // In log màu trắng để báo WIN — dùng Debug.Log (không phải warning/error)
            Debug.Log("===== WIN! Intern đã chiến thắng =====");

            // Báo GameManager Player Win
            GameManager.Instance.GameOver(true);
        }
        // Kiểm tra nếu object chạm vào là Bot (Bot về đích trước Player → LOSE)
        else if (other.CompareTag("Bot"))
        {
            // In log màu vàng (warning) để báo LOSE
            Debug.LogWarning("===== LOSE! Deadline thắng bạn =====");

            // Báo GameManager là Bot Win (Player thua)
            GameManager.Instance.GameOver(false);
        }
        // Các object khác (items, particles, v.v.) chạm vào → bỏ qua hoàn toàn
    }
}
