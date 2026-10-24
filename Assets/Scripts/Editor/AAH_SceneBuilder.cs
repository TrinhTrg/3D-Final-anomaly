#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// AAH_SceneBuilder — Editor utility to automatically build the
/// "HospitalDay1" prototype scene for Anomaly Animal Hospital.
/// 
/// Access via: Unity menu → Tools → AAH → Build Hospital Day 1 Scene
/// 
/// This creates:
/// - Hospital level blockout (walls, floor, ceiling, rooms)
/// - Player (FPS Controller from existing project)
/// - 3 Animal Patient objects (2 normal, 1 anomaly)
/// - Reception desk with interaction trigger
/// - Examination room
/// - UI Canvas with HUD
/// - Lighting setup
/// - GameManager
/// </summary>
public class AAH_SceneBuilder : EditorWindow
{
    [MenuItem("Tools/AAH/Build Hospital Day 1 Scene")]
    public static void BuildScene()
    {
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── LIGHTING ────────────────────────────────────────────────
        SetupLighting();

        // ── ENVIRONMENT ─────────────────────────────────────────────
        GameObject environment = new GameObject("=== ENVIRONMENT ===");
        BuildHospitalLayout(environment.transform);

        // ── PLAYER ──────────────────────────────────────────────────
        GameObject player = BuildPlayer();

        // ── PATIENTS ────────────────────────────────────────────────
        GameObject patients = new GameObject("=== PATIENTS ===");
        BuildPatients(patients.transform);

        // ── MANAGERS ────────────────────────────────────────────────
        GameObject managers = new GameObject("=== MANAGERS ===");
        BuildManagers(managers.transform);

        // ── UI ──────────────────────────────────────────────────────
        BuildUI();

        // ── SAVE SCENE ──────────────────────────────────────────────
        string scenePath = "Assets/Scenes/HospitalDay1.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log("[AAH] Hospital Day 1 scene built and saved to: " + scenePath);
        EditorUtility.DisplayDialog("AAH Scene Builder",
            "Scene 'HospitalDay1' built successfully!\n\n" +
            "Location: Assets/Scenes/HospitalDay1.unity\n\n" +
            "Next steps:\n" +
            "1. Open the scene\n" +
            "2. Wire up the UI references in the Inspectors\n" +
            "3. Press Play to test",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────
    // LIGHTING
    // ─────────────────────────────────────────────────────────────────
    static void SetupLighting()
    {
        // Directional light — dim, slightly blue hospital vibe
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.2f, 0.25f, 0.35f);
        light.intensity = 0.3f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Point light at reception — warm
        CreatePointLight("Light_Reception", new Vector3(0, 2.5f, 2f), new Color(1f, 0.9f, 0.7f), 1.2f, 8f);

        // Point light at exam room
        CreatePointLight("Light_ExamRoom", new Vector3(8, 2.5f, 2f), new Color(0.8f, 1f, 0.8f), 1.0f, 8f);

        // Ambient — very dark
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 10f;
        RenderSettings.fogEndDistance = 25f;
    }

    static void CreatePointLight(string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
    }

    // ─────────────────────────────────────────────────────────────────
    // HOSPITAL LAYOUT
    // ─────────────────────────────────────────────────────────────────
    static void BuildHospitalLayout(Transform parent)
    {
        // Floor
        CreateBox("Floor", parent, new Vector3(0, -0.05f, 5f), new Vector3(20f, 0.1f, 20f), GetMat("FloorMat"));

        // Ceiling
        CreateBox("Ceiling", parent, new Vector3(0, 3.1f, 5f), new Vector3(20f, 0.1f, 20f), GetMat("CeilingMat"));

        // ─── WALLS ───
        // Back wall (behind reception)
        CreateBox("Wall_Back", parent, new Vector3(0, 1.5f, -5f), new Vector3(20f, 3f, 0.2f), GetMat("WallMat"));
        // Front wall
        CreateBox("Wall_Front", parent, new Vector3(0, 1.5f, 15f), new Vector3(20f, 3f, 0.2f), GetMat("WallMat"));
        // Left wall
        CreateBox("Wall_Left", parent, new Vector3(-10f, 1.5f, 5f), new Vector3(0.2f, 3f, 20f), GetMat("WallMat"));
        // Right wall
        CreateBox("Wall_Right", parent, new Vector3(10f, 1.5f, 5f), new Vector3(0.2f, 3f, 20f), GetMat("WallMat"));

        // ─── HALLWAY DIVIDER WALL ───
        // Wall separating reception from hallway (with door gap)
        CreateBox("Wall_DivLeft", parent, new Vector3(-4f, 1.5f, 5f), new Vector3(12f, 3f, 0.2f), GetMat("WallMat"));
        CreateBox("Wall_DivRight", parent, new Vector3(7f, 1.5f, 5f), new Vector3(6f, 3f, 0.2f), GetMat("WallMat"));
        // Door frame top
        CreateBox("Wall_DivDoorTop", parent, new Vector3(2f, 2.7f, 5f), new Vector3(2f, 0.6f, 0.2f), GetMat("WallMat"));

        // ─── EXAM ROOM ───
        // Right side room
        CreateBox("ExamRoom_Wall_Left", parent, new Vector3(4f, 1.5f, 10f), new Vector3(0.2f, 3f, 10f), GetMat("WallMat"));
        CreateBox("ExamRoom_Wall_Back", parent, new Vector3(7f, 1.5f, 14.9f), new Vector3(6f, 3f, 0.2f), GetMat("WallMat"));

        // ─── RECEPTION DESK ───
        CreateBox("ReceptionDesk_Top", parent, new Vector3(0, 0.9f, 1f), new Vector3(3f, 0.1f, 1.2f), GetMat("TableMat"));
        CreateBox("ReceptionDesk_Front", parent, new Vector3(0, 0.45f, 1.6f), new Vector3(3f, 0.9f, 0.1f), GetMat("TableMat"));
        CreateBox("ReceptionDesk_Back", parent, new Vector3(0, 0.45f, 0.4f), new Vector3(3f, 0.9f, 0.1f), GetMat("TableMat"));
        CreateBox("ReceptionDesk_Left", parent, new Vector3(-1.5f, 0.45f, 1f), new Vector3(0.1f, 0.9f, 1.2f), GetMat("TableMat"));
        CreateBox("ReceptionDesk_Right", parent, new Vector3(1.5f, 0.45f, 1f), new Vector3(0.1f, 0.9f, 1.2f), GetMat("TableMat"));

        // ─── EXAM TABLE ───
        CreateBox("ExamTable_Top", parent, new Vector3(7f, 0.75f, 10f), new Vector3(2f, 0.1f, 1f), GetMat("TableMat"));
        CreateBox("ExamTable_Leg1", parent, new Vector3(6.2f, 0.35f, 9.6f), new Vector3(0.1f, 0.7f, 0.1f), GetMat("TableMat"));
        CreateBox("ExamTable_Leg2", parent, new Vector3(7.8f, 0.35f, 9.6f), new Vector3(0.1f, 0.7f, 0.1f), GetMat("TableMat"));
        CreateBox("ExamTable_Leg3", parent, new Vector3(6.2f, 0.35f, 10.4f), new Vector3(0.1f, 0.7f, 0.1f), GetMat("TableMat"));
        CreateBox("ExamTable_Leg4", parent, new Vector3(7.8f, 0.35f, 10.4f), new Vector3(0.1f, 0.7f, 0.1f), GetMat("TableMat"));

        // Waiting area chairs
        BuildChair("Chair_1", parent, new Vector3(-6f, 0, 1f));
        BuildChair("Chair_2", parent, new Vector3(-7f, 0, 1f));
        BuildChair("Chair_3", parent, new Vector3(-8f, 0, 1f));
    }

    static void BuildChair(string name, Transform parent, Vector3 pos)
    {
        GameObject chair = new GameObject(name);
        chair.transform.SetParent(parent);
        chair.transform.position = pos;
        // Seat
        CreateBox("Seat", chair.transform, pos + new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.1f, 0.6f), GetMat("TableMat"));
        // Back
        CreateBox("Back", chair.transform, pos + new Vector3(0, 0.7f, -0.3f), new Vector3(0.6f, 0.8f, 0.1f), GetMat("TableMat"));
    }

    // ─────────────────────────────────────────────────────────────────
    // PLAYER
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildPlayer()
    {
        // Player capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1f, -3f);
        player.GetComponent<Renderer>().enabled = false; // invisible in FPS

        // Set Player tag
        player.tag = "Player";

        // Remove Capsule mesh collider, add CharacterController
        DestroyImmediate(player.GetComponent<CapsuleCollider>());
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0, 0);

        // FPS Controller (existing script)
        SC_FPSController fps = player.AddComponent<SC_FPSController>();
        fps.walkingSpeed = 3f;
        fps.runningSpeed = 5f;

        // Camera
        GameObject camGO = new GameObject("PlayerCamera");
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0, 0.8f, 0);
        Camera cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = 80f;
        camGO.tag = "MainCamera";

        // Flashlight removed - hands are kept empty

        // Wire up FPS controller's camera reference
        fps.playerCamera = cam;

        // Anomaly Detection on player
        AAH_AnomalyDetection detection = player.AddComponent<AAH_AnomalyDetection>();
        detection.playerCamera = cam;
        detection.detectionRange = 6f;

        Debug.Log("[AAH] Player built at " + player.transform.position);
        return player;
    }

    // ─────────────────────────────────────────────────────────────────
    // PATIENTS
    // ─────────────────────────────────────────────────────────────────
    static void BuildPatients(Transform parent)
    {
        // Day 1: 3 patients, 0 anomalies (anomaly visual for testing = 1 anomaly)
        // For Week 2 prototype: 2 normal, 1 anomaly
        BuildPatient("Patient_Biscuit", parent, new Vector3(-6f, 0.5f, 0.8f), false, "Biscuit the Rabbit");
        BuildPatient("Patient_Spots", parent, new Vector3(-7.5f, 0.5f, 0.8f), false, "Spots the Dalmatian");
        BuildPatient("Patient_Anomaly_Rex", parent, new Vector3(-9f, 0.5f, 0.8f), true, "Rex");

        // Start patients hidden (reception desk will activate them)
        // AAH_ReceptionDesk handles this
    }

    static GameObject BuildPatient(string name, Transform parent, Vector3 pos, bool isAnomaly, string patientName)
    {
        // Root
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = pos;

        // Body (cube — placeholder animal body)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0, 0);
        body.transform.localScale = new Vector3(0.5f, 0.4f, 0.7f);

        // Color
        Renderer bodyRend = body.GetComponent<Renderer>();
        Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyMat.color = isAnomaly ? new Color(0.6f, 0.4f, 0.4f) : new Color(0.85f, 0.75f, 0.6f);
        bodyRend.material = bodyMat;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 0.35f, 0.25f);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Renderer headRend = head.GetComponent<Renderer>();
        headRend.material = bodyMat;

        // Normal eyes
        CreateEye("Eye_L", head.transform, new Vector3(-0.1f, 0.05f, 0.17f), Color.black, 0.05f);
        CreateEye("Eye_R", head.transform, new Vector3(0.1f, 0.05f, 0.17f), Color.black, 0.05f);

        // Anomaly cue: Third eye (hidden by default)
        GameObject thirdEye = null;
        if (isAnomaly)
        {
            thirdEye = CreateEye("Eye_Third", head.transform, new Vector3(0, 0.12f, 0.18f), Color.red, 0.07f);
            thirdEye.SetActive(false); // Hidden until CCTV or inspection
        }

        // ── Inspection Trigger Zone ──
        GameObject triggerZone = new GameObject("InspectionTrigger");
        triggerZone.transform.SetParent(root.transform);
        triggerZone.transform.localPosition = Vector3.zero;

        SphereCollider sc = triggerZone.AddComponent<SphereCollider>();
        sc.radius = 1.5f;
        sc.isTrigger = true;

        // Patient inspection component
        AAH_PatientInspection inspection = triggerZone.AddComponent<AAH_PatientInspection>();
        inspection.isAnomaly = isAnomaly;
        inspection.patientName = patientName;
        inspection.anomalyCueObject = thirdEye;
        // UI refs wired manually in Inspector after scene is built

        // ── Animal Patient component on root ──
        AAH_AnimalPatient animalPatient = root.AddComponent<AAH_AnimalPatient>();
        animalPatient.patientName = patientName;
        animalPatient.isAnomaly = isAnomaly;
        animalPatient.anomalyCueRenderer = thirdEye;

        // Start hidden (reception desk activates them)
        root.SetActive(false);

        return root;
    }

    static GameObject CreateEye(string name, Transform parent, Vector3 localPos, Color color, float radius)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = name;
        eye.transform.SetParent(parent);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = Vector3.one * radius;

        Material eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        eyeMat.color = color;

        if (color == Color.red)
        {
            // Emissive red for anomaly eye
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", Color.red * 2f);
        }

        eye.GetComponent<Renderer>().material = eyeMat;
        DestroyImmediate(eye.GetComponent<Collider>()); // no collider on eyes
        return eye;
    }

    // ─────────────────────────────────────────────────────────────────
    // MANAGERS
    // ─────────────────────────────────────────────────────────────────
    static void BuildManagers(Transform parent)
    {
        // GameManager
        GameObject gmGO = new GameObject("GameManager");
        gmGO.transform.SetParent(parent);
        AAH_GameManager gm = gmGO.AddComponent<AAH_GameManager>();
        gm.currentDay = 1;
        gm.patientsToInspect = 3;
        gm.anomaliesPresent = 0;

        // Reception Desk Manager
        GameObject rdGO = new GameObject("ReceptionDeskManager");
        rdGO.transform.SetParent(parent);
        rdGO.transform.position = new Vector3(0, 0, 1f);
        BoxCollider rdCol = rdGO.AddComponent<BoxCollider>();
        rdCol.size = new Vector3(4f, 2f, 2f);
        rdCol.isTrigger = true;
        AAH_ReceptionDesk desk = rdGO.AddComponent<AAH_ReceptionDesk>();
        desk.useManualBell = false;
        desk.timeBetweenArrivals = 15.0f;

        Debug.Log("[AAH] Managers created. Wire UI references in Inspector!");
    }

    // ─────────────────────────────────────────────────────────────────
    // UI CANVAS
    // ─────────────────────────────────────────────────────────────────
    static void BuildUI()
    {
        Debug.Log("[AAH] UI must be wired manually in Inspector after scene opens.");
        // Note: Full Canvas setup via script requires complex UGUI operations.
        // The instructions below tell the developer how to set up UI manually.
        // A GameObject is created as a placeholder.
        GameObject uiNote = new GameObject("_UI_SETUP_REQUIRED");
        uiNote.transform.position = Vector3.zero;
        // See Phase D setup guide for UI wiring instructions.
    }

    // ─────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────
    static GameObject CreateBox(string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = worldPos;
        go.transform.localScale = scale;
        if (mat != null)
            go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static Material GetMat(string matName)
    {
        string path = $"Assets/Materials/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            // Default colors per type
            if (matName.Contains("Floor")) mat.color = new Color(0.35f, 0.35f, 0.38f);
            else if (matName.Contains("Ceiling")) mat.color = new Color(0.2f, 0.2f, 0.22f);
            else if (matName.Contains("Wall")) mat.color = new Color(0.55f, 0.55f, 0.6f);
            else if (matName.Contains("Table")) mat.color = new Color(0.4f, 0.35f, 0.3f);
        }
        return mat;
    }
}
#endif
