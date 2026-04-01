// using System.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachineCameraSetup : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS
    // ═══════════════════════════════════════════════════════════

    [Header("Auto-find Player on Start")]
    public bool autoFindPlayer = true;
    public string playerTag = "Player";

    [Header("Camera Offsets")]
    public Vector3 followOffset = new Vector3(0, 4f, -8f);

    [Header("Cinematic Boss Pan")]
    [Tooltip("Bán kính quỹ đạo orbit quanh Boss (mét)")]
    public float orbitRadius = 6f;

    [Tooltip("Độ cao camera lúc bắt đầu (đỉnh đầu Boss)")]
    public float orbitHeightStart = 7f;

    [Tooltip("Độ cao camera lúc zoom xuống thấp (ngang chân Boss)")]
    public float orbitHeightEnd = 1.5f;

    [Tooltip("Góc BẮT ĐẦU orbit (độ). 180 = xuất phát từ SAU LƯNG boss.")]
    [Range(0f, 360f)]
    public float startAngleDeg = 180f;

    [Tooltip("Góc KẾT THÚC orbit (độ). 0 = dừng TRƯỚC MẶT boss.")]
    [Range(0f, 360f)]
    public float endAngleDeg = 0f;

    [Tooltip("Offset điểm LookAt so với pivot Boss. Y=1.5 = nhìn vào mặt/ngực.")]
    public Vector3 bossLookAtOffset = new Vector3(0, 1.5f, 0);

    [Header("Damping (Follow Player)")]
    public float xDamping = 1f;
    public float yDamping = 3f;
    public float zDamping = 5f;

    // ─── Private ────────────────────────────────────────────────
    private CinemachineVirtualCamera _vcam;
    private CinemachineVirtualCamera _bossCam;
    private Coroutine _panCoroutine;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        _vcam = GetComponent<CinemachineVirtualCamera>();
        CinemachineBrain[] allBrains = FindObjectsOfType<CinemachineBrain>();
        if (allBrains.Length > 1)
        {
            Debug.LogError($"[Camera] Phát hiện {allBrains.Length} CinemachineBrain! " +
                           "Chỉ được có 1. Kiểm tra lại scene.");
        }
    }

    private void Start()
    {
        /*if (!autoFindPlayer) return;
        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null) { Debug.LogWarning("[CinemachineCameraSetup] Player not found!"); return; }
        SetupCamera(player.transform);*/
        if (!autoFindPlayer) return;

        GameObject player = GameObject.FindWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("[CinemachineCameraSetup] Player not found!");
            return;
        }

        SetupCamera(player.transform);

        // ✅ FIX GIẬT: Force snap camera về đúng vị trí ngay frame đầu
        // Không dùng StartCoroutine vì cần chạy TRƯỚC khi render
        // StartCoroutine(SnapCameraOnStart());
    }
    private IEnumerator SnapCameraOnStart()
    {
        // Đợi 1 frame để Cinemachine khởi tạo xong
        yield return null;

        // Tìm CinemachineBrain trên MainCamera
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            // ManualUpdate() + snap = force camera nhảy ngay đến vị trí đúng, không blend
            _vcam.ForceCameraPosition(
                Camera.main.transform.position,
                Camera.main.transform.rotation
            );

            // Update brain ngay lập tức để flush vị trí
            brain.ManualUpdate();
        }

        Debug.Log("[CinemachineCameraSetup] Camera snapped to start position.");
    }

    // ═══════════════════════════════════════════════════════════
    //  SETUP FOLLOW CAMERA
    // ═══════════════════════════════════════════════════════════

    public void SetupCamera(Transform target)
    {
        _vcam.Follow = target;
        _vcam.LookAt = target;

        // Framing Transposer = 2D, không có BindingMode → dùng Cinemachine3rdPersonFollow
        var follow3rd = _vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (follow3rd != null)
        {
            follow3rd.ShoulderOffset = new Vector3(0f, 6f, 0f); // ← tăng Y để cao hơn
            follow3rd.CameraDistance = 18;                         // ← khoảng cách ra sau
            follow3rd.VerticalArmLength = 1.5f;
            Debug.Log($"[CinemachineCameraSetup] 3rd Person Follow OK → distance: {follow3rd.CameraDistance}");
            // return; // Bỏ return để phần Snap camera phía dưới được chạy
        }
        _vcam.PreviousStateIsValid = false;

        // ✅ Force update ngay frame này
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.ManualUpdate();
        }
    }

    public void ChangeTarget(Transform newTarget)
    {
        if (_vcam != null && newTarget != null)
        {
            _vcam.Follow = newTarget;
            _vcam.LookAt = newTarget;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  CINEMATIC PAN (DÙNG CHUNG CHO PLAYER VÀ BOSS)
    // ═══════════════════════════════════════════════════════════

    public void StartCinematicPan(Transform target, float duration)
    {
        if (_vcam == null || target == null) return;

        // --- Tạo Virtual Camera phụ nếu chưa có ---
        if (_bossCam == null)
        {
            GameObject camObj = new GameObject("TempCinematicCamera");
            _bossCam = camObj.AddComponent<CinemachineVirtualCamera>();
            _bossCam.m_Lens = _vcam.m_Lens;

            // BẮT BUỘC: Thêm Composer để mấu chốt LookAt hoạt động (Tự vặn cổ nhìn đúng mục tiêu)
            CinemachineComposer composer = _bossCam.AddCinemachineComponent<CinemachineComposer>();
            composer.m_HorizontalDamping = 0f;
            composer.m_VerticalDamping = 0f;
        }

        // Cập nhật mục tiêu nhìn
        _bossCam.LookAt = target;
        
        // Cập nhật offset nhìn (Nhìn ngực/mặt thay vì bàn chân)
        CinemachineComposer comp = _bossCam.GetCinemachineComponent<CinemachineComposer>();
        if (comp != null) comp.m_TrackedObjectOffset = bossLookAtOffset;

        // Priority cao hơn camera player → chiếm quyền điều khiển
        _bossCam.Priority = 99;

        if (_panCoroutine != null) StopCoroutine(_panCoroutine);
        _panCoroutine = StartCoroutine(PanRoutine(target, duration));
    }

    private IEnumerator PanRoutine(Transform target, float duration)
    {
        if (_bossCam == null) yield break;

        float elapsed = 0f;
        float orbitPhaseDur = duration * 0.4f;  // Phase 1: 40% orbit vòng
        float scanPhaseDur  = duration * 0.6f;  // Phase 2: 60% scan trên-dưới

        while (elapsed < duration)
        {
            // Capping delta time to 0.05 (20fps min) so a huge lag spike during restart doesn't skip the whole animation
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += dt;

            if (_bossCam == null) break;

            // -- Lấy hướng Mục Tiêu (Target) trong world space --
            // Dùng world forward của mục tiêu để orbit đúng
            Vector3 targetPos     = target.position;
            Vector3 targetForward = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
            if (targetForward.sqrMagnitude < 0.001f) targetForward = Vector3.forward;
            Vector3 targetRight   = Vector3.Cross(Vector3.up, targetForward); // Trục phải của mục tiêu

            float angleDeg;
            float camHeight;

            if (elapsed <= orbitPhaseDur)
            {
                // ── PHASE 1: ORBIT 180° từ lưng → mặt ──────────
                float t1 = Mathf.Clamp01(elapsed / orbitPhaseDur);
                float s1 = Mathf.SmoothStep(0f, 1f, t1);

                // startAngleDeg=180 (lưng) → endAngleDeg=0 (mặt)
                angleDeg  = Mathf.Lerp(startAngleDeg, endAngleDeg, s1);
                camHeight = orbitHeightStart;
            }
            else
            {
                // ── PHASE 2: SCAN ĐẦU → CHÂN → ĐẦU ────────────
                // Đứng yên trước mặt Boss
                float t2 = Mathf.Clamp01((elapsed - orbitPhaseDur) / scanPhaseDur);

                // PingPong: cao → thấp → cao
                float pp = Mathf.PingPong(t2 * 2f, 1f);
                float s2 = Mathf.SmoothStep(0f, 1f, pp);

                angleDeg  = endAngleDeg;
                camHeight = Mathf.Lerp(orbitHeightStart, orbitHeightEnd, s2);
            }

            // -- Tính vị trí camera từ góc --
            // angleDeg=0   → cos=1, sin=0  → camera ở PHÍA TRƯỚC MẶT Boss
            // angleDeg=180 → cos=-1, sin=0 → camera ở SAU LƯNG Boss
            float rad = angleDeg * Mathf.Deg2Rad;
            float fwdComp = Mathf.Cos(rad); // thành phần dọc theo targetForward
            float rgtComp = Mathf.Sin(rad); // thành phần dọc theo targetRight (tạo cung tròn)

            Vector3 worldOffset = targetForward * (orbitRadius * fwdComp)
                                + targetRight   * (orbitRadius * rgtComp)
                                + Vector3.up  * camHeight;

            // -- Đặt vị trí thẳng vào transform của camera object --
            // DoNothing body giữ nguyên vị trí này, không override
            _bossCam.transform.position = targetPos + worldOffset;

            yield return null;
        }

        Debug.Log("[CinemachineCameraSetup] Intro pan complete for " + target.gameObject.name);
    }

    public void ResetToPlayer(Transform player)
    {
        if (_vcam == null || player == null) return;

        if (_panCoroutine != null) StopCoroutine(_panCoroutine);

        // ✅ FIX: Tắt Brain tạm 1 frame để skip blend, tránh giật
        StartCoroutine(ResetToPlayerSmooth(player));
    }

    private IEnumerator ResetToPlayerSmooth(Transform player)
    {
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();

        // 1. Tắt blend tạm thời
        CinemachineBlendDefinition originalBlend = default;
        if (brain != null)
        {
            originalBlend = brain.m_DefaultBlend;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.Cut, 0f // Cut = không blend
            );
        }

        // 2. Destroy bossCam → Cinemachine tự switch về FollowCamera
        if (_bossCam != null)
        {
            Destroy(_bossCam.gameObject);
            _bossCam = null;
        }

        // 3. Snap FollowCamera về đúng vị trí player ngay lập tức
        _vcam.Follow = player;
        _vcam.LookAt = player;
        _vcam.PreviousStateIsValid = false;

        // 4. Đợi 1 frame để Cinemachine xử lý xong
        yield return null;

        // 5. Khôi phục blend về bình thường
        if (brain != null)
        {
            brain.m_DefaultBlend = originalBlend;
        }

        Debug.Log("[CinemachineCameraSetup] Reset to Player — no blend.");
    }
    public void CleanupBeforeRestart()
    {
        // Dừng tất cả coroutine camera
        if (_panCoroutine != null)
        {
            StopCoroutine(_panCoroutine);
            _panCoroutine = null;
        }

        // Destroy bossCam ngay lập tức, không chờ
        if (_bossCam != null)
        {
            Destroy(_bossCam.gameObject);
            _bossCam = null;
        }

        // Reset priority về mặc định
        if (_vcam != null)
        {
            _vcam.Priority = 10;
            _vcam.PreviousStateIsValid = false;
        }

        // Đổi blend thành Cut để scene mới không blend từ state cũ
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.Cut, 0f
            );
        }

        Debug.Log("[CinemachineCameraSetup] Cleanup done before restart.");
    }
}