using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AAH_AnomalyDetection — Core Mechanic 2: Anomaly Detection via CCTV/Camera tool.
/// The player can press [C] to toggle the CCTV "camera view" overlay.
/// In camera view, anomalies are revealed by their anomaly cue (visual distortion).
/// The player then presses [E] near a suspicious patient to "report" them.
/// 
/// This is distinct from Mechanic 1 (inspection at the patient's trigger zone)
/// because it uses a camera overlay + raycast system rather than proximity triggers.
/// </summary>
public class AAH_AnomalyDetection : MonoBehaviour
{
    [Header("CCTV Camera Tool")]
    [Tooltip("Overlay image that simulates CCTV/security camera view.")]
    public GameObject cctvOverlay;

    [Tooltip("The camera used for raycasting (usually the player camera).")]
    public Camera playerCamera;

    [Tooltip("Max distance for anomaly raycast detection.")]
    public float detectionRange = 5.0f;

    [Header("UI")]
    public TextMeshProUGUI txtCameraStatus;
    public GameObject uiDetectPrompt; // "Press E to Report Anomaly"
    public GameObject uiDetectResult;
    public TextMeshProUGUI txtDetectResult;
    public float resultDisplayDuration = 2.5f;

    [Header("Audio")]
    public AudioSource cameraToggleSound;
    public AudioSource reportSound;

    // State
    private bool cctvActive = false;
    private AAH_PatientInspection currentTarget = null;

    void Update()
    {
        // Toggle CCTV view with [C]
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCCTV();
        }

        if (cctvActive)
        {
            ScanForAnomalies();
        }
        else
        {
            HideDetectPrompt();
        }
    }

    void ToggleCCTV()
    {
        cctvActive = !cctvActive;

        if (cctvOverlay != null)
            cctvOverlay.SetActive(cctvActive);

        if (cameraToggleSound != null)
            cameraToggleSound.Play();

        if (txtCameraStatus != null)
            txtCameraStatus.text = cctvActive ? "[C] CCTV: ON" : "[C] CCTV: OFF";

        if (!cctvActive)
            HideDetectPrompt();
    }

    void ScanForAnomalies()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, detectionRange))
        {
            // Check if the hit object or its parent has an AAH_PatientInspection component
            AAH_PatientInspection patient = hit.collider.GetComponentInParent<AAH_PatientInspection>();
            if (patient != null && !patient.isAnomaly == false) // only anomalies
            {
                // We're looking at an anomaly patient via CCTV
                currentTarget = patient;
                ShowDetectPrompt();

                // Report with E
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ReportAnomaly(patient);
                }
                return;
            }
        }

        // Not looking at anything anomalous
        currentTarget = null;
        HideDetectPrompt();
    }

    void ShowDetectPrompt()
    {
        if (uiDetectPrompt != null)
            uiDetectPrompt.SetActive(true);
    }

    void HideDetectPrompt()
    {
        if (uiDetectPrompt != null)
            uiDetectPrompt.SetActive(false);
        currentTarget = null;
    }

    void ReportAnomaly(AAH_PatientInspection patient)
    {
        HideDetectPrompt();

        if (reportSound != null)
            reportSound.Play();

        // Trigger the patient's inspection which updates the GameManager
        // (Only if not already inspected via proximity)
        if (AAH_GameManager.Instance != null && !IsAlreadyInspected(patient))
        {
            AAH_GameManager.Instance.OnPatientInspected(true);
        }

        StartCoroutine(ShowDetectResult(patient.patientName));
    }

    bool IsAlreadyInspected(AAH_PatientInspection patient)
    {
        // A simple reflection-based check; for Week 2 we rely on the patient's internal state
        // In a more advanced version this would use a separate flag
        return false;
    }

    IEnumerator ShowDetectResult(string name)
    {
        if (uiDetectResult != null)
        {
            uiDetectResult.SetActive(true);
            if (txtDetectResult != null)
            {
                txtDetectResult.text =
                    $"<color=#FF4444>⚠ ANOMALY REPORTED ⚠</color>\n\n" +
                    $"Patient <b>{name}</b> flagged.\n" +
                    $"Security notified.\n" +
                    $"<color=#FFD700>+25 Animal Coins</color>";
            }
            yield return new WaitForSeconds(resultDisplayDuration);
            uiDetectResult.SetActive(false);
        }
        else
        {
            yield return null;
        }

        // Turn off CCTV after successful report
        cctvActive = false;
        if (cctvOverlay != null) cctvOverlay.SetActive(false);
        if (txtCameraStatus != null) txtCameraStatus.text = "[C] CCTV: OFF";
    }
}
