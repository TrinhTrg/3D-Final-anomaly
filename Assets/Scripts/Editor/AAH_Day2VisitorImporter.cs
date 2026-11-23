using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class AAH_Day2VisitorImporter : AssetPostprocessor
{
    private const string Folder = "Assets/Models/Animals/Patient";
    private const string Moratter = "Assets/Models/Animals/Anomaly/Moratter.glb";
    private const string Catalog = "Assets/Resources/VetNightmare/Day2Visitors.asset";

    private static bool IsModelPath(string path) => path.Equals(Moratter, StringComparison.OrdinalIgnoreCase)
        || (path.StartsWith(Folder + "/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase));
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsModelPath))
        {
            EditorApplication.delayCall -= Sync;
            EditorApplication.delayCall += Sync;
        }
    }
    [MenuItem("Tools/Vet Nightmare/Sync Day 2 visitors")]
    private static void Sync()
    {
        var roster = AssetDatabase.LoadAssetAtPath<AAH_Day2Visitors>(Catalog);
        if (roster == null) { Debug.LogError("Thiếu " + Catalog); return; }
        var paths = AssetDatabase.FindAssets("", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)).Distinct().OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        var imported = paths.Select(path => new { model = AssetDatabase.LoadAssetAtPath<GameObject>(path),
            name = System.IO.Path.GetFileNameWithoutExtension(path) }).Where(entry => entry.model != null).ToArray();
        var models = imported.Select(entry => entry.model).ToArray();
        var names = imported.Select(entry => entry.name).ToArray();
        var anomaly = AssetDatabase.LoadAssetAtPath<GameObject>(Moratter);
        if (roster.moratter != anomaly || roster.normalModels == null || !roster.normalModels.SequenceEqual(models) ||
            roster.normalModelNames == null || !roster.normalModelNames.SequenceEqual(names))
        {
            roster.moratter = anomaly;
            roster.normalModels = models;
            roster.normalModelNames = names;
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();
        }
        if (anomaly == null || models.Length < 2) Debug.LogError("Kiểm tra Moratter.glb và ít nhất hai model trong " + Folder);
        else Debug.Log("Day 2: Moratter + " + models.Length + " model bệnh nhân có thể bốc thăm.");
    }
}
