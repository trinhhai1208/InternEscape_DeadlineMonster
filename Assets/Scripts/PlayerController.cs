using System.Collections;       // Cần cho IEnumerator (Coroutine)
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Các biến có thể chỉnh trong Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Movement")] // Tạo tiêu đề nhóm trong Inspector

    // Tốc độ chạy cơ bản (units/giây). public để SkinUpItem.cs có thể tăng trực tiếp.
    public float speed = 10f;

    // Tốc độ trượt ngang khi đổi lane (Lerp factor). Cao = đổi lane nhanh/gấp.
    public float laneSwitchSpeed = 15f;

    // Vị trí X (world) của 3 lane: trái (-5), giữa (0), phải (5).
    // Mảng để dễ mở rộng nếu cần thêm lane.
    public float[] lanes = { -5f, 0, 5f };

    [Header("Skin Models")]

    // Mảng các GameObject skin (model 3D) — mỗi phần tử là 1 skin khác nhau.
    // Index 0 = Intern, 1 = Fresher, 2 = Junior, 3 = Senior.
    // Chỉ 1 skin active tại 1 thời điểm.
    public GameObject[] skinModels;

    [Header("Mobile Input")]
    // Ngưỡng vuốt (pixel) tối thiểu để nhận diện là 1 lần vuốt. (Chỉnh trong Inspector)
    public float swipeThreshold = 30f;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS — Chỉ dùng nội bộ, không hiện trong Inspector
    // ═══════════════════════════════════════════════════════════

    // Lane hiện tại đang đứng (index trong mảng lanes[]). Bắt đầu ở giữa (index 1).
    private int currentLane = 1;

    // Rigidbody để điều khiển vật lý của Player (MovePosition thay vì transform.position).
    private Rigidbody rb;

    // Animator của skin hiện tại — cần cập nhật sau mỗi lần đổi skin.
    private Animator animator;

    // Hệ số nhân tốc độ tạm thời (Coffee boost hoặc Obstacle slow).
    // Nhân vào speed khi tính vận tốc thực: vận tốc = speed * speedMultiplier.
    // = 1f khi bình thường, > 1f khi boost, < 1f khi slow.
    private float speedMultiplier = 1f;

    // Cờ kiểm tra đang bị slow không — để không apply slow 2 lần cùng lúc.
    private bool isSlowed = false;

    // Lưu Coroutine đang chạy của Coffee boost để có thể dừng nếu nhặt thêm cà phê.
    private Coroutine activeBoostCoroutine;

    // Index skin hiện tại trong mảng skinModels[]. Bắt đầu từ 0 (Intern).
    private int currentSkinIndex = 0;

    // Biến phụ trợ cho Mobile Swipe
    private Vector2 startTouchPosition;
    private bool isSwiping = false;

    // Cho phép các script khác (vd: SkinUpItem) xem đang ở skin số mấy
    public int SkinIndex => currentSkinIndex;

    // ----- SPLINE DATA -----
    // Index hiện tại của Player trên đường GenMap
    private int currentPathIndex = 0;


    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    // Start() chạy 1 lần khi scene bắt đầu hoặc khi GameObject được tạo
    void Start()
    {
        // GetComponent<T>(): tìm component type T trên cùng GameObject.
        // Cần Rigidbody để dùng MovePosition và MoveRotation.
        rb = GetComponent<Rigidbody>();

        // Khóa tất cả trục xoay để Player không bị lật/ngã khi di chuyển.
        // FreezeRotation = khóa cả X, Y, Z rotation.
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Hiện skin mặc định (index 0 = Intern), ẩn tất cả skin còn lại.
        ShowSkin(currentSkinIndex);

        // Lấy Animator từ model skin đang active để drive animation.
        RefreshAnimator();
    }

    // Update() chạy mỗi frame — dùng cho input (cần responsive ngay lập tức)
    void Update()
    {
        // ─── PC INPUT (Keyboard) ─────────────────────────────────────────

        // GetKeyDown: true chỉ ĐÚNG 1 frame khi phím được nhấn xuống (không lặp khi giữ).
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLeft();

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveRight();

        // ─── MOBILE INPUT (Touch Swipe) ──────────────────────────────────

        // Kiểm tra xem có ngón tay nào đang chạm màn hình không
        if (Input.touchCount > 0)
        {
            // Lấy ngón tay đầu tiên chạm vào (index 0)
            Touch touch = Input.GetTouch(0);

            // Bắt đầu chạm
            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                isSwiping = true;
            }
            // Đang lướt hoặc vừa thả tay ra
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
            {
                if (isSwiping)
                {
                    Vector2 endTouchPosition = touch.position;
                    float xDistance = endTouchPosition.x - startTouchPosition.x;
                    float yDistance = endTouchPosition.y - startTouchPosition.y;

                    // Chỉ nhận diện lướt khi: quá ngưỡng pixel VÀ lướt ngang nhiều hơn lướt dọc (để tránh vuốt nhầm khi vuốt lên/xuống)
                    if (Mathf.Abs(xDistance) > swipeThreshold && Mathf.Abs(xDistance) > Mathf.Abs(yDistance))
                    {
                        if (xDistance < 0) MoveLeft();   // Vuốt trái
                        else               MoveRight();  // Vuốt phải

                        // Reset cờ để không bị lướt liên tục quá nhiều lane trong 1 lần vuốt
                        isSwiping = false;
                    }
                }
            }
        }

        // Cập nhật parameter "Speed" trên Animator để blend animation.
        // Chỉ chạy hoạt ảnh nếu Game đang thực sự Play
        if (animator != null)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGamePlaying)
                animator.SetFloat("Speed", 0);
            else
                animator.SetFloat("Speed", speed * speedMultiplier);
        }
    }

    // Hàm phụ trợ đổi lane
    private void MoveLeft()
    {
        // Chỉ giảm nếu chưa ở lane trái nhất
        if (currentLane > 0) currentLane--;
    }

    private void MoveRight()
    {
        // Chỉ tăng nếu chưa ở lane phải nhất
        if (currentLane < lanes.Length - 1) currentLane++;
    }

    // FixedUpdate() chạy mỗi physics frame (mặc định 50 lần/giây, cố định).
    void FixedUpdate()
    {
        // Nếu Game chưa nhấn nút Start, hoặc đang load Cắt Cảnh Camera -> Đứng yên
        if (GameManager.Instance != null && !GameManager.Instance.isGamePlaying)
            return;

        // Bắt buộc phải có đường GenMap để chạy
        if (GenMap.Instance == null || GenMap.Instance.splineSample == null || GenMap.Instance.splineSample.Count == 0)
            return;

        float moveDelta = speed * speedMultiplier * Time.fixedDeltaTime;

        // 1. Tịnh tiến mốc Điểm Nhìn (Path Index) theo tốc độ lướt
        while (currentPathIndex < GenMap.Instance.splineSample.Count - 1)
        {
            Vector3 nextPos = GenMap.Instance.splineSample[currentPathIndex + 1].position;
            
            // Tính khoảng cách từ ta đến điểm tiếp theo (bỏ qua độ cao)
            Vector3 flatNext = new Vector3(nextPos.x, 0, nextPos.z);
            Vector3 flatPlayer = new Vector3(rb.position.x, 0, rb.position.z);
            
            // Vượt mép Điểm thì nâng Index lên
            if (Vector3.Dot(flatNext - flatPlayer, GenMap.Instance.splineSample[currentPathIndex].forward) <= 0 || 
                Vector3.Distance(flatPlayer, flatNext) < moveDelta)
            {
                currentPathIndex++;
            }
            else break;
        }

        // Neo chặn index cuối cùng khỏi lỗi tràn mảng
        if (currentPathIndex >= GenMap.Instance.splineSample.Count) 
            currentPathIndex = GenMap.Instance.splineSample.Count - 1;

        PathSample sample = GenMap.Instance.splineSample[currentPathIndex];

        // 2. Di chuyển tịnh tiến tới trước tự do thoải mái
        Vector3 baseNewPos = rb.position + sample.forward * moveDelta;

        // 3. Tính độ lệch Làn: Kéo dần nhân vật về phía trục X (Local Vector Mép đường)
        Vector3 offsetFromCenter = baseNewPos - sample.position;
        // Chiếu lên trục vuông góc của đường để biết Player đang dạt ra ngoài bao xa
        float currentLateral = Vector3.Dot(offsetFromCenter, sample.right);
        float targetLateral = lanes[currentLane];
        // Kéo dần mượt mà vào giữa lane mục tiêu
        float nextLateral = Mathf.Lerp(currentLateral, targetLateral, laneSwitchSpeed * Time.fixedDeltaTime);
        
        // Vị trí chốt: Bằng Điểm Tới Trước + Độ lệch hông Trái Phải
        Vector3 finalPos = baseNewPos + sample.right * (nextLateral - currentLateral);
        // Đảm bảo chạy lượn lên dốc / xuống dốc thì bám dính lấy sàn
        finalPos.y = Mathf.Lerp(rb.position.y, sample.position.y, 10f * Time.fixedDeltaTime);

        // Update Physics
        rb.MovePosition(finalPos);

        // 4. Xoay mặt siêu mượt bám Khúc Cua
        Quaternion targetRotation = Quaternion.LookRotation(sample.forward, sample.up);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
    }

    // ═══════════════════════════════════════════════════════════
    //  SKIN SWAP
    // ═══════════════════════════════════════════════════════════

    // Hàm public để SkinUpItem gọi khi Player nhặt SkinUp
    // endgameSpeedMultiplier: hệ số lưu vào GameManager (1f = không lưu thêm speed)
    public void UpgradeSkin(float endgameSpeedMultiplier)
    {
        // Tăng index skin, nhưng không vượt quá số skin có.
        // Mathf.Min: lấy giá trị nhỏ hơn giữa 2 số — đảm bảo không out of bounds.
        currentSkinIndex = Mathf.Min(currentSkinIndex + 1, skinModels.Length - 1);

        // Ẩn skin cũ, hiện skin mới theo index vừa cập nhật
        ShowSkin(currentSkinIndex);

        // Lấy Animator từ skin mới (mỗi model có thể có Animator riêng)
        RefreshAnimator();

        // Thông báo cho GameManager biết SkinUp đã được dùng (lưu level và multiplier)
        GameManager.Instance.OnSkinUpCollected(currentSkinIndex, endgameSpeedMultiplier);

        Debug.Log("[Player] Skin swapped to: " + currentSkinIndex);
    }

    // Ẩn tất cả model, chỉ hiện model tại index chỉ định
    private void ShowSkin(int index)
    {
        // Kiểm tra null và rỗng trước khi dùng
        if (skinModels == null || skinModels.Length == 0) return;

        // Duyệt qua tất cả skin model
        for (int i = 0; i < skinModels.Length; i++)
            if (skinModels[i] != null)
                // SetActive(i == index): chỉ true khi i đúng bằng index cần hiện
                skinModels[i].SetActive(i == index);
    }

    // Sau khi đổi skin, lấy Animator từ model đang active (mỗi skin có Animator riêng)
    private void RefreshAnimator()
    {
        if (skinModels == null || currentSkinIndex >= skinModels.Length) return;

        // Lấy model đang active
        GameObject activeModel = skinModels[currentSkinIndex];

        if (activeModel != null)
            // GetComponentInChildren: tìm Animator trong children của model này.
            // Dùng InChildren vì Animator thường gắn trên mesh child, không phải root.
            animator = activeModel.GetComponentInChildren<Animator>();
    }

    // ═══════════════════════════════════════════════════════════
    //  SPEED BOOST (Coffee)
    // ═══════════════════════════════════════════════════════════

    // Gọi từ CoffeeItem — tăng tốc tạm thời trong 'duration' giây
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        // Nếu đang boost rồi (nhặt thêm cà phê) → dừng coroutine cũ trước
        if (activeBoostCoroutine != null)
            StopCoroutine(activeBoostCoroutine);

        // Bắt đầu coroutine mới và lưu reference để có thể Stop nếu cần
        activeBoostCoroutine = StartCoroutine(BoostCoroutine(multiplier, duration));
    }

    // ═══════════════════════════════════════════════════════════
    //  OBSTACLE SLOW
    // ═══════════════════════════════════════════════════════════

    // Gọi từ Obstacle — giảm tốc trong 'duration' giây
    // Default parameters: nếu gọi không truyền tham số, dùng giá trị mặc định này
    public void ApplyObstacleSlow(float slowMultiplier = 0.5f, float duration = 2f)
    {
        // Nếu đang bị slow rồi → bỏ qua, không apply thêm (chồng slow)
        if (isSlowed) return;

        // StartCoroutine: bắt đầu chạy hàm bất đồng bộ (có yield return)
        StartCoroutine(SlowCoroutine(slowMultiplier, duration));
    }

    // ═══════════════════════════════════════════════════════════
    //  ENDGAME SPEED BONUS
    // ═══════════════════════════════════════════════════════════

    // Gọi khi game kết thúc thắng — cộng thêm speed từ các SkinUp đã nhặt
    public void ApplyEndgameSpeedBonus()
    {
        // Lấy hệ số nhân speed từ GameManager (tổng hợp từ tất cả SkinUp)
        float mult = GameManager.Instance.GetEndgameSpeedMultiplier();

        if (mult > 1f) // Chỉ áp dụng nếu có tăng tốc (> bình thường)
        {
            speed *= mult; // Nhân tốc độ thực tế
            Debug.Log("[Player] Endgame speed: " + speed);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  COROUTINES — Hàm bất đồng bộ (chạy song song với gameplay)
    // ═══════════════════════════════════════════════════════════

    // IEnumerator: kiểu trả về bắt buộc cho Coroutine
    // Coroutine có thể "tạm dừng" ở yield return và tiếp tục sau đó
    private IEnumerator BoostCoroutine(float multiplier, float duration)
    {
        // Đặt speedMultiplier = multiplier ngay lập tức (tăng tốc từ frame này)
        speedMultiplier = multiplier;

        // Tạm dừng coroutine 'duration' giây, trong thời gian đó game vẫn chạy bình thường
        yield return new WaitForSeconds(duration);

        // Sau 'duration' giây, khôi phục về bình thường
        speedMultiplier = 1f;

        // Xóa reference vì coroutine đã kết thúc
        activeBoostCoroutine = null;
    }

    private IEnumerator SlowCoroutine(float slowMultiplier, float duration)
    {
        // Đánh dấu đang bị slow (để ApplyObstacleSlow không apply thêm)
        isSlowed = true;

        // Áp dụng giảm tốc (vd: 0.5 = còn 50% tốc độ)
        speedMultiplier = slowMultiplier;

        // Báo Animator chuyển sang animation Walk (IsSlowed = true → Run → Walk)
        if (animator != null)
            animator.SetBool("IsSlowed", true);

        // Chờ hết thời gian slow
        yield return new WaitForSeconds(duration);

        // Phục hồi tốc độ bình thường
        speedMultiplier = 1f;

        // Bỏ cờ slow
        isSlowed = false;

        // Báo Animator chuyển lại Run (IsSlowed = false → Walk → Run)
        if (animator != null)
            animator.SetBool("IsSlowed", false);
    }
}