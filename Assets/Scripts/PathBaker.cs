using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GenMap))]
public class PathBaker : MonoBehaviour
{
    [Header("Baking Settings")]
    [Tooltip("Khoảng cách (mét) giữa các mốc tọa độ sinh ra. Nhỏ = mượt nhưng data nặng (Khuyên dùng: 0.5 - 1.0)")]
    public float resolution = 0.5f;

    [Tooltip("Tự động dính sát đường chạy xuống mặt phẳng Mesh thực tế (Khắc phục lỗi xiên dốc)")]
    public bool snapToGround = true;

    [Tooltip("Khoảng cách tia Laser dò đường từ trên trời chiếu xuống")]
    public float raycastHeight = 10f;

    [Tooltip("Vẽ trước đường tham chiếu trong cửa sổ Scene")]
    public bool drawGizmos = true;

    private List<Vector3> rawNodes = new List<Vector3>();

    // Hàm thông minh tự quét TẤT CẢ các điểm mốc (kể cả điểm neo ẩn bên trong rẽ cua)
    private List<Vector3> GetPathNodes()
    {
        List<Vector3> nodes = new List<Vector3>();
        foreach (Transform child in transform)
        {
            Transform anchorStart = child.Find("Anchor_Start");
            Transform anchorExit = child.Find("Anchor_Exit");
            bool hasAnyAnchor = false;

            // 1. Lấy lối vào
            if (anchorStart != null) 
            {
                if (nodes.Count == 0 || Vector3.Distance(nodes[nodes.Count - 1], anchorStart.position) > 0.05f)
                    nodes.Add(anchorStart.position);
                hasAnyAnchor = true;
            }

            // 2. Lấy xương sống đường cong (Nếu user thêm Anchor_Mid để giúp uốn lượn)
            for (int i = 0; i < child.childCount; i++)
            {
                Transform gc = child.GetChild(i);
                if (gc.name.Contains("Anchor_Mid"))
                {
                    if (nodes.Count == 0 || Vector3.Distance(nodes[nodes.Count - 1], gc.position) > 0.05f)
                        nodes.Add(gc.position);
                }
            }

            // 3. Lấy lối ra
            if (anchorExit != null) 
            {
                if (nodes.Count == 0 || Vector3.Distance(nodes[nodes.Count - 1], anchorExit.position) > 0.05f)
                    nodes.Add(anchorExit.position);
                hasAnyAnchor = true;
            }

            // 4. Mặc định cho Model nào không xài Anchor Tool
            if (!hasAnyAnchor)
            {
                if (nodes.Count == 0 || Vector3.Distance(nodes[nodes.Count - 1], child.position) > 0.05f)
                    nodes.Add(child.position);
            }
        }
        return nodes;
    }

    [ContextMenu("Bake Path To GenMap")]
    public void BakePath()
    {
        GenMap genMap = GetComponent<GenMap>();
        if (genMap == null) return;

        // 1. Quét tất cả Node mốc bằng bộ quét phân giải kĩ bên trên
        rawNodes = GetPathNodes();

        if (rawNodes.Count < 2)
        {
            Debug.LogWarning("⚠️ PathBaker: Bạn phải thả ít nhất 2 Object con vào làm mốc để nối đường!");
            return;
        }

        // 2. Chế thêm 2 Node tẩu thoát (Đầu và Cuối) để phép nội suy Catmull-Rom có thể bẻ cong đoạn đầu/cuối
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(rawNodes[0] - (rawNodes[1] - rawNodes[0])); // Phantom Node Start
        controlPoints.AddRange(rawNodes);
        controlPoints.Add(rawNodes[rawNodes.Count - 1] + (rawNodes[rawNodes.Count - 1] - rawNodes[rawNodes.Count - 2])); // Phantom Node End

        genMap.splineSample = new List<PathSample>();
        float totalLength = 0f;

        // 3. Bake (Sinh điểm) dữa trên Catmull-Rom (Chạy qua từng đoạn giữa 2 mốc)
        for (int i = 1; i < controlPoints.Count - 2; i++)
        {
            Vector3 p0 = controlPoints[i - 1];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = controlPoints[i + 2];

            // Xem đoạn này dài bao nhiêu mét để chia nhỏ theo Resolution
            float segLen = Vector3.Distance(p1, p2);
            int steps = Mathf.Max(2, Mathf.FloorToInt(segLen / resolution));

            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                
                // Toán học: Tính Toạ độ tịnh tiến và Hướng tại phần trăm 't'
                Vector3 pos = CalculateCatmullRom(t, p0, p1, p2, p3);
                Vector3 dir = CalculateCatmullRomTangent(t, p0, p1, p2, p3).normalized;
                Vector3 upVec = Vector3.up;

                // TỰ ĐỘNG BÁM ĐẤT (Snapping): Phóng tia Laser từ trên trời xuống để đo độ xiên của dốc
                if (snapToGround)
                {
                    if (Physics.Raycast(pos + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hit, raycastHeight * 2f))
                    {
                        pos = hit.point; // Đặt chuẩn xác 100% bằng cao độ mặt phẳng
                        upVec = hit.normal; // Lấy pháp tuyến mặt phẳng để ngả người theo độ nghiêng con dốc!
                    }
                }

                // Lưu PathSample
                PathSample sample = new PathSample();
                sample.position = pos;
                sample.forward = dir;
                sample.up = upVec;
                sample.size = 1f;

                // Tính quãng đường tích lũy
                if (genMap.splineSample.Count > 0)
                {
                    totalLength += Vector3.Distance(genMap.splineSample[genMap.splineSample.Count - 1].position, pos);
                }
                
                genMap.splineSample.Add(sample);
            }
        }
        
        // 4. Móc điểm dứt điểm nằm chính xác ở mốc cuối cùng (đề phòng vòng lặp For hụt)
        PathSample lastSample = new PathSample();
        Vector3 lastPos = rawNodes[rawNodes.Count - 1];
        Vector3 lastUp = Vector3.up;
        
        if (snapToGround && Physics.Raycast(lastPos + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hitLast, raycastHeight * 2f))
        {
            lastPos = hitLast.point;
            lastUp = hitLast.normal;
        }

        lastSample.position = lastPos;
        lastSample.up = lastUp;
        Vector3 lastDir = rawNodes[rawNodes.Count - 1] - rawNodes[rawNodes.Count - 2];
        // Không ép hướng Y về 0 nữa để hỗ trợ leo dốc tốt hơn
        lastSample.forward = lastDir.normalized;
        genMap.splineSample.Add(lastSample);

        // 5. Cập nhật Percent chuẩn (Từ 0 đến 1)
        float currentLen = 0f;
        genMap.splineSample[0].percent = 0;
        for (int i = 1; i < genMap.splineSample.Count; i++)
        {
            currentLen += Vector3.Distance(genMap.splineSample[i - 1].position, genMap.splineSample[i].position);
            genMap.splineSample[i].percent = currentLen / totalLength;
        }

        Debug.Log("✅ [PathBaker] Nướng thành công " + genMap.splineSample.Count + " điểm (Samples) vào GenMap! Data đã sẵn sàng.");
    }

    // ─── Vẽ trước đường trong Editor (Không hiện trong Game thực) ───
    private void OnDrawGizmos()
    {
        // 1. Vẽ hạt ngọc Lục bảo để hiển thị các mốc xương sống chuẩn xác mà Tool quét được
        Gizmos.color = Color.green;
        foreach (Vector3 node in GetPathNodes()) {
            Gizmos.DrawWireSphere(node, 0.5f);
        }

        if (!drawGizmos) return;

        GenMap genMap = GetComponent<GenMap>();
        if (genMap != null && genMap.splineSample != null && genMap.splineSample.Count > 1)
        {
            // 2. Vẽ cái đường Dây Kẽm Đỏ xương sống
            Gizmos.color = Color.red;
            for (int i = 0; i < genMap.splineSample.Count - 1; i++)
            {
                Gizmos.DrawLine(genMap.splineSample[i].position, genMap.splineSample[i + 1].position);
            }

            // 3. Lâu lâu vẽ xương ngang để bạn biết toạ độ nó đang bẻ cong chuẩn không (Màu Xanh lơ)
            for (int i = 0; i < genMap.splineSample.Count; i += 10)
            {
                var s = genMap.splineSample[i];
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(s.position, s.forward * 2f); // Hướng mũi nhọn
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(s.position, s.right * 5f);  // Rìa phải
                Gizmos.DrawRay(s.position, -s.right * 5f); // Rìa trái
            }
        }
    }

    // ─── Toán Học Catmull-Rom Nội Suy Đường Cong ───
    Vector3 CalculateCatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * (
            (2 * p1) +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t
        );
    }
    
    // Tính đạo hàm Curve để tìm Hướng đi thẳng (Forward) dọc theo cung tròn
    Vector3 CalculateCatmullRomTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * (
            (-p0 + p2) +
            2 * (2 * p0 - 5 * p1 + 4 * p2 - p3) * t +
            3 * (-p0 + 3 * p1 - 3 * p2 + p3) * t * t
        );
    }
}
