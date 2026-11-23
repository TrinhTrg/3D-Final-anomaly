using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HospitalReceptionFix
{
    private const float InteractionRange = 7f;

    [MenuItem("Tools/Hospital/Sửa tương tác quầy tiếp nhận")]
    private static void Apply()
    {
        var scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            UnityEngine.Debug.LogWarning("Hãy thoát Play Mode trước khi sửa scene.");
            return;
        }

        if (scene.name != "Hospital_day1" && scene.name != "Hospital_day2")
        {
            UnityEngine.Debug.LogWarning("Hãy mở scene Hospital_day1 hoặc Hospital_day2.");
            return;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            var sequence = root.GetComponentInChildren<AAH_CheckInSequence>(true);
            if (sequence == null) continue;

            var checkIn = new SerializedObject(sequence);
            string[] fields =
            {
                "stepClipboard", "stepCameraTripod", "stepComputer", "stepPrinter"
            };

            var steps = new SerializedObject[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                var target = checkIn.FindProperty(fields[i])?.objectReferenceValue;
                if (target == null)
                {
                    UnityEngine.Debug.LogWarning("Thiếu tham chiếu " + fields[i], sequence);
                    return;
                }

                steps[i] = new SerializedObject(target);
                if (steps[i].FindProperty("interactionDistance") == null)
                {
                    UnityEngine.Debug.LogWarning("Không tìm thấy interactionDistance.", target);
                    return;
                }
            }

            foreach (var step in steps)
            {
                step.FindProperty("interactionDistance").floatValue = InteractionRange;
                step.ApplyModifiedProperties();
            }

            bool ringFixed = PositionPrinterRing(steps[3]);
            EditorSceneManager.MarkSceneDirty(scene);

            if (ringFixed)
                UnityEngine.Debug.Log($"Đã chỉnh tầm E và vòng máy in trong {scene.name}. Nhấn Ctrl+S để lưu.");
            else
                UnityEngine.Debug.LogWarning("Đã chỉnh tầm E cho cả bốn vật. Hãy kiểm tra hai ô " +
                    "Target Collider và Highlight Ring trên bước máy in.", steps[3].targetObject);
            return;
        }

        UnityEngine.Debug.LogWarning("Không tìm thấy AAH_CheckInSequence trong scene đang mở.");
    }

    private static bool PositionPrinterRing(SerializedObject printer)
    {
        var collider = printer.FindProperty("targetCollider")?.objectReferenceValue as Collider;
        var ring = printer.FindProperty("highlightRing")?.objectReferenceValue as GameObject;
        var line = ring != null ? ring.GetComponent<LineRenderer>() : null;
        if (collider == null || line == null ||
            !collider.enabled || !collider.gameObject.activeInHierarchy)
            return false;

        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        center.y = bounds.max.y + 0.04f;

        float radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude + 0.10f;
        const int pointCount = 64;

        Undo.RecordObject(line, "Chỉnh vòng xanh máy in");
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * 2f * Mathf.PI / pointCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            line.SetPosition(i, line.transform.InverseTransformPoint(center + offset));
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(line);
        EditorUtility.SetDirty(line);
        return true;
    }
}
