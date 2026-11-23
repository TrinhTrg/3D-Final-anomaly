using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class AAH_TrashCan : MonoBehaviour
{
    // Also permits a designer to add this component to a differently named bin.
    internal static void Bind(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (!IsBinName(node.name) && node.GetComponent<AAH_TrashCan>() == null) continue;
            if (!node.TryGetComponent<AAH_TrashCan>(out _)) node.gameObject.AddComponent<AAH_TrashCan>();
            // Imported GLBs can store geometry far from the node pivot. Size the
            // hitbox from the actual mesh in its own local coordinate system.
            foreach (var filter in node.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                if (filter.TryGetComponent<BoxCollider>(out var existing) && existing.enabled) continue;
                var box = filter.gameObject.AddComponent<BoxCollider>();
                box.center = filter.sharedMesh.bounds.center;
                box.size = filter.sharedMesh.bounds.size;
                box.isTrigger = true;
            }
        }
    }

    private static bool IsBinName(string name)
    {
        string compact = name.Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
        const string prefix = "trashcan";
        if (!compact.StartsWith(prefix, System.StringComparison.Ordinal)) return false;
        string suffix = compact.Substring(prefix.Length);
        return int.TryParse(suffix, out int index) && index >= 1 && index <= 5;
    }
}
