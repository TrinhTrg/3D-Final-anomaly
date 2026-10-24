#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// AAH_QuickSetupTool — Công cụ tự động thiết lập Bệnh viện, Player, và Bệnh nhân
/// dựa trên đúng các model bạn đã tải về thư mục Assets/Models/
/// </summary>
public class AAH_QuickSetupTool : EditorWindow
{
    [MenuItem("Tools/AAH/Auto-Setup With Imported Models")]
    public static void ShowWindow()
    {
        GetWindow<AAH_QuickSetupTool>("AAH Model Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("🏥 ANOMALY ANIMAL HOSPITAL - QUICK SETUP", EditorStyles.boldLabel);
        GUILayout.Label("Tự động áp dụng model thực tế vào scene HospitalDay1.", EditorStyles.helpBox);
        GUILayout.Space(10);

        // 1. HOSPITAL BUILDING
        GUILayout.Label("1. BỆNH VIỆN & MÔI TRƯỜNG", EditorStyles.boldLabel);
        if (GUILayout.Button("🏢 Áp dụng Model Bệnh Viện (hospital_full / hospital_model)", GUILayout.Height(30)))
        {
            SetupHospitalEnvironment();
        }

        GUILayout.Space(10);

        // 2. PLAYER CHARACTER
        GUILayout.Label("2. NHẬP VAI NHÂN VẬT (PLAYER BODY)", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Gắn Player 01")) AttachPlayerBody("Player_01");
        if (GUILayout.Button("Gắn Player 02")) AttachPlayerBody("Player_02");
        if (GUILayout.Button("Gắn Player 03")) AttachPlayerBody("Player_03");
        if (GUILayout.Button("Gắn Player 04")) AttachPlayerBody("Player_04");
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 3. SPAWN PATIENTS & ANOMALIES
        GUILayout.Label("3. ĐƯA BỆNH NHÂN & DỊ THƯỜNG VÀO QUẦY LỄ TÂN", EditorStyles.boldLabel);
        if (GUILayout.Button("🐾 Thiết lập 3 Bệnh nhân (2 Bình thường + 1 Dị thường thực tế)", GUILayout.Height(35)))
        {
            SetupRealPatients();
        }

        GUILayout.Space(15);
        if (GUILayout.Button("💾 Lưu Scene (Save Scene)", GUILayout.Height(25)))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[AAH] Đã lưu Scene!");
        }
    }

    private static void SetupHospitalEnvironment()
    {
        // Ưu tiên hospital_full, nếu không thì lấy hospital_model
        string[] candidates = new string[] {
            "Assets/Models/Hospital/hospital_full.glb",
            "Assets/Models/Hospital/hospital_model.glb"
        };

        GameObject modelAsset = null;
        foreach (var path in candidates)
        {
            modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (modelAsset != null) break;
        }

        if (modelAsset == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file model bệnh viện trong Assets/Models/Hospital/!", "OK");
            return;
        }

        // Tìm hoặc tạo nhóm ENVIRONMENT
        GameObject envGroup = GameObject.Find("=== ENVIRONMENT ===");
        if (envGroup == null) envGroup = new GameObject("=== ENVIRONMENT ===");

        // Tắt hoặc dọn dẹp các khối hộp blockout cũ
        foreach (Transform child in envGroup.transform)
        {
            child.gameObject.SetActive(false);
        }

        // Tạo instance model bệnh viện
        GameObject hospInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        hospInstance.name = "Hospital_Building";
        hospInstance.transform.SetParent(envGroup.transform);
        hospInstance.transform.position = Vector3.zero;
        hospInstance.transform.rotation = Quaternion.identity;

        // Thêm collider tự động
        AAH_ModelSetup setup = hospInstance.AddComponent<AAH_ModelSetup>();
        setup.role = AAH_ModelSetup.ModelRole.Environment_HospitalBuilding;
        setup.AutoSetup();

        EditorUtility.DisplayDialog("Thành công", "Đã nạp model Bệnh viện và tạo MeshCollider chống xuyên sàn/tường!", "OK");
    }

    private static void AttachPlayerBody(string playerName)
    {
        string path = $"Assets/Models/Characters/Players/{playerName}.glb";
        GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (playerAsset == null)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy model {playerName} tại: {path}", "OK");
            return;
        }

        GameObject playerGO = GameObject.Find("Player");
        if (playerGO == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'Player' trong scene!", "OK");
            return;
        }

        // Xóa model cơ thể cũ nếu đã có
        Transform oldBody = playerGO.transform.Find("CharacterModel");
        if (oldBody != null) DestroyImmediate(oldBody.gameObject);

        // Tạo model cơ thể mới
        GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset);
        body.name = "CharacterModel";
        body.transform.SetParent(playerGO.transform);
        body.transform.localPosition = new Vector3(0, -1.0f, 0);
        body.transform.localRotation = Quaternion.identity;

        EditorUtility.DisplayDialog("Thành công", $"Đã gắn model {playerName} vào Player!", "OK");
    }

    private static void SetupRealPatients()
    {
        Transform patientGroup = GameObject.Find("=== PATIENTS ===")?.transform;
        if (patientGroup == null)
        {
            GameObject go = new GameObject("=== PATIENTS ===");
            patientGroup = go.transform;
        }

        // Xóa các khối placeholder cũ
        for (int i = patientGroup.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(patientGroup.GetChild(i).gameObject);
        }

        // 1. Patient thường 1: Hokaido
        CreatePatientInstance("Assets/Models/Animals/Patient/Hokaido.glb", "Patient_Hokaido", "Hokaido", false, new Vector3(-1.5f, 0f, 2.5f), patientGroup);

        // 2. Patient thường 2: Ron
        CreatePatientInstance("Assets/Models/Animals/Patient/Ron.glb", "Patient_Ron", "Ron", false, new Vector3(-3.0f, 0f, 2.5f), patientGroup);

        // 3. Anomaly: Moratter hoặc Ron Anomaly
        CreatePatientInstance("Assets/Models/Animals/Anomaly/Moratter.glb", "Patient_Anomaly_Moratter", "Moratter (Anomaly)", true, new Vector3(-4.5f, 0f, 2.5f), patientGroup);

        // Cập nhật lại danh sách hàng chờ cho quầy lễ tân
        AAH_ReceptionDesk desk = Object.FindFirstObjectByType<AAH_ReceptionDesk>();
        if (desk != null)
        {
            desk.patientObjects = new GameObject[patientGroup.childCount];
            for (int i = 0; i < patientGroup.childCount; i++)
            {
                desk.patientObjects[i] = patientGroup.GetChild(i).gameObject;
            }
            EditorUtility.SetDirty(desk);
        }

        // Chạy lại Setup UI để liên kết trigger
        AAH_UIBuilder.SetupUI();

        EditorUtility.DisplayDialog("Thành công", "Đã tạo 3 bệnh nhân bằng model thật (.glb) và kết nối với Quầy lễ tân & UI!", "OK");
    }

    private static void CreatePatientInstance(string assetPath, string objName, string displayName, bool isAnomaly, Vector3 pos, Transform parent)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[AAH] Không tìm thấy file: {assetPath}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.name = objName;
        instance.transform.SetParent(parent);
        instance.transform.position = pos;

        AAH_ModelSetup setup = instance.AddComponent<AAH_ModelSetup>();
        setup.role = isAnomaly ? AAH_ModelSetup.ModelRole.Patient_Anomaly : AAH_ModelSetup.ModelRole.Patient_Normal;
        setup.patientName = displayName;
        setup.AutoSetup();

        instance.SetActive(false); // Bắt đầu ẩn, quầy lễ tân sẽ kích hoạt lần lượt
    }
}
#endif
