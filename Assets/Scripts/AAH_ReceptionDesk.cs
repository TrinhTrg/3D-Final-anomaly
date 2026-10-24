using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AAH_ReceptionDesk — The reception desk where patients check in.
/// This drives the patient queue and spawning for the current shift.
/// Week 2 prototype: 3 patients arrive sequentially, player must handle each.
/// </summary>
public class AAH_ReceptionDesk : MonoBehaviour
{
    [Header("Patient Queue")]
    [Tooltip("Patient GameObjects placed in the scene. Order determines arrival sequence.")]
    public GameObject[] patientObjects;

    [Tooltip("Time delay (seconds) between patient arrivals.")]
    public float timeBetweenArrivals = 10.0f;

    [Header("Reception Bell / Interaction")]
    [Tooltip("Bell trigger — player presses E here to call next patient.")]
    public bool useManualBell = false;

    [Header("UI")]
    public GameObject uiBellPrompt;      // "Press E: Call Next Patient"
    public TextMeshProUGUI txtQueueStatus;

    [Header("Dialogue / Popup")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public float dialogueDuration = 4.0f;

    // State
    private int currentPatientIndex = 0;
    private bool playerAtDesk = false;
    private bool waitingForPlayer = false;

    // Pre-defined patient arrival dialogue lines
    private string[] arrivalLines = new string[]
    {
        "Patient at reception: \"Good day, doctor. I'm here for my checkup...\"",
        "Patient at reception: \"Hello... I have an appointment. I hope this is quick.\"",
        "Patient at reception: \"Excuse me... I was told to come here today. I'm not feeling well.\""
    };

    void Start()
    {
        // Hide all patients at start
        foreach (var p in patientObjects)
            if (p != null) p.SetActive(false);

        if (uiBellPrompt != null)
            uiBellPrompt.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (!useManualBell)
        {
            // Auto-arrive patients on a timer
            StartCoroutine(AutoArrivePatients());
        }
    }

    IEnumerator AutoArrivePatients()
    {
        yield return new WaitForSeconds(3.0f); // Brief initial delay

        while (currentPatientIndex < patientObjects.Length)
        {
            if (AAH_GameManager.Instance != null && !AAH_GameManager.Instance.shiftActive)
                yield break;

            ActivateNextPatient();
            yield return new WaitForSeconds(timeBetweenArrivals);
        }
    }

    void ActivateNextPatient()
    {
        if (currentPatientIndex >= patientObjects.Length) return;

        GameObject patient = patientObjects[currentPatientIndex];
        if (patient != null)
        {
            patient.SetActive(true);
            ShowArrivalDialogue(currentPatientIndex);
        }
        currentPatientIndex++;

        UpdateQueueUI();
    }

    void ShowArrivalDialogue(int index)
    {
        if (dialoguePanel == null || dialogueText == null) return;
        string line = (index < arrivalLines.Length) ? arrivalLines[index] : "Patient has arrived.";
        StartCoroutine(ShowDialogueCoroutine(line));
    }

    IEnumerator ShowDialogueCoroutine(string text)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = text;
        yield return new WaitForSeconds(dialogueDuration);
        dialoguePanel.SetActive(false);
    }

    void UpdateQueueUI()
    {
        if (txtQueueStatus != null)
        {
            int remaining = patientObjects.Length - (AAH_GameManager.Instance != null
                ? AAH_GameManager.Instance.patientsInspected : 0);
            txtQueueStatus.text = $"Patients waiting: {Mathf.Max(0, remaining)}";
        }
    }

    // Manual bell interaction (optional)
    void OnTriggerStay(Collider other)
    {
        if (!useManualBell) return;
        if (!other.CompareTag("MainCamera")) return;

        playerAtDesk = true;
        if (uiBellPrompt != null)
            uiBellPrompt.SetActive(currentPatientIndex < patientObjects.Length);

        if (Input.GetKeyDown(KeyCode.E) && currentPatientIndex < patientObjects.Length)
        {
            ActivateNextPatient();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("MainCamera")) return;
        playerAtDesk = false;
        if (uiBellPrompt != null)
            uiBellPrompt.SetActive(false);
    }
}
