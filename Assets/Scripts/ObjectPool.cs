// Chỉ cần System.Collections.Generic cho Queue<T> — không cần System.Collections hay UnityEngine.*
// vì ObjectPool là class C# thuần, không kế thừa MonoBehaviour
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ObjectPool: "Kho" chứa các GameObject pre-created cho 1 loại prefab.
/// Mục đích: Tránh gọi Instantiate/Destroy liên tục (tốn CPU) bằng cách
/// tái sử dụng các object cũ thông qua SetActive(true/false).
///
/// Cách dùng:
///   var pool = new ObjectPool(prefab, 30, container);  // Tạo pool với 30 objects
///   GameObject obj = pool.Get(pos, rot);               // Lấy từ pool
///   pool.Return(obj);                                  // Trả về pool
/// </summary>
public class ObjectPool
{
    // ─── Biến lưu trữ ────────────────────────────────────────────

    // readonly: chỉ gán 1 lần trong constructor, sau đó không thể thay đổi.
    // prefab: bản mẫu để tạo các object mới khi pool cần thêm.
    private readonly GameObject prefab;

    // container: Transform cha chứa tất cả pooled objects trong Hierarchy.
    // Giúp Hierarchy gọn hơn, không bị lộn xộn.
    private readonly Transform container;

    // Queue<T>: hàng đợi FIFO (First In First Out) — object nào trả về trước sẽ được lấy ra trước.
    // Phù hợp hơn List<T> cho pool vì Get/Return thường xuyên và không cần random access.
    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    // ─── Constructor ─────────────────────────────────────────────

    // Constructor: chạy khi tạo pool = new ObjectPool(...)
    // prefab: loại object cần pool
    // initialSize: số object tạo sẵn ngay từ đầu (pre-warm)
    // container: Transform cha (tùy chọn, có thể null)
    public ObjectPool(GameObject prefab, int initialSize, Transform container = null)
    {
        // Lưu prefab và container vào field (this. để phân biệt với tham số cùng tên)
        this.prefab    = prefab;
        this.container = container;

        // Pre-warm: tạo sẵn 'initialSize' objects ngay khi khởi tạo pool.
        // Mục đích: tránh tạo mới trong giữa gameplay (gây lag).
        for (int i = 0; i < initialSize; i++)
            // CreateNew() tạo 1 object mới (inactive), Enqueue thêm vào cuối Queue.
            pool.Enqueue(CreateNew());
    }

    // ─── Get: Lấy 1 object từ pool ───────────────────────────────

    /// <summary>
    /// Lấy 1 object từ pool, đặt vào vị trí và góc xoay cho trước, rồi kích hoạt.
    /// Nếu pool hết (Count = 0) → tự động tạo thêm 1 object mới.
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        // Toán tử 3 ngôi (ternary): nếu pool còn object → Dequeue (lấy ra đầu Queue),
        // ngược lại → CreateNew() tạo thêm 1 cái (pool không bao giờ trống hẳn).
        GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateNew();

        // Đặt vị trí và góc xoay cùng lúc — tối ưu hơn gọi riêng position rồi rotation.
        obj.transform.SetPositionAndRotation(position, rotation);

        // Kích hoạt object: Start() sẽ chạy lại nếu chưa chạy, hoặc OnEnable() nếu đã chạy.
        obj.SetActive(true);

        // Trả về reference để caller dùng tiếp
        return obj;
    }

    // ─── Return: Trả object về pool ──────────────────────────────

    /// <summary>
    /// Tắt object và cất vào cuối Queue để dùng lại lần sau.
    /// Thay thế hoàn toàn cho Destroy().
    /// </summary>
    public void Return(GameObject obj)
    {
        // Tắt object: ẩn khỏi Scene và dừng mọi Update/coroutine trên nó.
        // Nhưng object vẫn tồn tại trong memory — không bị GC thu hồi.
        obj.SetActive(false);

        // Đưa vào cuối hàng đợi để dùng lại lần sau
        pool.Enqueue(obj);
    }

    // ─── CreateNew: Tạo object mới (private) ─────────────────────

    // Hàm nội bộ — chỉ được gọi khi pool cần thêm object
    private GameObject CreateNew()
    {
        // Object.Instantiate: vì đây là class C# thuần (không MonoBehaviour),
        // phải dùng Object.Instantiate thay vì chỉ gọi Instantiate.
        // container: đặt object này làm con của container trong Hierarchy.
        GameObject obj = Object.Instantiate(prefab, container);

        // Tắt ngay — object mới tạo phải ở trạng thái inactive cho đến khi Get() gọi
        obj.SetActive(false);

        return obj;
    }
}
