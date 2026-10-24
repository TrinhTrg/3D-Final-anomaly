using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AAH_PatientInspection — Handles the "Press E to Inspect" interaction for animal patients.
/// Attach this to the patient's trigger collider GameObject.
/// Reuses the same OnTriggerStay pattern as the existing KeyScript and Door scripts.
/// 
/// Mechanic 1: Patient Inspection — player approaches patient, presses E, 
/// gets NORMAL or ANOMALY result, and this updates the GameManager counter.
/// </summary>
public class AAH_PatientInspection : MonoBehaviour
{
    [Header("Patient Settings")]
    [Tooltip("Is this patient an anomaly? Set true to make it an anomaly patient.")]
    public bool isAnomaly = false;

    [Tooltip("Unique patient name shown in inspection result.")]
    public string patientName = "Patient";

    [Header("Anomaly Visual Cues (only used if isAnomaly = true)")]
    [Tooltip("Optional: extra renderer component that shows the 'third eye' or other anomaly cue.")]
    public GameObject anomalyCueObject;

    [Header("UI References")]
    [Tooltip("The interaction prompt UI text (e.g. 'Press E to Inspect').")]
    public GameObject uiPromptObject;

    [Header("Result Panel")]
    [Tooltip("Panel shown after inspection with result text.")]
    public GameObject uiResultPanel;
    public TextMeshProUGUI uiResultText;
    public float resultDisplayDuration = 3.0f;

    [Header("Audio")]
    public AudioSource inspectSound;

    // Internal state
    private bool alreadyInspected = false;
    private bool playerInRange = false;

    void Start()
    {
        // Anomaly visual cues are hidden initially
        if (anomalyCueObject != null)
            anomalyCueObject.SetActive(false);

        if (uiPromptObject != null)
            uiPromptObject.SetActive(false);

        if (uiResultPanel != null)
            uiResultPanel.SetActive(false);
    }

    void OnTriggerStay(Collider other)
    {
        if (alreadyInspected) return;
        if (!other.CompareTag("MainCamera")) return;

        playerInRange = true;
        if (uiPromptObject != null)
            uiPromptObject.SetActive(true);

        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformInspection();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("MainCamera")) return;
        playerInRange = false;

        if (!alreadyInspected && uiPromptObject != null)
            uiPromptObject.SetActive(false);
    }

    void PerformInspection()
    {
        if (alreadyInspected) return;
        alreadyInspected = true;

        if (uiPromptObject != null)
            uiPromptObject.SetActive(false);

        // Play sound
        if (inspectSound != null)
            inspectSound.Play();

        // Notify GameManager
        if (AAH_GameManager.Instance != null)
            AAH_GameManager.Instance.OnPatientInspected(isAnomaly);

        // Show inspection result
        StartCoroutine(ShowResult());
    }

    IEnumerator ShowResult()
    {
        if (uiResultPanel != null)
        {
            uiResultPanel.SetActive(true);

            if (uiResultText != null)
            {
                if (isAnomaly)
                {
                    uiResultText.text =
                        $"<b>INSPECTION REPORT — {patientName.ToUpper()}</b>\n\n" +
                        "<color=#FF4444>⚠ ANOMALY DETECTED ⚠</color>\n\n" +
                        "Abnormal readings found.\n" +
                        "Extra eyes detected in scan.\n" +
                        "Biological signature: <color=#FF4444>NON-STANDARD</color>";

                    // Reveal the anomaly cue visually
                    if (anomalyCueObject != null)
                        anomalyCueObject.SetActive(true);
                }
                else
                {
                    uiResultText.text =
                        $"<b>INSPECTION REPORT — {patientName.ToUpper()}</b>\n\n" +
                        "<color=#44FF88>✓ NORMAL</color>\n\n" +
                        "Vital signs stable.\n" +
                        "No anomalies detected.\n" +
                        "Biological signature: <color=#44FF88>STANDARD</color>";
                }
            }

            yield return new WaitForSeconds(resultDisplayDuration);
            uiResultPanel.SetActive(false);
        }
        else
        {
            yield return null;
        }
    }

    // Allow external reset (for future multi-patient waves)
    public void ResetPatient()
    {
        alreadyInspected = false;
        playerInRange = false;
        if (uiPromptObject != null) uiPromptObject.SetActive(false);
        if (uiResultPanel != null) uiResultPanel.SetActive(false);
        if (anomalyCueObject != null) anomalyCueObject.SetActive(false);
    }
}
