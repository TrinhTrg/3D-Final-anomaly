using UnityEngine;

/// <summary>
/// AAH_ModelSetup — Attach this to ANY imported 3D model to quickly configure it
/// as a specific role in the hospital. Select the ModelRole in Inspector,
/// then click "Setup" context menu to auto-configure colliders, triggers, and scripts.
/// 
/// USAGE:
/// 1. Drag your .glb model into the scene
/// 2. Add this component to it
/// 3. Set the ModelRole (Patient, Furniture, NPC, etc.)
/// 4. Right-click component → "Auto Setup This Model"
/// </summary>
public class AAH_ModelSetup : MonoBehaviour
{
    public enum ModelRole
    {
        Patient_Normal,
        Patient_Anomaly,
        NPC_DrMupple,
        NPC_Polly,
        NPC_Brady,
        NPC_Clyde,
        NPC_OfficerDogman,
        Furniture_ReceptionDesk,
        Furniture_ExamTable,
        Furniture_Chair,
        Furniture_Bed,
        Equipment_Flashlight,
        Equipment_LaserGun,
        Equipment_Camera,
        Equipment_CoffeeMachine,
        Environment_HospitalBuilding,
        Monster_BedMonster,
        Monster_EyeMass,
        Monster_Slimebert,
        Prop_Generic
    }

    [Header("Configuration")]
    public ModelRole role = ModelRole.Prop_Generic;
    public string customName = "";

    [Header("Patient Settings (if Patient role)")]
    public string patientName = "Patient";
    
    [Header("Scale Override")]
    [Tooltip("Set to (0,0,0) to keep original scale")]
    public Vector3 scaleOverride = Vector3.zero;

    /// <summary>
    /// Call this from context menu or button to auto-setup the model.
    /// Right-click this component in Inspector → "Auto Setup This Model"
    /// </summary>
    [ContextMenu("Auto Setup This Model")]
    public void AutoSetup()
    {
        // Apply scale if specified
        if (scaleOverride != Vector3.zero)
            transform.localScale = scaleOverride;

        // Apply name
        if (!string.IsNullOrEmpty(customName))
            gameObject.name = customName;

        switch (role)
        {
            case ModelRole.Patient_Normal:
                SetupAsPatient(false);
                break;
            case ModelRole.Patient_Anomaly:
                SetupAsPatient(true);
                break;
            case ModelRole.NPC_DrMupple:
                SetupAsNPC("Dr. Mupple");
                break;
            case ModelRole.NPC_Polly:
                SetupAsNPC("Polly");
                break;
            case ModelRole.NPC_Brady:
                SetupAsNPC("Brady");
                break;
            case ModelRole.NPC_Clyde:
                SetupAsNPC("Clyde");
                break;
            case ModelRole.NPC_OfficerDogman:
                SetupAsNPC("Officer Dogman");
                break;
            case ModelRole.Furniture_ReceptionDesk:
                SetupAsFurniture("ReceptionDesk");
                break;
            case ModelRole.Furniture_ExamTable:
                SetupAsFurniture("ExamTable");
                break;
            case ModelRole.Furniture_Chair:
                SetupAsFurniture("Chair");
                break;
            case ModelRole.Furniture_Bed:
                SetupAsBed();
                break;
            case ModelRole.Equipment_Flashlight:
                SetupAsEquipment("Flashlight");
                break;
            case ModelRole.Equipment_LaserGun:
                SetupAsEquipment("LaserGun");
                break;
            case ModelRole.Equipment_Camera:
                SetupAsEquipment("CCTVCamera");
                break;
            case ModelRole.Equipment_CoffeeMachine:
                SetupAsEquipment("CoffeeMachine");
                break;
            case ModelRole.Environment_HospitalBuilding:
                SetupAsEnvironment();
                break;
            case ModelRole.Monster_BedMonster:
            case ModelRole.Monster_EyeMass:
            case ModelRole.Monster_Slimebert:
                SetupAsMonster(role.ToString());
                break;
            default:
                SetupAsGenericProp();
                break;
        }

        Debug.Log($"[AAH] Model '{gameObject.name}' setup complete as {role}");
    }

    void SetupAsPatient(bool isAnomaly)
    {
        gameObject.name = string.IsNullOrEmpty(customName) 
            ? (isAnomaly ? $"Patient_Anomaly_{patientName}" : $"Patient_{patientName}") 
            : customName;

        // Ensure collider exists on root or children
        EnsureCollider();

        // Add AAH_AnimalPatient component
        AAH_AnimalPatient animal = GetOrAddComponent<AAH_AnimalPatient>();
        animal.patientName = patientName;
        animal.isAnomaly = isAnomaly;

        // Create inspection trigger zone (child)
        GameObject triggerZone = transform.Find("InspectionTrigger")?.gameObject;
        if (triggerZone == null)
        {
            triggerZone = new GameObject("InspectionTrigger");
            triggerZone.transform.SetParent(transform);
            triggerZone.transform.localPosition = Vector3.zero;
        }

        SphereCollider sc = GetOrAddComponent<SphereCollider>(triggerZone);
        sc.radius = 1.8f;
        sc.isTrigger = true;

        AAH_PatientInspection inspection = GetOrAddComponent<AAH_PatientInspection>(triggerZone);
        inspection.isAnomaly = isAnomaly;
        inspection.patientName = patientName;

        // Connect
        animal.anomalyCueRenderer = null; // User can assign anomaly visual later
    }

    void SetupAsNPC(string npcName)
    {
        gameObject.name = string.IsNullOrEmpty(customName) ? $"NPC_{npcName}" : customName;
        EnsureCollider();

        // Add basic NPC trigger zone
        GameObject triggerZone = transform.Find("NPCTrigger")?.gameObject;
        if (triggerZone == null)
        {
            triggerZone = new GameObject("NPCTrigger");
            triggerZone.transform.SetParent(transform);
            triggerZone.transform.localPosition = Vector3.zero;
        }

        SphereCollider sc = GetOrAddComponent<SphereCollider>(triggerZone);
        sc.radius = 2.0f;
        sc.isTrigger = true;

        // Add Dr. Mupple script if applicable
        if (npcName == "Dr. Mupple")
        {
            AAH_DrMupple mupple = GetOrAddComponent<AAH_DrMupple>();
            mupple.muppleModel = gameObject;
        }
    }

    void SetupAsFurniture(string furnitureName)
    {
        gameObject.name = string.IsNullOrEmpty(customName) ? furnitureName : customName;
        
        // Add MeshCollider to all renderers if no collider exists
        EnsureColliderOnAllChildren();
        
        // Make static for optimization
        gameObject.isStatic = true;
    }

    void SetupAsBed()
    {
        gameObject.name = string.IsNullOrEmpty(customName) ? "HospitalBed" : customName;
        EnsureColliderOnAllChildren();

        // Add trigger under bed for BedMonster
        GameObject underBed = transform.Find("UnderBedTrigger")?.gameObject;
        if (underBed == null)
        {
            underBed = new GameObject("UnderBedTrigger");
            underBed.transform.SetParent(transform);
            underBed.transform.localPosition = new Vector3(0, -0.3f, 0);
        }

        BoxCollider box = GetOrAddComponent<BoxCollider>(underBed);
        box.size = new Vector3(1.5f, 0.5f, 2f);
        box.isTrigger = true;
    }

    void SetupAsEquipment(string equipName)
    {
        gameObject.name = string.IsNullOrEmpty(customName) ? equipName : customName;
        EnsureCollider();

        // Add pickup trigger
        GameObject pickupTrigger = transform.Find("PickupTrigger")?.gameObject;
        if (pickupTrigger == null)
        {
            pickupTrigger = new GameObject("PickupTrigger");
            pickupTrigger.transform.SetParent(transform);
            pickupTrigger.transform.localPosition = Vector3.zero;
        }

        SphereCollider sc = GetOrAddComponent<SphereCollider>(pickupTrigger);
        sc.radius = 1.2f;
        sc.isTrigger = true;
    }

    void SetupAsEnvironment()
    {
        gameObject.name = string.IsNullOrEmpty(customName) ? "Hospital_Environment" : customName;
        
        // Add MeshCollider to all meshes for walkable surfaces
        EnsureColliderOnAllChildren();
        
        // Make static
        gameObject.isStatic = true;
        SetStaticRecursive(transform);
    }

    void SetupAsMonster(string monsterType)
    {
        gameObject.name = string.IsNullOrEmpty(customName) ? monsterType : customName;
        EnsureCollider();

        // Add chase trigger
        GameObject chaseTrigger = transform.Find("ChaseTrigger")?.gameObject;
        if (chaseTrigger == null)
        {
            chaseTrigger = new GameObject("ChaseTrigger");
            chaseTrigger.transform.SetParent(transform);
            chaseTrigger.transform.localPosition = Vector3.zero;
        }

        SphereCollider sc = GetOrAddComponent<SphereCollider>(chaseTrigger);
        sc.radius = 5f;
        sc.isTrigger = true;

        // Reuse existing MonsterChase if available
        Rigidbody rb = GetOrAddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void SetupAsGenericProp()
    {
        EnsureCollider();
    }

    // ── UTILITY METHODS ──────────────────────────────────────────────

    void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            // Auto-size from renderers
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                box.center = rend.bounds.center - transform.position;
                box.size = rend.bounds.size;
            }
        }
    }

    void EnsureColliderOnAllChildren()
    {
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        foreach (var mf in meshes)
        {
            if (mf.GetComponent<Collider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }
    }

    void SetStaticRecursive(Transform t)
    {
        t.gameObject.isStatic = true;
        foreach (Transform child in t)
            SetStaticRecursive(child);
    }

    T GetOrAddComponent<T>() where T : Component
    {
        T comp = GetComponent<T>();
        if (comp == null) comp = gameObject.AddComponent<T>();
        return comp;
    }

    T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}
