using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates gameplay events and applies the sanity rules. Each taser hit id is
/// applied at most once. When networking is added, call these methods on the
/// authoritative instance only.
/// </summary>
public class AAH_SanityEventRules : MonoBehaviour
{
    public static AAH_SanityEventRules Instance { get; private set; }

    [Header("Taser")]
    [SerializeField] private int _normalPatientPenalty = 15;
    [SerializeField] private int _teammatePenalty = 2;
    [SerializeField] private int _anomalyReward = 2;

    private readonly HashSet<int> _registeredHits = new HashSet<int>();
    private readonly HashSet<object> _activeAnomalyThreats = new HashSet<object>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public enum TaserResult { Ignored, NormalPatient, Teammate, Anomaly }

    /// <summary>Register one taser hit. Returns Ignored for duplicates or unknown targets.</summary>
    public TaserResult RegisterTaserHit(int hitId, AAH_PlayerSanity shooter, GameObject target)
    {
        if (shooter == null || target == null || !_registeredHits.Add(hitId)) return TaserResult.Ignored;

        var teammate = target.GetComponentInParent<AAH_PlayerSanity>();
        if (teammate != null)
        {
            if (teammate == shooter) return TaserResult.Ignored;
            shooter.ApplyChange(-_teammatePenalty);
            teammate.ApplyChange(-_teammatePenalty);
            return TaserResult.Teammate;
        }

        bool? anomaly = IsAnomaly(target);
        if (anomaly == null) return TaserResult.Ignored;
        if (anomaly.Value) { shooter.ApplyChange(_anomalyReward); return TaserResult.Anomaly; }
        shooter.ApplyChange(-_normalPatientPenalty);
        return TaserResult.NormalPatient;
    }

    private static bool? IsAnomaly(GameObject target)
    {
        var animal = target.GetComponentInParent<AAH_AnimalPatient>();
        if (animal != null) return animal.isAnomaly;
        var inspection = target.GetComponentInParent<AAH_PatientInspection>();
        if (inspection != null) return inspection.isAnomaly;
        if (target.GetComponentInParent<AAH_PatientActor>() != null) return false;
        return null;
    }

    /// <summary>An anomaly got checked in: every player drains until the threat is resolved.</summary>
    public void ReportAnomalyCheckedIn(object threat)
    {
        if (threat == null || !_activeAnomalyThreats.Add(threat)) return;
        foreach (var player in AAH_PlayerSanity.All) player.StartAnomalyDrain(threat);
    }
    public void ReportAnomalyResolved(object threat)
    {
        if (threat == null || !_activeAnomalyThreats.Remove(threat)) return;
        foreach (var player in AAH_PlayerSanity.All) player.StopAnomalyDrain(threat);
    }

    public void BeginRedEyeEncounter(AAH_PlayerSanity player, object encounter) { if (player != null) player.BeginRedEyeEncounter(encounter); }
    public void EndRedEyeEncounter(AAH_PlayerSanity player, object encounter) { if (player != null) player.EndRedEyeEncounter(encounter); }
}
