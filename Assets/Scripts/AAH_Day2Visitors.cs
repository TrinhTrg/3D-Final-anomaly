using System.Collections.Generic;
using UnityEngine;

public sealed class AAH_Day2Visitors : ScriptableObject
{
    public GameObject moratter;
    public GameObject[] normalModels;
    public string[] normalModelNames; // Model filenames in the same order as normalModels.
    public bool TryCreateQueue(AAH_PatientActor.PatientProfile polly, out AAH_PatientActor.PatientProfile[] queue)
    {
        queue = null;
        if (moratter == null || polly == null || polly.model == null || normalModels == null) return false;
        var pool = new List<GameObject>();
        foreach (var model in normalModels)
            if (model != null && model != moratter && model != polly.model && !pool.Contains(model)) pool.Add(model);
        if (pool.Count < 2) return false;
        var first = Draw(pool);
        var second = Draw(pool);
        var moratterProfile = Profile(moratter, "Moratter", "Tôi có hẹn. Nhịp tim lần này… đúng với tờ giấy rồi chứ?", true);
        moratterProfile.facingCorrection = 0f;
        queue = new[] {
            moratterProfile,
            NormalProfile(first, "Chào bác sĩ. Cho tôi đăng ký khám với."),
            NormalProfile(second, "Tới lượt tôi rồi phải không? Tôi có mang theo giấy hẹn."),
            new AAH_PatientActor.PatientProfile { model = polly.model, displayName = polly.displayName,
                facingCorrection = polly.facingCorrection, dialogueKey = "day2.polly.arrival",
                dialogue = "Bác sĩ còn nhớ tôi không? Tối qua cơn đau đầu lại quay về." }
        };
        return true;
    }
    private AAH_PatientActor.PatientProfile NormalProfile(GameObject model, string line)
    {
        int index = System.Array.IndexOf(normalModels, model);
        string name = normalModelNames != null && index >= 0 && index < normalModelNames.Length &&
            !string.IsNullOrEmpty(normalModelNames[index]) ? normalModelNames[index] : model.name;
        if (string.Equals(name.Replace("_", ""), "fishbaby", System.StringComparison.OrdinalIgnoreCase))
            line = "Gâu... Gâu gâu.. gâu!";
        var profile = Profile(model, name.Replace('_', ' '), line);
        // Correct each imported facing axis so the navigation root can use +Z.
        switch (name.ToLowerInvariant())
        {
            case "chuckle":
            case "duckman":
            case "hokaido":
            case "rattina":
            case "skinny":
                profile.facingCorrection = 90f;
                break;
            case "hazmat":
                profile.facingCorrection = -90f;
                break;
            case "dog_baby":
            case "fish_baby":
            case "grandba_hokaido":
            case "ratthew":
            case "ratthew_model_animal_hospitalrigged":
            case "ron":
                profile.facingCorrection = 0f;
                break;
            // Looby faces -Z, matching the PatientProfile default of 180 degrees.
        }
        return profile;
    }
    private static GameObject Draw(List<GameObject> pool)
    {
        int i = Random.Range(0, pool.Count);
        GameObject model = pool[i];
        pool.RemoveAt(i);
        return model;
    }
    private static AAH_PatientActor.PatientProfile Profile(GameObject model, string name, string line, bool anomaly = false)
    {
        return new AAH_PatientActor.PatientProfile { model = model, displayName = name, dialogue = line,
            isAnomaly = anomaly, dialogueKey = anomaly ? "day2.moratter.arrival" : "day2.patient.arrival" };
    }
}
