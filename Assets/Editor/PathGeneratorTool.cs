using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class PathGeneratorTool : EditorWindow
{
    // Cài đặt công cụ
    public List<GameObject> pathModules = new List<GameObject>();
    public Transform pathRoot;
    public Transform endPointObject;

    // Trạng thái theo dõi
    private GameObject lastModule;
    private int selectedPreviewIndex = 0;
    private GameObject previewInstance;

    [MenuItem("Window/Intern Escape/Path Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<PathGeneratorTool>("Path Generator");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        ClearPreview();
    }

    private void OnGUI()
    {
        GUILayout.Label("   Lắp Ráp Đường Đua Vô Tận", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Phân khu Setup
        GUILayout.Label("1. Cài đặt Kho Chứa (Prefabs)", EditorStyles.label);
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty modulesProperty = so.FindProperty("pathModules");
        EditorGUILayout.PropertyField(modulesProperty, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Tẩy Tuỷ Các Module Cua Nhập (Tự động gắn Anchor)"))
        {
            AutoInjectAnchorsToPrefabs();
        }

        EditorGUILayout.Space();
        
        // Cài đặt Gốc
        pathRoot = (Transform)EditorGUILayout.ObjectField("Thư mục chứa đường (Khuyên dùng: Map/GenMap)", pathRoot, typeof(Transform), true);
        
        // Điểm Cán Đích (Tự chạy nhảy về đuôi map)
        endPointObject = (Transform)EditorGUILayout.ObjectField("Vật thể Cán Đích (End Point)", endPointObject, typeof(Transform), true);

        // Nút Khởi Tạo Lại
        if (GUILayout.Button("Xóa Sạch Bản Đồ Đang Nối & Bắt Đầu Lại Từ (0,0,0)"))
        {
            ClearAllAndStartFresh();
        }

        EditorGUILayout.Space();
        // 2. Chức năng Nối Đường
        GUILayout.Label("2. Thao Tác Nối Tự Động", EditorStyles.label);

        // Hiển thị Preview Toggle
        bool canPreview = pathModules.Count > 0 && lastModule != null;
        if (canPreview)
        {
            selectedPreviewIndex = EditorGUILayout.Popup("Xem Trước (Bóng ma): ", selectedPreviewIndex, pathModules.Select(m => m != null ? m.name : "None").ToArray());
            UpdatePreview();
        }
        else
        {
            ClearPreview();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Nối Đường Ngẫu Nhiên"))
        {
            if (pathModules.Count > 0)
            {
                int randomIdx = Random.Range(0, pathModules.Count);
                if (pathModules[randomIdx] != null)
                {
                    StitchModule(pathModules[randomIdx]);
                }
            }
        }

        if (GUILayout.Button(" Nối Mảnh Đang Xem Trước"))
        {
            if (pathModules.Count > 0 && selectedPreviewIndex < pathModules.Count)
            {
                if (pathModules[selectedPreviewIndex] != null)
                {
                    StitchModule(pathModules[selectedPreviewIndex]);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 3. Công cụ Hỗ Trợ Khác
        GUILayout.Label("3. Hỗ Trợ Đắc Lực", EditorStyles.label);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↩Hoàn Tác (Undo) Mảnh Vừa Lắp", GUILayout.Height(30)))
        {
            UndoLastModule();
        }

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Nướng Dữ Liệu Chạy (Bake Path)", GUILayout.Height(30)))
        {
            AutoBakePath();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Báo cáo vị trí Tracker
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(lastModule != null ? $"Đang nối tiếp vào đuôi của: {lastModule.name}" : "Bản đồ trống. Bấm Random hoặc Nối Mảnh để bắt đầu đặt viên gạch đầu tiên.", MessageType.Info);
    }

    private void ClearAllAndStartFresh()
    {
        if (pathRoot != null)
        {
            for (int i = pathRoot.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(pathRoot.GetChild(i).gameObject);
            }
        }
        else
        {
            // Tự khởi tạo môi trường
            GameObject rootObj = new GameObject("Environment_Generated");
            Undo.RegisterCreatedObjectUndo(rootObj, "Create Root");
            pathRoot = rootObj.transform;
            
            // Nếu chưa có GenMap và PathBaker thì tự gắn
            if (rootObj.GetComponent<GenMap>() == null)
                Undo.AddComponent<GenMap>(rootObj);
            if (rootObj.GetComponent<PathBaker>() == null)
                Undo.AddComponent<PathBaker>(rootObj);
        }

        lastModule = null;
        ClearPreview();
    }

    private void UndoLastModule()
    {
        // Kích hoạt Undo standard của Unity
        Undo.PerformUndo();

        // Cập nhật lại logic mẩu cuối cùng sau khi Undo
        if (pathRoot != null && pathRoot.childCount > 0)
        {
            lastModule = pathRoot.GetChild(pathRoot.childCount - 1).gameObject;
        }
        else
        {
            lastModule = null;
        }
        UpdatePreview();
    }

    private void StitchModule(GameObject prefab)
    {
        if (prefab == null) return;

        // Tính vị trí kết dính
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (lastModule != null)
        {
            CalculateExitTransform(lastModule, out spawnPos, out spawnRot);
        }

        // Đổ bóng xuất hiện
        GameObject newPath = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // CÂN ĐỐI 2 ĐẦU: Tìm móc nối Start của Prefab mới để ghép khớp với móc nối Exit của Prefab cũ.
        Transform anchorStart = newPath.transform.Find("Anchor_Start");
        Vector3 localStartPos = anchorStart != null ? anchorStart.localPosition : Vector3.zero;
        Quaternion localStartRot = anchorStart != null ? anchorStart.localRotation : Quaternion.identity;

        // Xoay sao cho mũi tên Start hướng cùng chiều với mũi tên Exit cũ
        newPath.transform.rotation = spawnRot * Quaternion.Inverse(localStartRot);
        // Tịnh tiến lùi lại khoảng cách từ tâm object tới Anchor_Start
        newPath.transform.position = spawnPos - newPath.transform.rotation * localStartPos;
        
        if (pathRoot != null)
        {
            newPath.transform.SetParent(pathRoot);
        }

        // Báo cho Unity Window biết việc vừa tạo để có thể Undo (Ctrl Z)
        Undo.RegisterCreatedObjectUndo(newPath, "Stitch Module " + prefab.name);

        lastModule = newPath;
        UpdatePreview();
    }

    // ─── THUẬT TOÁN TÍNH TÂM VÀ GÓC NỐI (ANCHOR SYSTEM) ───
    private void CalculateExitTransform(GameObject previousMod, out Vector3 outPos, out Quaternion outRot)
    {
        // TRANG BỊ 1: Tìm xem có mốc nào dev thả tay (Anchor_Exit) không?
        Transform anchor = previousMod.transform.Find("Anchor_Exit");
        if (anchor != null)
        {
            outPos = anchor.position;
            outRot = anchor.rotation;
            return;
        }

        // TRANG BỊ 2: Dùng thuật toán heuristic dò đỉnh đầu đường (Bounding Box của trần lưới)
        Renderer[] renderers = previousMod.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            // Giải định đường rượt đuổi kéo dài dọc theo Z mặt phẳng. Z lớn nhất chính là lối thoát.
            // Lấy độ dài theo trục Z trên toạ độ Local (World -> Local bounds)
            float localLengthZ = 0f;
            foreach (var r in renderers) {
                if (r is MeshRenderer mr && mr.GetComponent<MeshFilter>() != null) {
                    Bounds meshBounds = mr.GetComponent<MeshFilter>().sharedMesh.bounds;
                    float zMax = meshBounds.max.z * mr.transform.lossyScale.z;
                    if(zMax > localLengthZ) localLengthZ = zMax;
                }
            }

            if (localLengthZ <= 0) localLengthZ = combinedBounds.extents.z * 2f; // fall back

            outPos = previousMod.transform.position + previousMod.transform.forward * localLengthZ;
            outRot = previousMod.transform.rotation;
            return;
        }

        // TRANG BỊ MẶC ĐỊNH: Rơi vào hộp đen nếu mesh hỏng
        outPos = previousMod.transform.position;
        outRot = previousMod.transform.rotation;
    }

    // Gắn mũi tên ẩn vào Prefab nếu nó chưa có để lập trình viên tự chỉnh mốc Rẻ Cua
    private void AutoInjectAnchorsToPrefabs()
    {
        int injected = 0;
        foreach (GameObject prefab in pathModules)
        {
            if (prefab == null) continue;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
            if (string.IsNullOrEmpty(assetPath)) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(assetPath))
            {
                GameObject prefabRoot = editingScope.prefabContentsRoot;
                bool changed = false;

                // 1. Tiêm Anchor Khởi Đầu (Đầu vào)
                if (prefabRoot.transform.Find("Anchor_Start") == null)
                {
                    GameObject anchorStart = new GameObject("Anchor_Start");
                    anchorStart.transform.SetParent(prefabRoot.transform);
                    anchorStart.transform.localPosition = Vector3.zero;
                    anchorStart.transform.localRotation = Quaternion.identity;
                    changed = true;
                }

                // 2. Tiêm Anchor Kết Thúc (Đầu ra)
                if (prefabRoot.transform.Find("Anchor_Exit") == null)
                {
                    GameObject anchor = new GameObject("Anchor_Exit");
                    anchor.transform.SetParent(prefabRoot.transform);
                    
                    // Đo nhẹ Z Max cho đỡ vất vả
                    float guessedZ = 0f;
                    MeshFilter[] mfs = prefabRoot.GetComponentsInChildren<MeshFilter>();
                    foreach (var mf in mfs) {
                        if(mf.sharedMesh != null) {
                            float z = mf.sharedMesh.bounds.max.z * mf.transform.lossyScale.z;
                            if (z > guessedZ) guessedZ = z;
                        }
                    }

                    anchor.transform.localPosition = new Vector3(0, 0, guessedZ);
                    anchor.transform.localRotation = Quaternion.identity;
                    changed = true;
                }

                if (changed) injected++;
            }
        }
        Debug.Log($"[Path Generator] Đã tự động tiêm đinh rút (Anchor_Exit) vào {injected} Prefabs. Hãy mở Prefab đó lên và điều chỉnh góc toạ độ chữ Z nếu đó là đường cua.");
    }

    // ─── TÍNH NĂNG XEM TRƯỚC (BÓNG MA TRong SCENE) ───
    private void UpdatePreview()
    {
        if (pathModules.Count == 0 || lastModule == null)
        {
            ClearPreview();
            return;
        }

        GameObject prefab = pathModules[Mathf.Clamp(selectedPreviewIndex, 0, pathModules.Count - 1)];
        if (prefab == null) return;

        if (previewInstance == null || previewInstance.name != prefab.name + "_Preview")
        {
            ClearPreview();
            previewInstance = Instantiate(prefab);
            previewInstance.name = prefab.name + "_Preview";
            // Làm nó thành bóng ma không thể chọn được vào và không bị save vào màn
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            
            // Xóa hết collider đỡ cản trở chuột
            Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>();
            foreach(var c in colliders) DestroyImmediate(c); 
            
            // Biến nó thành trong suốt hoặc mờ
            Renderer[] rnds = previewInstance.GetComponentsInChildren<Renderer>();
            foreach(Renderer r in rnds) {
                // Rất phèn nhưng hiệu quả: làm tối các material đi
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    Material gMat = new Material(Shader.Find("Standard"));
                    gMat.color = new Color(0, 1, 0, 0.4f); // Xanh lá cây mờ
                    gMat.SetFloat("_Mode", 3); // Transparent
                    gMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    gMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    gMat.SetInt("_ZWrite", 0);
                    gMat.DisableKeyword("_ALPHATEST_ON");
                    gMat.EnableKeyword("_ALPHABLEND_ON");
                    gMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    gMat.renderQueue = 3000;
                    mats[i] = gMat;
                }
                r.sharedMaterials = mats;
            }
        }

        Vector3 pPos;
        Quaternion pRot;
        CalculateExitTransform(lastModule, out pPos, out pRot);

        Transform anchorStartPreview = previewInstance.transform.Find("Anchor_Start");
        Vector3 localStartPosP = anchorStartPreview != null ? anchorStartPreview.localPosition : Vector3.zero;
        Quaternion localStartRotP = anchorStartPreview != null ? anchorStartPreview.localRotation : Quaternion.identity;

        previewInstance.transform.rotation = pRot * Quaternion.Inverse(localStartRotP);
        previewInstance.transform.position = pPos - previewInstance.transform.rotation * localStartPosP;
    }

    private void ClearPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Luôn khoá preview bám đuôi theo vị trí mảnh cũ
        if (lastModule != null && previewInstance != null)
        {
            UpdatePreview();
        }
    }

    // ─── TÍCH HỢP TỰ ĐỘNG BAKE PATH ───
    private void AutoBakePath()
    {
        if (pathRoot == null)
        {
            Debug.LogError("Chưa chỉ định thư mục Path Root. Hãy gán vào ô số 1.");
            return;
        }

        PathBaker baker = pathRoot.GetComponent<PathBaker>();
        if (baker == null)
        {
            baker = Undo.AddComponent<PathBaker>(pathRoot.gameObject);
        }

        GenMap genMap = pathRoot.GetComponent<GenMap>();
        if (genMap == null)
        {
            Undo.AddComponent<GenMap>(pathRoot.gameObject);
        }

        // Chạy Bake Data từ các Module
        baker.BakePath();
        
        // Tự động kéo điểm Đích chốt chặn chặn ở Map cuối cùng
        if (endPointObject != null && genMap.splineSample != null && genMap.splineSample.Count > 0)
        {
            Undo.RecordObject(endPointObject, "Move End Point");
            var lastSample = genMap.splineSample[genMap.splineSample.Count - 1];
            endPointObject.position = lastSample.position;
            // Xoay khung thành vuông góc với trục đường và đứng thẳng bám dốc
            endPointObject.rotation = Quaternion.LookRotation(lastSample.forward, lastSample.up);
            Debug.Log($"Đã tự động kéo cụm Đích (End Point) dời về km cuối cùng tại toạ độ {lastSample.position}!");
        }

        // Cập nhật lên Editor báo hiệu file vừa thay đổi (để khi bấm Save, mảng Map được lưu trọn)
        EditorUtility.SetDirty(genMap);
        Debug.Log(" Đã Auto Bake hệ thống nốt đường thành công!!!");
    }
}
