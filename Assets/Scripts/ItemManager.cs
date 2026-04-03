using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý việc tạo vật phẩm (Items) dọc theo đường chạy sử dụng kỹ thuật Object Pooling 
/// để tối ưu hiệu năng (tránh khởi tạo/hủy liên tục).
/// </summary>
public class ItemManager : MonoBehaviour
{
    /// <summary>
    /// Bản thực thi duy nhất của ItemManager.
    /// </summary>
    public static ItemManager Instance { get; private set; }

    /// <summary>
    /// Dữ liệu cấu hình cho từng loại vật phẩm.
    /// </summary>
    [System.Serializable]
    public class ItemSpawnData
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float spawnWeight = 0.33f;
        [Tooltip("Số objects tạo sẵn trong pool")]
        public int poolSize = 30;
    }

    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Item Prefabs")]
    public ItemSpawnData codeCommitData;
    public ItemSpawnData coffeeData;
    public ItemSpawnData skinUpData;
    public ItemSpawnData obstacleData;

    [Header("Path Settings")]
    [Tooltip("Khoảng cách giữa các hàng vật phẩm")]
    public float spawnSpacing = 5f;
    [Tooltip("Độ cao so với mặt sàn của vật phẩm")]
    public float itemY = 2.5f;

    [Header("Lane Settings")]
    public float[] lanes = { -5f, 0f, 5f };

    [Header("SkinUp Limit")]
    public int maxSkinUpCount = 3;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS — Object Pools
    // ═══════════════════════════════════════════════════════════

    private ObjectPool _codeCommitPool;
    private ObjectPool _coffeePool;
    private ObjectPool _skinUpPool;
    private ObjectPool _obstaclePool;
    private Transform _poolContainer;

    private List<GameObject> _activeItems = new List<GameObject>();
    private int _skinUpSpawnedCount = 0;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    //  POOL MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Khởi tạo các thùng chứa (Pools) cho từng loại vật phẩm.
    /// </summary>
    private void InitPools()
    {
        _poolContainer = new GameObject("[ItemPool]").transform;
        _poolContainer.SetParent(transform);

        if (codeCommitData?.prefab != null)
            _codeCommitPool = new ObjectPool(codeCommitData.prefab, codeCommitData.poolSize, _poolContainer);
        if (coffeeData?.prefab != null)
            _coffeePool = new ObjectPool(coffeeData.prefab, coffeeData.poolSize, _poolContainer);
        if (skinUpData?.prefab != null)
            _skinUpPool = new ObjectPool(skinUpData.prefab, skinUpData.poolSize, _poolContainer);
        if (obstacleData?.prefab != null)
            _obstaclePool = new ObjectPool(obstacleData.prefab, obstacleData.poolSize, _poolContainer);
    }

    private ObjectPool GetPool(GameObject prefab)
    {
        if (prefab == codeCommitData?.prefab) return _codeCommitPool;
        if (prefab == coffeeData?.prefab) return _coffeePool;
        if (prefab == skinUpData?.prefab) return _skinUpPool;
        if (prefab == obstacleData?.prefab) return _obstaclePool;
        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  SPAWN LOGIC
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Rải toàn bộ vật phẩm dựa trên dữ liệu đường cong Spline từ GenMap.
    /// </summary>
    private void SpawnAllItems()
    {
        if (GenMap.Instance == null || GenMap.Instance.splineSample == null || GenMap.Instance.splineSample.Count < 2)
        {
            Debug.LogWarning("[ItemManager] GenMap chưa có data!");
            return;
        }

        var samples = GenMap.Instance.splineSample;
        float totalLength = 0f;
        for (int i = 1; i < samples.Count; i++)
            totalLength += Vector3.Distance(samples[i - 1].position, samples[i].position);

        int rowCount = Mathf.FloorToInt(totalLength / spawnSpacing);
        if (rowCount <= 0) return;

        HashSet<int> skinUpRows = CalculateSkinUpRows(rowCount);

        for (int row = 1; row <= rowCount; row++)
        {
            float targetDist = row * spawnSpacing;
            PathSample sample = GetSampleAtDistance(samples, targetDist);

            if (skinUpRows.Contains(row))
                SpawnSkinUp(sample);
            else
                SpawnRowItems(sample);
        }
    }

    private HashSet<int> CalculateSkinUpRows(int rowCount)
    {
        HashSet<int> rows = new HashSet<int>();
        if (_skinUpPool != null && maxSkinUpCount > 0)
        {
            for (int i = 1; i <= maxSkinUpCount; i++)
            {
                int r = Mathf.RoundToInt(rowCount * ((float)i / (maxSkinUpCount + 1)));
                rows.Add(Mathf.Clamp(r, 1, rowCount));
            }
        }
        return rows;
    }

    private void SpawnSkinUp(PathSample sample)
    {
        int mid = lanes.Length / 2;
        Vector3 laneOffset = sample.right * lanes[mid];
        Vector3 pos = new Vector3(sample.position.x + laneOffset.x, sample.position.y + itemY, sample.position.z + laneOffset.z);
        Quaternion curveRot = Quaternion.LookRotation(sample.forward, sample.up);
        GameObject obj = _skinUpPool.Get(pos, curveRot * skinUpData.prefab.transform.rotation);
        _activeItems.Add(obj);
    }

    private void SpawnRowItems(PathSample sample)
    {
        int bugCountInRow = 0;

        for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
        {
            GameObject prefab = PickNormalItemPrefab();

            if (prefab != null && obstacleData != null && prefab == obstacleData.prefab)
            {
                bugCountInRow++;
                if (bugCountInRow >= lanes.Length)
                {
                    prefab = (Random.value > 0.1f && codeCommitData?.prefab != null) ? codeCommitData.prefab : coffeeData?.prefab;
                }
            }

            if (prefab == null) continue;
            ObjectPool pool = GetPool(prefab);
            if (pool == null) continue;

            Vector3 laneOffset = sample.right * lanes[laneIndex];
            Vector3 pos = new Vector3(sample.position.x + laneOffset.x, sample.position.y + itemY, sample.position.z + laneOffset.z);
            Quaternion curveRot = Quaternion.LookRotation(sample.forward, sample.up);
            GameObject obj = pool.Get(pos, curveRot * prefab.transform.rotation);
            _activeItems.Add(obj);
        }
    }

    private PathSample GetSampleAtDistance(List<PathSample> samples, float targetDist)
    {
        float accumulated = 0f;
        for (int i = 1; i < samples.Count; i++)
        {
            float segLen = Vector3.Distance(samples[i - 1].position, samples[i].position);
            if (accumulated + segLen >= targetDist)
            {
                float t = (targetDist - accumulated) / segLen;
                PathSample result = new PathSample();
                result.position = Vector3.Lerp(samples[i - 1].position, samples[i].position, t);
                result.forward = Vector3.Slerp(samples[i - 1].forward, samples[i].forward, t).normalized;
                result.up = Vector3.Slerp(samples[i - 1].up, samples[i].up, t).normalized;
                // result.right được tự động tính trong class PathSample qua Vector3.Cross(up, forward)
                return result;
            }
            accumulated += segLen;
        }
        return samples[samples.Count - 1];
    }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════════

    public void ReturnToPool(GameObject item)
    {
        _activeItems.Remove(item);
        ObjectPool pool = GetPoolByName(item);
        if (pool != null) pool.Return(item);
        else item.SetActive(false);
    }

    private ObjectPool GetPoolByName(GameObject obj)
    {
        string n = obj.name.Replace("(Clone)", "").Trim();
        if (codeCommitData?.prefab != null && codeCommitData.prefab.name == n) return _codeCommitPool;
        if (coffeeData?.prefab != null && coffeeData.prefab.name == n) return _coffeePool;
        if (skinUpData?.prefab != null && skinUpData.prefab.name == n) return _skinUpPool;
        if (obstacleData?.prefab != null && obstacleData.prefab.name == n) return _obstaclePool;
        return null;
    }

    private GameObject PickNormalItemPrefab()
    {
        var candidates = new List<(GameObject prefab, float weight)>();
        if (codeCommitData?.prefab != null) candidates.Add((codeCommitData.prefab, codeCommitData.spawnWeight));
        if (coffeeData?.prefab != null) candidates.Add((coffeeData.prefab, coffeeData.spawnWeight));
        if (obstacleData?.prefab != null) candidates.Add((obstacleData.prefab, obstacleData.spawnWeight));

        if (candidates.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var c in candidates) totalWeight += c.weight;
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var c in candidates)
        {
            cumulative += c.weight;
            if (roll <= cumulative) return c.prefab;
        }
        return candidates[candidates.Count - 1].prefab;
    }

    public void OnSkinUpCollected()
    {
        _skinUpSpawnedCount++;
    }

    public void StopSpawning() { }

    public void ClearAll()
    {
        foreach (var item in _activeItems)
        {
            if (item == null) continue;
            ObjectPool pool = GetPoolByName(item);
            if (pool != null) pool.Return(item);
            else item.SetActive(false);
        }
        _activeItems.Clear();
        _skinUpSpawnedCount = 0;
    }

    public void Restart()
    {
        ClearAll();
        SpawnAllItems();
    }
}