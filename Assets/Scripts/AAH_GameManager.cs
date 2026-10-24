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

    [Header("State")]
    public bool shiftActive = true;
    public bool shiftEnded = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
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
            int score = normalPatientsCleared * 10 + anomaliesDetected * 25;
            txtShiftResult.text =
                $"<b>SHIFT {currentDay} COMPLETE</b>\n\n" +
                $"Patients Inspected: {patientsInspected}\n" +
                $"Anomalies Detected: {anomaliesDetected}\n" +
                $"Normal Cleared: {normalPatientsCleared}\n\n" +
                $"<color=#FFD700>Score: {score} Animal Coins</color>";
        }
    }

    void UpdateUI()
    {
        if (txtShiftLabel != null)
            txtShiftLabel.text = $"SHIFT {currentDay}";

        if (txtObjective != null)
        {
            if (!shiftEnded)
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
