using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AAH_DoorScriptLinkRepair
{
    private const string OldGuid = "6b1a12b6bb56916498ff4d1a928c9565";
    private const string UploadedGuid = "2a4fb0fa5c897154fae4320111b1bf49";

    [MenuItem("Tools/Vet Nightmare/Restore Door Interaction")]
    private static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string scriptGuid = FindScriptGuid();
        if (scriptGuid == null)
        {
            Debug.LogError("Không tìm thấy script đang biên dịch cho AAH_AutoDoor.");
            return;
        }

        var paths = new List<string>();
        foreach (string name in new[] { "Hospital_day1", "Hospital_day2" })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (!File.Exists(path))
            {
                Debug.LogError("Không tìm thấy scene " + path);
                return;
            }
            if (SceneManager.GetSceneByPath(path).isLoaded)
            {
                EditorUtility.DisplayDialog("Restore Door Interaction",
                    "Lưu scene bệnh viện, mở riêng scene menu rồi chạy lại công cụ.", "OK");
                return;
            }
            paths.Add(path);
        }

        foreach (string path in paths)
        {
            string message = RepairScene(path, scriptGuid);
            Debug.Log(path + " — " + message);
        }
    }

    private static string FindScriptGuid()
    {
        foreach (string guid in AssetDatabase.FindAssets("AAH_AutoDoor t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == typeof(AAH_AutoDoor)) return guid;
        }
        return null;
    }

    // Edit only the script reference of the component with the Reception fields.
    // Preserve its local fileID, prefab overrides, line endings and all other data.
    private static string RepairScene(string path, string activeGuid)
    {
        byte[] original = File.ReadAllBytes(path);
        string scene = Encoding.UTF8.GetString(original);
        var documents = Regex.Matches(scene,
            @"(?ms)^--- !u!114 &[^\n]*\nMonoBehaviour:\r?$.*?(?=^--- !u!|\z)");
        var link = new Regex(@"(?m)^  m_Script: \{fileID: 11500000, guid: (?<guid>[a-fA-F0-9]{32}), type: 3\}\r?$");
        Group selected = null;
        int offset = 0;
        foreach (Match doc in documents)
        {
            if (!doc.Value.Contains("\n  _interactionPoint:") || !doc.Value.Contains("\n  _door:") ||
                !doc.Value.Contains("\n  _hinge:") || !doc.Value.Contains("\n  _openAngle:")) continue;
            var match = link.Match(doc.Value);
            if (!match.Success) continue;
            if (selected != null) return "Có nhiều component phù hợp. Scene được giữ nguyên.";
            selected = match.Groups["guid"];
            offset = doc.Index + selected.Index;
        }
        if (selected == null) return "Không tìm thấy dữ liệu cửa Reception trong scene dạng text.";
        if (selected.Value == activeGuid) return "Liên kết cửa đã đúng.";
        if (selected.Value != OldGuid && selected.Value != UploadedGuid)
            return "GUID cửa chưa được nhận diện. Scene được giữ nguyên.";
        string updated = scene.Remove(offset, selected.Length).Insert(offset, activeGuid);
        // Replace atomically on the same volume. Unity keeps the scene's original .meta.
        string temp = path + "." + Guid.NewGuid().ToString("N") + "~";
        try
        {
            File.WriteAllBytes(temp, Encoding.UTF8.GetBytes(updated));
            File.Replace(temp, path, null);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
        AssetDatabase.ImportAsset(path);
        return "Đã nối lại script cửa Reception.";
    }
}
