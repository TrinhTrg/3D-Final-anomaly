using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Restore only the missing script reference. The door mesh, hinge, player and
// opening angle stay serialized on the original scene component.
public static class AAH_RepairHospitalDoors
{
    private const string OriginalGuid = "6b1a12b6bb56916498ff4d1a928c9565";
    private const string UploadedGuid = "2a4fb0fa5c897154fae4320111b1bf49";
    private const string Menu = "Tools/Vet Nightmare/Repair Hospital Doors";

    [MenuItem(Menu, true)]
    private static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(Menu)]
    private static void Repair()
    {
        string guid = null;
        foreach (string id in AssetDatabase.FindAssets("AAH_AutoDoor t:MonoScript"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(id));
            if (script != null && script.GetClass() == typeof(AAH_AutoDoor)) { guid = id; break; }
        }
        if (string.IsNullOrEmpty(guid))
        {
            EditorUtility.DisplayDialog("Cửa Reception", "Chưa tìm thấy script AAH_AutoDoor đã biên dịch. Kiểm tra Console.", "OK");
            return;
        }
        var report = new StringBuilder();
        foreach (string scene in new[] { "Hospital_day1", "Hospital_day2" })
        {
            string path = "Assets/Scenes/" + scene + ".unity";
            report.Append(scene).Append(" — ");
            if (!File.Exists(path)) { report.AppendLine("Thiếu file scene."); continue; }
            if (SceneManager.GetSceneByPath(path).isLoaded)
            {
                report.AppendLine("Đang mở. Hãy mở riêng scene menu rồi chạy lại.");
                continue;
            }
            try { report.AppendLine(RepairScene(path, guid)); }
            catch (Exception e) { report.AppendLine(e.Message); }
        }
        EditorUtility.DisplayDialog("Cửa Reception", report.ToString(), "OK");
    }

    private static string Reference(string guid) => "m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}";

    private static string RepairScene(string path, string guid)
    {
        string source = File.ReadAllText(path);
        if (!source.StartsWith("%YAML", StringComparison.Ordinal))
            return "Scene không ở dạng YAML. Chưa sửa.";
        string target = Reference(guid);
        string fixedText = source.Replace(Reference(OriginalGuid), target).Replace(Reference(UploadedGuid), target);
        if (source == fixedText)
            return source.Contains(target) ? "Đã liên kết đúng." : "Không tìm thấy component cửa Reception trong scene.";
        string temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, fixedText, new UTF8Encoding(false));
            FileUtil.ReplaceFile(temp, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return "Đã nối lại script cửa Reception.";
    }
}
