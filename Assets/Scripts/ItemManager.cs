using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemManager: Spawn items dùng Object Pooling thay vì Instantiate/Destroy.
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [System.Serializable]
    public class ItemSpawnData
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float spawnWeight = 0.33f;
        [Tooltip("Số objects tạo sẵn trong pool. Nên >= số item cùng lúc trên path.")]
        public int poolSize = 30;
    }

    [Header("Item Prefabs")]
    public ItemSpawnData codeCommitData;
    public ItemSpawnData coffeeData;
    public ItemSpawnData skinUpData;
    public ItemSpawnData obstacleData;

    [Header("Path Settings")]
    public float spawnSpacing = 5f;
    public float itemY = 2.5f; // Dùng chung 1 chiều cao chuẩn cho toàn bộ Item

    [Header("Lane Settings")]
    public float[] lanes = { -5f, 0f, 5f };

    [Header("SkinUp Limit")]
    public int maxSkinUpCount = 3;

    // ─── Object Pools ─────────────────────────────────────────
    private ObjectPool codeCommitPool;
    private ObjectPool coffeePool;
    private ObjectPool skinUpPool;
    private ObjectPool obstaclePool;
    private Transform poolContainer;

    // Runtime
    private List<GameObject> activeItems = new List<GameObject>();
    private int skinUpSpawnedCount = 0;

    // ─── Unity Lifecycle ──────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        InitPools();
        SpawnAllItems();
    }

    // ─── Khởi tạo Pools ──────────────────────────────────────

    private void InitPools()
    {
        poolContainer = new GameObject("[ItemPool]").transform;
        poolContainer.SetParent(transform);

        if (codeCommitData?.prefab != null)
            codeCommitPool = new ObjectPool(codeCommitData.prefab, codeCommitData.poolSize, poolContainer);
        if (coffeeData?.prefab != null)
            coffeePool     = new ObjectPool(coffeeData.prefab,     coffeeData.poolSize,     poolContainer);
        if (skinUpData?.prefab != null)
            skinUpPool     = new ObjectPool(skinUpData.prefab,     skinUpData.poolSize,     poolContainer);
        if (obstacleData?.prefab != null)
            obstaclePool   = new ObjectPool(obstacleData.prefab,   obstacleData.poolSize,   poolContainer);

        Debug.Log("[ItemManager] Object Pools initialized.");
    }

    private ObjectPool GetPool(GameObject prefab)
    {
        if (prefab == codeCommitData?.prefab) return codeCommitPool;
        if (prefab == coffeeData?.prefab)     return coffeePool;
        if (prefab == skinUpData?.prefab)     return skinUpPool;
        if (prefab == obstacleData?.prefab)   return obstaclePool;
        return null;
    }

    // ─── Spawn toàn bộ path ───────────────────────────────────

    /*private void SpawnAllItems()
    {

        if (pathStart == null || pathEnd == null)
        {
            Debug.LogWarning("[ItemManager] Chưa gán pathStart hoặc pathEnd!");
            return;
        }

        Vector3 start = pathStart.position;
        Vector3 end = pathEnd.position;
        int rowCount = Mathf.FloorToInt(Vector3.Distance(start, end) / spawnSpacing);

        HashSet<int> skinUpRows = new HashSet<int>();
        if (skinUpPool != null && maxSkinUpCount > 0)
        {
            for (int i = 1; i <= maxSkinUpCount; i++)
            {
                int r = Mathf.RoundToInt(rowCount * ((float)i / (maxSkinUpCount + 1)));
                skinUpRows.Add(Mathf.Clamp(r, 1, rowCount));
            }
        }

        int total = 0;
        for (int row = 1; row <= rowCount; row++)
        {
            Vector3 rowBase = Vector3.Lerp(start, end, (float)row / rowCount);

            if (skinUpRows.Contains(row))
            {
                int mid = lanes.Length / 2;
                Vector3 pos = new Vector3(lanes[mid], itemY, rowBase.z);
                GameObject obj = skinUpPool.Get(pos, skinUpData.prefab.transform.rotation);
                activeItems.Add(obj);
                total++;
            }
            else
            {
                for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
                {
                    GameObject prefab = PickNormalItemPrefab();
                    if (prefab == null) continue;

                    ObjectPool pool = GetPool(prefab);
                    if (pool == null) continue;

                    bool isCC = prefab == codeCommitData?.prefab;
                    Vector3 pos = new Vector3(lanes[laneIndex], isCC ? codeCommitItemY : itemY, rowBase.z);
                    GameObject obj = pool.Get(pos, prefab.transform.rotation);
                    activeItems.Add(obj);
                    total++;
                }
            }
        }

        Debug.Log($"[ItemManager] Spawned {total} items via Object Pool ({rowCount} rows)");
    }*/
    private void SpawnAllItems()
    {
        if (GenMap.Instance == null || GenMap.Instance.splineSample == null
            || GenMap.Instance.splineSample.Count < 2)
        {
            Debug.LogWarning("[ItemManager] GenMap chưa có data! Hãy Bake Path trước.");
            return;
        }
        var samples = GenMap.Instance.splineSample;
        float totalLength = 0f;
        for (int i = 1; i < samples.Count; i++)
            totalLength += Vector3.Distance(samples[i - 1].position, samples[i].position);
        int rowCount = Mathf.FloorToInt(totalLength / spawnSpacing);
        if (rowCount <= 0) return;

        HashSet<int> skinUpRows = new HashSet<int>();
        if (skinUpPool != null && maxSkinUpCount > 0)
        {
            for (int i = 1; i <= maxSkinUpCount; i++)
            {
                int r = Mathf.RoundToInt(rowCount * ((float)i / (maxSkinUpCount + 1)));
                skinUpRows.Add(Mathf.Clamp(r, 1, rowCount));
            }
        }

        int total = 0;
        for (int row = 1; row <= rowCount; row++)
        {
            float targetDist = row * spawnSpacing;
            PathSample sample = GetSampleAtDistance(samples, targetDist);

            float splineX = sample.position.x;
            float splineY = sample.position.y;
            float splineZ = sample.position.z;

            if (skinUpRows.Contains(row))
            {
                int mid = lanes.Length / 2;
                Vector3 laneOffset = sample.right * lanes[mid];
                // Cộng thêm splineY để vật phẩm cũng leo lượn theo dốc
                Vector3 pos = new Vector3(splineX + laneOffset.x, splineY + itemY, splineZ + laneOffset.z);
                Quaternion curveRot = Quaternion.LookRotation(sample.forward, sample.up);
                GameObject obj = skinUpPool.Get(pos, curveRot * skinUpData.prefab.transform.rotation);
                activeItems.Add(obj);
                total++;
            }
            else
            {
                int bugCountInRow = 0; // Bộ đếm số chướng ngại vật trong 1 hàng ngang

                for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
                {
                    GameObject prefab = PickNormalItemPrefab();

                    // --- CHỐNG LỖI BÍT ĐƯỜNG MÀN CHƠI (WALL OF BUG) ---
                    if (prefab != null && obstacleData != null && prefab == obstacleData.prefab)
                    {
                        bugCountInRow++;
                        // Nếu Máy chủ tính đặt con Bug thứ 3 bít kín đường hẻm
                        if (bugCountInRow >= lanes.Length)
                        {
                            // Áp chế, ép nó đẻ ra CodeCommit (hoặc Coffee) làm lối thoát cho Player
                            prefab = (Random.value > 0.1f && codeCommitData?.prefab != null) ? codeCommitData.prefab : coffeeData?.prefab;
                        }
                    }

                    if (prefab == null) continue;
                    ObjectPool pool = GetPool(prefab);
                    if (pool == null) continue;

                    Vector3 laneOffset = sample.right * lanes[laneIndex];
                    // Cộng thêm splineY vào itemY để các vật phẩm đều mọc chuẩn 1 độ cao trên mặt phẳng nghiêng
                    Vector3 pos = new Vector3(splineX + laneOffset.x, splineY + itemY, splineZ + laneOffset.z);
                    Quaternion curveRot = Quaternion.LookRotation(sample.forward, sample.up);
                    GameObject obj = pool.Get(pos, curveRot * prefab.transform.rotation);
                    activeItems.Add(obj);
                    total++;
                }
            }
        }
        Debug.Log($"[ItemManager] Spawned {total} items theo spline ({rowCount} rows)");
    }

    // ── Helper: tìm PathSample tại khoảng cách targetDist trên spline ──
    private PathSample GetSampleAtDistance(List<PathSample> samples, float targetDist)
    {
        float accumulated = 0f;

        for (int i = 1; i < samples.Count; i++)
        {
            float segLen = Vector3.Distance(samples[i - 1].position, samples[i].position);

            if (accumulated + segLen >= targetDist)
            {
                // Nội suy giữa 2 điểm lân cận
                float t = (targetDist - accumulated) / segLen;

                PathSample result = new PathSample();
                result.position = Vector3.Lerp(samples[i - 1].position, samples[i].position, t);
                result.forward = Vector3.Slerp(samples[i - 1].forward, samples[i].forward, t).normalized;
                // Rất quan trọng: Tính thêm trục ngả người nghiêng theo sườn dốc mà PathBaker đã bắn Raycast!
                result.up = Vector3.Slerp(samples[i - 1].up, samples[i].up, t).normalized;
                return result;
            }

            accumulated += segLen;
        }

        // Fallback: trả về điểm cuối spline
        return samples[samples.Count - 1];
    }
    // ─── Public: Items tự gọi ReturnToPool khi bị collect ────

    /// <summary>
    /// Item script gọi hàm này thay vì Destroy(gameObject).
    /// ItemManager tự biết trả object về pool nào.
    /// </summary>
    public void ReturnToPool(GameObject item)
    {
        activeItems.Remove(item);
        ObjectPool pool = GetPoolByName(item);
        if (pool != null) pool.Return(item);
        else              item.SetActive(false);
    }

    private ObjectPool GetPoolByName(GameObject obj)
    {
        string n = obj.name.Replace("(Clone)", "").Trim();
        if (codeCommitData?.prefab != null && codeCommitData.prefab.name == n) return codeCommitPool;
        if (coffeeData?.prefab     != null && coffeeData.prefab.name     == n) return coffeePool;
        if (skinUpData?.prefab     != null && skinUpData.prefab.name     == n) return skinUpPool;
        if (obstacleData?.prefab   != null && obstacleData.prefab.name   == n) return obstaclePool;
        return null;
    }

    // ─── Weighted Random Pick ─────────────────────────────────

    private GameObject PickNormalItemPrefab()
    {
        var candidates = new List<(GameObject prefab, float weight)>();
        void Add(ItemSpawnData d) { if (d?.prefab != null) candidates.Add((d.prefab, d.spawnWeight)); }
        Add(codeCommitData); Add(coffeeData); Add(obstacleData);

        if (candidates.Count == 0) return null;

        float total = 0f;
        foreach (var c in candidates) total += c.weight;
        float roll = Random.Range(0f, total), cum = 0f;
        foreach (var c in candidates) { cum += c.weight; if (roll <= cum) return c.prefab; }
        return candidates[candidates.Count - 1].prefab;
    }

    // ─── Public API ───────────────────────────────────────────

    public void OnSkinUpCollected()
    {
        skinUpSpawnedCount++;
        Debug.Log($"[ItemManager] SkinUp {skinUpSpawnedCount}/{maxSkinUpCount}");
    }

    public void StopSpawning() { }
    public void StartSpawning() { }

    public void ClearAll()
    {
        foreach (var item in activeItems)
        {
            if (item == null) continue;
            ObjectPool pool = GetPoolByName(item);
            if (pool != null) pool.Return(item);
            else item.SetActive(false);
        }
        activeItems.Clear();
        skinUpSpawnedCount = 0;
    }

    public void Restart()
    {
        ClearAll();
        SpawnAllItems();
    }
}