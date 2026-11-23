using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AAH_GameManager — Central game state controller for Anomaly Animal Hospital.
/// Manages shift state, patient/anomaly counters, and win/loss conditions.
/// Week 2 prototype: Day 1 logic only.
/// </summary>
public class AAH_GameManager : MonoBehaviour
{
    public static AAH_GameManager Instance { get; private set; }

    [Header("Shift Settings")]
    public int currentDay = 1;
    public int patientsToInspect = 3;
    public int anomaliesPresent = 0; // Day 1 has 0 anomalies

    [Header("Runtime Counters")]
    public int patientsInspected = 0;
    public int anomaliesDetected = 0;
    public int normalPatientsCleared = 0;

    [Header("UI References")]
    public TextMeshProUGUI txtShiftLabel;
    public TextMeshProUGUI txtObjective;
    public TextMeshProUGUI txtPatientCount;
    public TextMeshProUGUI txtAnomalyCount;
    public GameObject panelShiftEnd;
    public TextMeshProUGUI txtShiftResult;
    [SerializeField] private TMPro.TMP_Text _earnedCoinsText;
    [Header("Client list screen")]
    [SerializeField] private TMPro.TMP_Text _clientListText;

    [Header("State")]
    public bool shiftActive = true;
    public bool shiftEnded = false;
    [Header("Day 1 story completion")]
    [SerializeField] private bool _requireDay1Story;
    private string _day1Objective = "Ngày 1: Tiếp nhận, khám và điều trị cho Polly.";
    [Header("Shift report gameplay sources")]
    [SerializeField] private AAH_CheckInSequence _reportCheckIn;
    [SerializeField] private AAH_RoomTreatmentSequence[] _reportTreatmentRooms;
    private readonly HashSet<string> _treatedPatientIds = new HashSet<string>();
    private readonly HashSet<string> _acceptedPatientIds = new HashSet<string>();
    private readonly HashSet<string> _treatedAnomalyIds = new HashSet<string>();
    private bool _boardChecked;
    private bool _numberOnlyBoard;
    public int PatientsTreated => _treatedPatientIds.Count;
    public int PatientsAccepted => _acceptedPatientIds.Count;
    private readonly HashSet<string> _visitorIds = new HashSet<string>();
    private readonly HashSet<string> _rejectedAnomalyIds = new HashSet<string>();
    private readonly HashSet<string> _deadPatientIds = new HashSet<string>();
    [Header("Existing reward rates")]
    [SerializeField, Min(0)] private int _coinsPerTreatment = 10;
    [SerializeField, Min(0)] private int _coinsPerBlockedAnomaly = 15;
    [SerializeField, Min(0)] private int _coinsPerCompletedShift = 20;
    [SerializeField, Min(0)] private int _coinsForZeroDeaths = 10;
    // A fresh Day 1 displays the rewards from this playthrough. The wallet
    // separately keeps the permanent balance and prevents duplicate payouts.
    private readonly HashSet<string> _day1Outcomes = new HashSet<string>();
    public int Day1Coins { get; private set; }
    public event System.Action<int> Day1CoinsChanged;
    private IAAH_CoinWallet _coinWallet;
    private string _coinShiftId;
    public int TotalAnimalCoins => _coinWallet != null ? _coinWallet.Balance : 0;
    public int TotalVisitors => _visitorIds.Count;
    public int BlockedAnomalies => _rejectedAnomalyIds.Count;
    public int PatientDeaths => Mathf.Max(_deadPatientIds.Count, _coinWallet != null ? _coinWallet.GetShiftDeaths(_coinShiftId) : 0);
    public int EarnedAnimalCoins => currentDay == 1 ? Day1Coins :
        (_coinWallet != null ? _coinWallet.GetShiftEarnings(_coinShiftId) : 0);
    private void AwardCoins(string outcomeId, int amount)
    {
        if (_coinWallet != null && !string.IsNullOrEmpty(_coinShiftId))
            _coinWallet.TryAward(_coinShiftId + ":" + outcomeId, _coinShiftId, amount);

        if (currentDay == 1 && amount > 0 && _day1Outcomes.Add(outcomeId))
        {
            Day1Coins += amount;
            Day1CoinsChanged?.Invoke(Day1Coins);
        }
    }
    private void AwardShiftCompletion()
    {
        AwardCoins("shift-completed", _coinsPerCompletedShift);
        if (PatientDeaths == 0) AwardCoins("zero-deaths", _coinsForZeroDeaths);
    }
    public string PerformanceRating
    {
        get
        {
            if (TotalVisitors == 0) return "CHƯA CÓ DỮ LIỆU";
            float successRate = (PatientsTreated + BlockedAnomalies) / (float)TotalVisitors;
            if (PatientDeaths == 0 && successRate >= 0.9f) return "XUẤT SẮC";
            if (successRate >= 0.7f) return "TỐT";
            if (successRate >= 0.5f) return "TRUNG BÌNH";
            return "CẦN CẢI THIỆN";
        }
    }
    // Outcome hooks: call only when the corresponding gameplay outcome actually occurs.
    private readonly HashSet<string> _refusedNormals = new HashSet<string>();
    private readonly HashSet<string> _deadNormals = new HashSet<string>();
    public void BeginDay2(int visitors)
    {
        patientsToInspect = visitors;
        anomaliesPresent = 1;
        SetDay1Objective("Ngày 2: Tiếp nhận bệnh nhân. Dùng cửa sập khi phát hiện khách bất thường.");
    }
    public bool HasDay2Outcome(string id) => _treatedPatientIds.Contains(id) ||
        _rejectedAnomalyIds.Contains(id) || _refusedNormals.Contains(id) || _deadPatientIds.Contains(id);
    public void RecordDeskRefusal(AAH_PatientActor visitor)
    {
        if (currentDay != 2 || shiftEnded || visitor == null || HasDay2Outcome(visitor.PatientId)) return;
        if (visitor.IsAnomaly) RecordAnomalyRejected(visitor.PatientId);
        else
        {
            _refusedNormals.Add(visitor.PatientId);
            RecordVisitor(visitor.PatientId);
            if (_soloSanity != null) _soloSanity.ApplyChange(-10);
        }
        UpdateUI();
    }
    public void CompleteDay2Story()
    {
        if (currentDay != 2 || shiftEnded) return;
        shiftEnded = true;
        shiftActive = false;
        SetDay1Objective("Ngày 2 hoàn tất.");
        ShowShiftEndPanel();
    }
    public void RecordVisitor(string visitorId)
    {
        if (!shiftEnded && !string.IsNullOrWhiteSpace(visitorId)) _visitorIds.Add(visitorId);
    }
    public void RecordAnomalyRejected(string visitorId)
    {
        if (shiftEnded || string.IsNullOrWhiteSpace(visitorId)) return;
        _visitorIds.Add(visitorId);
        if (!_rejectedAnomalyIds.Add(visitorId)) return;
        AwardCoins("anomaly-rejected:" + visitorId, _coinsPerBlockedAnomaly);
    }
    public void RecordPatientDeath(string patientId)
    {
        if (shiftEnded || string.IsNullOrWhiteSpace(patientId)) return;
        _visitorIds.Add(patientId);
        if (!_deadPatientIds.Add(patientId)) return;
        foreach (var victim in FindObjectsByType<AAH_PatientActor>(FindObjectsSortMode.None))
        {
            if (victim.gameObject.scene != gameObject.scene || victim.PatientId != patientId) continue;
            if (!victim.IsAnomaly && !_refusedNormals.Contains(victim.PatientId)) _deadNormals.Add(victim.PatientId);
            victim.FallToFloor();
            if (_reportCheckIn != null) _reportCheckIn.RemoveDeceased(victim);
            foreach (var room in FindObjectsByType<AAH_RoomTreatmentSequence>(FindObjectsSortMode.None))
                if (room.gameObject.scene == gameObject.scene) room.RemoveDeceased(victim);
            break;
        }
        _treatedPatientIds.Remove(patientId);
        _treatedAnomalyIds.Remove(patientId);
        UpdateClientList();
        if (_coinWallet != null) _coinWallet.RecordShiftDeath(_coinShiftId, patientId);
    }
    public bool HasPendingAttack()
    {
        foreach (var actor in FindObjectsByType<AAH_PatientActor>(FindObjectsSortMode.None))
            if (actor.gameObject.scene == gameObject.scene && actor.ThreatPending) return true;
        return false;
    }

    internal MonoBehaviour NearestHuntTarget(AAH_PatientActor attacker)
    {
        MonoBehaviour nearest = null;
        float best = float.PositiveInfinity;
        Vector3 origin = attacker.VisualBounds.center;
        void Consider(MonoBehaviour target)
        {
            if (!attacker.IsLivingTarget(target)) return;
            float distance = (AAH_PatientActor.TargetBounds(target).center - origin).sqrMagnitude;
            if (distance >= best) return;
            best = distance;
            nearest = target;
        }

        foreach (var patient in FindObjectsByType<AAH_PatientActor>(FindObjectsSortMode.None))
            Consider(patient);
        foreach (var player in AAH_PlayerSanity.All) Consider(player);
        return nearest;
    }

    internal bool TryResolveAttack(AAH_PatientActor attacker, MonoBehaviour target)
    {
        if (shiftEnded || !shiftActive || !attacker.CanStrike(target)) return false;
        if (target is AAH_PlayerSanity player)
        {
            player.ApplyChange(-player.Max);
            EndSoloGame();
        }
        else if (target is AAH_PatientActor victim)
        {
            RecordPatientDeath(victim.PatientId);
        }
        return true;
    }

    private void EndSoloGame()
    {
        if (shiftEnded) return;
        shiftEnded = true;
        shiftActive = false;
        gameObject.AddComponent<AAH_SoloGameOver>();
    }

    private AAH_PlayerSanity _soloSanity;
    private void OnEnable()
    {
        _soloSanity = FindFirstObjectByType<AAH_PlayerSanity>();
        if (_soloSanity != null) _soloSanity.Depleted += EndSoloGame;
        if (_reportCheckIn != null) _reportCheckIn.OnPatientCheckedIn += CountCheckIn;
        if (_reportTreatmentRooms != null) foreach (var room in _reportTreatmentRooms)
            if (room != null) room.PatientRecovered += CountTreatment;
    }
    private void OnDisable()
    {
        if (_soloSanity != null) _soloSanity.Depleted -= EndSoloGame;
        if (_reportCheckIn != null) _reportCheckIn.OnPatientCheckedIn -= CountCheckIn;
        if (_reportTreatmentRooms != null) foreach (var room in _reportTreatmentRooms)
            if (room != null) room.PatientRecovered -= CountTreatment;
    }
    private void CountCheckIn(string patientName, int roomNumber)
    {
        if (!shiftEnded && _reportCheckIn != null && _reportCheckIn.CurrentPatient != null)
        {
            string id = _reportCheckIn.CurrentPatient.PatientId;
            _acceptedPatientIds.Add(id);
            RecordVisitor(id);
        }
    }
    private void CountTreatment(AAH_PatientActor patient)
    {
        if (shiftEnded || patient == null || _deadPatientIds.Contains(patient.PatientId)) return;
        if (_treatedPatientIds.Add(patient.PatientId))
        {
            if (patient.IsAnomaly) _treatedAnomalyIds.Add(patient.PatientId);
            UpdateClientList();
        }
        RecordVisitor(patient.PatientId);
        AwardCoins("patient-treated:" + patient.PatientId, _coinsPerTreatment);
    }
    public void SetDay1Objective(string text)
    {
        _day1Objective = text;
        UpdateUI();
    }
    public void CompleteDay1Story()
    {
        if (!_requireDay1Story || currentDay != 1 || shiftEnded) return;
        shiftEnded = true; shiftActive = false;
        _day1Objective = "Ngày 1 hoàn tất.";
        UpdateUI();
        ShowShiftEndPanel();
        if (panelShiftEnd != null)
        {
            var report = panelShiftEnd.GetComponent<AAH_ShiftEndUI>();
            if (report == null) report = panelShiftEnd.AddComponent<AAH_ShiftEndUI>();
            report.PrepareDay1Report();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (gameObject.scene.name == AAH_ShiftEndUI.Day2Scene) currentDay = 2;
        try
        {
            _coinWallet = AAH_CoinWallet.Current;
            _coinShiftId = AAH_CoinWallet.BeginHospitalShift(currentDay, gameObject.scene.name);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Animal Coins could not initialize: " + e.Message, this);
        }
    }

    void Start()
    {
        var player = FindFirstObjectByType<SC_FPSController>();
        if (player != null && player.GetComponent<AAH_HospitalHands>() == null)
            player.gameObject.AddComponent<AAH_HospitalHands>();
        AAH_InteractionHints.CenterScenePrompts(gameObject.scene);
        UpdateUI();
        if (panelShiftEnd != null)
            panelShiftEnd.SetActive(false);
    }

    /// <summary>Call when a patient is inspected (normal or anomaly).</summary>
    public void OnPatientInspected(bool isAnomaly)
    {
        patientsInspected++;
        if (isAnomaly)
            anomaliesDetected++;
        else
            normalPatientsCleared++;

        UpdateUI();
        CheckShiftComplete();
    }

    void CheckShiftComplete()
    {
        if (currentDay == 2 || (_requireDay1Story && currentDay == 1)) return;
        if (patientsInspected >= patientsToInspect && !shiftEnded)
        {
            shiftEnded = true;
            shiftActive = false;
            StartCoroutine(ShowShiftEndDelay(2.0f));
        }
    }

    IEnumerator ShowShiftEndDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowShiftEndPanel();
    }

    void ShowShiftEndPanel()
    {
        if (shiftEnded) AwardShiftCompletion();
        if (panelShiftEnd != null)
        {
            panelShiftEnd.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Find player and disable movement
            SC_FPSController player = FindFirstObjectByType<SC_FPSController>();
            if (player != null) player.canMove = false;
        }

        if (txtShiftResult != null)
        {
            txtShiftResult.text = "<size=125%><b>BÁO CÁO CA TRỰC</b></size>\nNgày " + currentDay + " - Ca trực " + currentDay
                + "\n\n--------------------------\n\nBệnh nhân điều trị: <b>" + PatientsTreated
                + "</b>\nTổng lượt khách: <b>" + TotalVisitors
                + "</b>\nAnomaly từ chối: <b>" + BlockedAnomalies
                + "</b>\nSố ca tử vong: <b>" + PatientDeaths
                + "</b>\n\n--------------------------\n\nHiệu suất bệnh viện\n<color=#27874D><b>" + PerformanceRating
                + "</b></color>\n\n<size=80%>Điều trị +10 | Từ chối +15\nHoàn tất ca +20 | Không tử vong +10</size>";
            string earnedText = "<color=#996600><b>+" + EarnedAnimalCoins + " Animal Coins</b></color>";
            if (_earnedCoinsText != null) _earnedCoinsText.text = earnedText;
            else txtShiftResult.text += "\n\n" + earnedText;
        }
    }

    private static bool HasBoardHeadings(TMP_Text label)
    {
        string value = label.text ?? string.Empty;
        return value.IndexOf("CHECK IN PATIENTS", System.StringComparison.OrdinalIgnoreCase) >= 0
            && value.IndexOf("NORMAL ANIMALS", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ResolveClientListText()
    {
        if (_clientListText != null && _clientListText.gameObject.scene == gameObject.scene
            && _clientListText.isActiveAndEnabled
            && (HasBoardHeadings(_clientListText) || int.TryParse(_clientListText.text, out _))) return;

        TMP_Text match = null;
        foreach (var label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (label.gameObject.scene != gameObject.scene || !label.isActiveAndEnabled || !HasBoardHeadings(label)) continue;
            if (match != null)
            {
                Debug.LogWarning("Có nhiều bảng NORMAL ANIMALS. Hãy gán đúng Client List Text trên GameManager.", this);
                return;
            }
            match = label;
        }
        if (match != null) _clientListText = match;
        else if (_clientListText == null)
            Debug.LogWarning("Chưa tìm thấy bảng NORMAL ANIMALS. Hãy gán Client List Text trên GameManager.", this);
    }

    private void UpdateClientList()
    {
        if (!_boardChecked)
        {
            _boardChecked = true;
            ResolveClientListText();
            _numberOnlyBoard = _clientListText != null && int.TryParse(_clientListText.text, out _);
        }
        if (_clientListText == null) return;
        int treatedNormals = PatientsTreated - _treatedAnomalyIds.Count;
        int remaining = Mathf.Max(0, patientsToInspect - anomaliesPresent - treatedNormals - _refusedNormals.Count - _deadNormals.Count);
        if (_numberOnlyBoard)
        {
            _clientListText.SetText("{0:0}", remaining);
            return;
        }
        _clientListText.SetText("CHECK IN PATIENTS\n\nSHIFT TODAY - DAY {0:0}\nNORMAL ANIMALS\n<size=160%>{1:0}</size>", currentDay, remaining);
    }

    void UpdateUI()
    {
        UpdateClientList();
        if (txtShiftLabel != null)
            txtShiftLabel.text = $"SHIFT {currentDay}";

        if (txtObjective != null)
        {
            if (currentDay == 2 || (_requireDay1Story && currentDay == 1))
                txtObjective.text = _day1Objective;
            else if (!shiftEnded)
                txtObjective.text = $"Inspect patients at Reception\nInspected: {patientsInspected}/{patientsToInspect}";
            else
                txtObjective.text = "Shift complete! Dr. Mupple is evaluating...";
        }

        if (txtPatientCount != null)
            txtPatientCount.text = $"Patients: {patientsInspected}/{patientsToInspect}";

        if (txtAnomalyCount != null)
            txtAnomalyCount.text = $"Anomalies: {anomaliesDetected}";
    }
}
