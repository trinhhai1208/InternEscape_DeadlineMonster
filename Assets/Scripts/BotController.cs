using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BotController: Điều khiển "Deadline" — kẻ đuổi theo Player.
/// Bot tự chạy về phía trước theo transform.forward, tốc độ tăng dần theo thời gian.
/// Khi Bot bắt kịp Player (va chạm) → Game Over.
/// </summary>
public class BotController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS
    // ═══════════════════════════════════════════════════════════

    [Header("Movement")]

    // Tốc độ ban đầu của Bot khi game bắt đầu (units/giây).
    // Nên thấp hơn Player.speed một chút để Player có lợi thế ban đầu.
    public float baseSpeed = 10f;

    // Lượng tốc độ tăng thêm mỗi giây (acceleration). Bot càng lúc càng nhanh.
    public float speedIncreaseRate = 0.15f;

    // Giới hạn tốc độ tối đa của Bot. Đảm bảo Bot không vượt quá ngưỡng này.
    public float maxSpeed = 15f;

    [Header("References")]

    // Reference đến Transform của Player — dùng để tính khoảng cách (nếu cần sau này).
    // Hiện chưa được dùng trong code nhưng có thể dùng cho AI snapping.
    public Transform player;

    [Header("Animation & Audio")]

    // Animator của model Bot. Gán trực tiếp trong Inspector hoặc để trống (tự tìm trong children).
    public Animator animator;

    // Chiếc loa phụ tản nhiệt tiếng động kinh hoàng khi Boss đến gần
    public AudioSource bossFootstepSource;
    public AudioClip bossFootstepClip;

    // ─── Biến nội bộ ────────────────────────────────────────────

    // Tốc độ hiện tại của Bot — thay đổi theo thời gian từ baseSpeed đến maxSpeed.
    private float currentSpeed;

    // Rigidbody để di chuyển Bot qua physics (MovePosition).
    private Rigidbody rb;

    // Cờ báo hiệu Game bắt đầu để chạy
    // (Bỏ qua khi xem Intro Camera)

    // ----- SPLINE & LANE DATA -----
    private int currentPathIndex = 0;
    private int currentLane = 1; // 0=Trái, 1=Giữa, 2=Phải
    // Lấy nguyên toạ độ Lane giống Player để chạy song song
    private float[] lanes = { -5f, 0, 5f };
    private float laneSwitchSpeed = 10f;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void Start()
    {
        // Lấy Rigidbody component trên cùng GameObject với BotController
        rb = GetComponent<Rigidbody>();

        // Khóa tất cả trục xoay để Bot không bị lật khi di chuyển — giống Player
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Đặt tốc độ ban đầu bằng baseSpeed (chưa tăng tốc)
        currentSpeed = baseSpeed;

        // Nếu Animator chưa được kéo vào Inspector → tự tìm trong children của Bot
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        // Cảnh báo nếu không tìm thấy Animator — Bot sẽ không animate
        if (animator == null)
            Debug.LogWarning("[BotController] Không tìm thấy Animator! Hãy gán CharacterAnimator.controller cho child model của Bot.");
            
        // Setup Băng Loa Kinh Hoàng
        if (bossFootstepSource != null && bossFootstepClip != null)
        {
            bossFootstepSource.clip = bossFootstepClip;
            bossFootstepSource.loop = true;
            bossFootstepSource.volume = 0f; // Khởi đầu câm lặng vì đứng xa
            bossFootstepSource.Play();
        }

        // Bắt đầu AI Đổi Lane Ngẫu Nhiên
        StartCoroutine(RandomLaneSwitching());
    }

    // Coroutine: Boss tự đổi Lane liên tục để đánh lừa người chơi
    IEnumerator RandomLaneSwitching()
    {
        while (true)
        {
            // Nghỉ 2 tới 5 giây rồi mới lật chuyển
            yield return new WaitForSeconds(Random.Range(2f, 5f));
            
            // Chỉ đổi nếu game đang chạy
            if (GameManager.Instance != null && GameManager.Instance.isGamePlaying)
            {
                // Random lane từ 0 đến 2
                int newLane = Random.Range(0, 3);
                currentLane = newLane;
            }
        }
    }

    // Update() chạy mỗi frame — dùng cho logic không liên quan physics
    void Update()
    {
        // Tăng tốc dần nếu Game đang diễn ra
        if (GameManager.Instance != null && GameManager.Instance.isGamePlaying)
        {
            currentSpeed = Mathf.Min(currentSpeed + speedIncreaseRate * Time.deltaTime, maxSpeed);
        }

        // Cập nhật parameter "Speed" cho Animator để điều khiển animation chạy
        if (animator != null)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGamePlaying)
            {
                animator.SetFloat("Speed", 0);
                if (bossFootstepSource != null && bossFootstepSource.isPlaying) bossFootstepSource.Pause();
            }
            else
            {
                animator.SetFloat("Speed", currentSpeed);
                if (bossFootstepSource != null && !bossFootstepSource.isPlaying) bossFootstepSource.UnPause();
            }
        }

        // --- HỆ THỐNG ÂM THANH ÁP SÁT TRUY SÁT ---
        if (bossFootstepSource != null && player != null && GameManager.Instance != null && GameManager.Instance.isGamePlaying)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            
            // Công thức hù doạ: Sát < 15 mét mới nổ tiếng chân dồn dập. 
            // Ở cự ly 15 mét = Volume 0%, Cự ly 0 mét = Volume 100%.
            if (dist < 15f)
            {
                bossFootstepSource.volume = 1f - (dist / 15f);
            }
            else
            {
                bossFootstepSource.volume = 0f;
            }
        }
    }

    // FixedUpdate cho physics movement — cố định 50 lần/giây
    void FixedUpdate()
    {
        // Chặn di chuyển khi Intro Camera
        if (GameManager.Instance != null && !GameManager.Instance.isGamePlaying) return;

        // Bắt buộc phải xài đường GenMap
        if (GenMap.Instance == null || GenMap.Instance.splineSample == null || GenMap.Instance.splineSample.Count == 0) return;

        float moveDelta = currentSpeed * Time.fixedDeltaTime;

        // 1. Dịch chuyển mốc Path Index
        while (currentPathIndex < GenMap.Instance.splineSample.Count - 1)
        {
            Vector3 nextPos = GenMap.Instance.splineSample[currentPathIndex + 1].position;
            Vector3 flatNext = new Vector3(nextPos.x, 0, nextPos.z);
            Vector3 flatBot = new Vector3(rb.position.x, 0, rb.position.z);
            
            if (Vector3.Dot(flatNext - flatBot, GenMap.Instance.splineSample[currentPathIndex].forward) <= 0 || 
                Vector3.Distance(flatBot, flatNext) < moveDelta)
            {
                currentPathIndex++;
            }
            else break;
        }

        if (currentPathIndex >= GenMap.Instance.splineSample.Count) 
            currentPathIndex = GenMap.Instance.splineSample.Count - 1;

        PathSample sample = GenMap.Instance.splineSample[currentPathIndex];

        // 2. Chạy tới trước
        Vector3 baseNewPos = rb.position + sample.forward * moveDelta;

        // 3. Chạy đổi Lane (AI dí player gắt gao)
        Vector3 offsetFromCenter = baseNewPos - sample.position;
        float currentLateral = Vector3.Dot(offsetFromCenter, sample.right);
        float nextLateral = Mathf.Lerp(currentLateral, lanes[currentLane], laneSwitchSpeed * Time.fixedDeltaTime);
        
        Vector3 finalPos = baseNewPos + sample.right * (nextLateral - currentLateral);
        finalPos.y = Mathf.Lerp(rb.position.y, sample.position.y, 10f * Time.fixedDeltaTime);

        rb.MovePosition(finalPos);

        // 4. Bám mắt xoay Cua 90 độ
        Quaternion targetRotation = Quaternion.LookRotation(sample.forward, sample.up);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, 15f * Time.fixedDeltaTime));
    }

    // ═══════════════════════════════════════════════════════════
    //  COLLISION
    // ═══════════════════════════════════════════════════════════

    // OnCollisionEnter: gọi khi 2 Collider NON-trigger va chạm vật lý thật
    // (Khác OnTriggerEnter dùng khi isTrigger = true)
    private void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra nếu Bot đụng vào Player
        if (collision.gameObject.CompareTag("Player"))
        {
            // In cảnh báo (màu vàng) — Bot bắt kịp Player
            Debug.LogWarning("===== LOSE! Bạn đã bị deadline dí =====");

            // Báo cho GameManager xử lý Game Over (Thua)
            GameManager.Instance.GameOver(false);
        }
    }
}
