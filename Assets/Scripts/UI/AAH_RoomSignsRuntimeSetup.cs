using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-50)]
public class AAH_RoomSignsRuntimeSetup : MonoBehaviour
{
    private static bool _initialized = false;
    private readonly System.Collections.Generic.Dictionary<string, MeshFilter> _signModels =
        new System.Collections.Generic.Dictionary<string, MeshFilter>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInit()
    {
        _initialized = false;
    }

    void Awake()
    {
        if (_initialized) return;
        _initialized = true;
        SetupSigns();
    }

    [ContextMenu("Setup Signs Now")]
    public void SetupSigns()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0) font = fonts[0];
        }

        Transform container = transform.Find("RoomSigns_Labels");
        if (container != null)
        {
            DestroyImmediate(container.gameObject);
        }

        // Only inspect sign models in this scene; leave unrelated text and UI alone.
        _signModels.Clear();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (MeshFilter model in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (model.name.StartsWith("name_") || model.name.StartsWith("number_"))
                    _signModels[model.name] = model;
            }
        }

        GameObject containerGO = new GameObject("RoomSigns_Labels");
        // Preserve identity world rotation and positive unit scale, even if the manager is rotated.
        containerGO.transform.SetParent(transform, true);

        // 1. name_store_room: "Store"
        // Model min/max: X [-157.63, -157.31], Y [-15.90, -11.22], Z [-13.92, -12.12]
        // Center: X=157.47, Y=13.02, Z=-13.56. Thin in X (thickness=0.32).
        // Two faces: +X (towards corridor/lobby at X < 157.47? Wait, let's look at rotation and sides)
        CreateDoubleSidedSign(containerGO.transform, "Label_name_store_room", "Store",
            new Vector3(157.47f, 13.02f, -13.56f),
            new Vector2(4.68f, 1.80f),
            Quaternion.Euler(0, 90, 0),
            0.18f, font, 2.8f);

        // 2. name_reception_room: "Reception"
        // Center: X=118.70, Y=10.05, Z=-4.96. Thin in Z (Y in model).
        CreateDoubleSidedSign(containerGO.transform, "Label_name_reception_room", "Reception",
            new Vector3(118.70f, 10.05f, -4.96f),
            new Vector2(4.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 2.4f);

        // 3. name_medical_wing: "1-5"
        // Center: X=144.94, Y=13.00, Z=-28.34. Thin in Z (Y in model).
        CreateDoubleSidedSign(containerGO.transform, "Label_name_medical_wing", "1-5",
            new Vector3(144.94f, 13.00f, -28.34f),
            new Vector2(4.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.2f);

        // 4. name_emergency_wing: "6-10"
        // Center: X=145.07, Y=13.02, Z=1.24. Thin in Z (Y in model).
        CreateDoubleSidedSign(containerGO.transform, "Label_name_emergency_wing", "6-10",
            new Vector3(145.07f, 13.02f, 1.24f),
            new Vector2(4.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.2f);

        // 5. number_room_01 to number_room_08 (from model)
        // number_room_01 -> "1": (155.16, 10.18, -50.58)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_01", "1",
            new Vector3(155.16f, 10.18f, -50.58f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_02 -> "2": (134.34, 10.20, -50.76)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_02", "2",
            new Vector3(134.34f, 10.20f, -50.76f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_03 -> "3": (155.16, 10.20, -89.28)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_03", "3",
            new Vector3(155.16f, 10.20f, -89.28f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_04 -> "4": (134.34, 10.20, -89.58)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_04", "4",
            new Vector3(134.34f, 10.20f, -89.58f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_05 -> "5": (144.84, 10.10, -103.34)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_05", "5",
            new Vector3(144.84f, 10.10f, -103.34f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_06 -> "6": (134.94, 10.20, 31.44)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_06", "6",
            new Vector3(134.94f, 10.20f, 31.44f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_07 -> "7": (155.86, 10.18, 31.42)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_07", "7",
            new Vector3(155.86f, 10.18f, 31.42f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // number_room_08 -> "8": (145.20, 10.28, 71.33)
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_08", "8",
            new Vector3(145.20f, 10.28f, 71.33f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // 6. number_room_09 and number_room_10:
        // In this wing (rooms 6-10):
        // Room 6: X=134.94, Z=31.44
        // Room 7: X=155.86, Z=31.42
        // Room 8: X=145.20, Z=71.33
        // Room 9 and 10 sign positions follow the corridor pattern:
        // Room 9 (left wall): X=134.94, Y=10.20, Z=51.38
        // Room 10 (right wall): X=155.86, Y=10.18, Z=51.38
        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_09", "9",
            new Vector3(134.94f, 10.20f, 51.38f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        CreateDoubleSidedSign(containerGO.transform, "Label_number_room_10", "10",
            new Vector3(155.86f, 10.18f, 51.38f),
            new Vector2(3.68f, 1.80f),
            Quaternion.Euler(0, 0, 0),
            0.18f, font, 3.5f);

        // These are the corresponding existing models in HospitalDay1.
        CreateDoubleSidedSign(containerGO.transform, "Label_name_heart_scan_room_06", "Heart Scan",
            Vector3.zero, new Vector2(4.68f, 1.80f), Quaternion.identity, 0.18f, font, 2.4f);
        CreateDoubleSidedSign(containerGO.transform, "Label_name_x-ray_room_07", "X-Ray",
            Vector3.zero, new Vector2(4.68f, 1.80f), Quaternion.identity, 0.18f, font, 2.4f);
        CreateDoubleSidedSign(containerGO.transform, "Label_name_scan_x-ray_room_07", "X-Ray",
            Vector3.zero, new Vector2(4.68f, 1.80f), Quaternion.identity, 0.18f, font, 2.4f);

        Debug.Log("[AAH_RoomSignsRuntimeSetup] Successfully created all visible TMP signs!");
    }

    void CreateDoubleSidedSign(Transform parent, string name, string text, Vector3 center, Vector2 size, Quaternion rotation, float halfThickness, TMP_FontAsset font, float fontSize)
    {
        string modelName = name.StartsWith("Label_") ? name.Substring(6) : name;
        if (_signModels.TryGetValue(modelName, out MeshFilter model) && model.sharedMesh != null)
        {
            Bounds bounds = model.sharedMesh.bounds;
            Transform modelTransform = model.transform;
            bool thinInX = bounds.size.x < bounds.size.y;
            Vector3 normalAxis = thinInX ? Vector3.right : Vector3.up;
            Vector3 widthAxis = thinInX ? Vector3.up : Vector3.right;
            Vector3 normal = modelTransform.TransformVector(normalAxis).normalized;
            Vector3 up = modelTransform.TransformVector(Vector3.back).normalized;
            center = modelTransform.TransformPoint(bounds.center);
            rotation = Quaternion.LookRotation(normal, up);
            size = new Vector2(
                (thinInX ? bounds.size.y : bounds.size.x) * modelTransform.TransformVector(widthAxis).magnitude,
                bounds.size.z * modelTransform.TransformVector(Vector3.back).magnitude);
            halfThickness = (thinInX ? bounds.extents.x : bounds.extents.y)
                * modelTransform.TransformVector(normalAxis).magnitude;
        }

        GameObject signRoot = new GameObject(name);
        signRoot.transform.SetParent(parent, false);
        signRoot.transform.position = center;
        signRoot.transform.rotation = rotation;
        signRoot.transform.localScale = Vector3.one;

        // TMP's readable face points along local -Z, not +Z.
        CreateTextMesh(signRoot.transform, "Front", text, new Vector3(0, 0, halfThickness + 0.02f), Quaternion.Euler(0, 180, 0), size, font, fontSize);
        CreateTextMesh(signRoot.transform, "Back", text, new Vector3(0, 0, -(halfThickness + 0.02f)), Quaternion.identity, size, font, fontSize);
    }

    void CreateTextMesh(Transform parent, string faceName, string text, Vector3 localPos, Quaternion localRot, Vector2 size, TMP_FontAsset font, float fontSize)
    {
        GameObject go = new GameObject(faceName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.isRightToLeftText = false;
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.rectTransform.sizeDelta = size;
        tmp.sortingOrder = 5;
    }
}