using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AAH_DrMupple — Dr. Mupple NPC controller.
/// Appears at the end of the shift to give the performance report.
/// Week 2: Simple walk-in → dialogue → reward sequence.
/// </summary>
public class AAH_DrMupple : MonoBehaviour
{
    [Header("NPC")]
    public GameObject muppleModel;
    public float walkInDelay = 1.5f;
    public Vector3 standPosition;      // Where he walks to (world position)
    public float moveSpeed = 2.0f;

    [Header("Dialogue Panel")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public float lineDisplayTime = 4.0f;

    [Header("Audio")]
    public AudioSource muppleVoice;

    // State
    private bool activated = false;

    void Start()
    {
        if (muppleModel != null)
            muppleModel.SetActive(false);
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    /// <summary>Call this when the shift ends to trigger Dr. Mupple's entrance.</summary>
    public void TriggerShiftEnd()
    {
        if (activated) return;
        activated = true;
        StartCoroutine(MuppleEntranceSequence());
    }

    IEnumerator MuppleEntranceSequence()
    {
        yield return new WaitForSeconds(walkInDelay);

        if (muppleModel != null)
        {
            muppleModel.SetActive(true);

            // Walk to stand position
            float elapsed = 0f;
            Vector3 start = muppleModel.transform.position;
            while (elapsed < 2.0f)
            {
                elapsed += Time.deltaTime;
                muppleModel.transform.position = Vector3.Lerp(start, standPosition, elapsed / 2.0f);
                yield return null;
            }
            muppleModel.transform.position = standPosition;
        }

        yield return new WaitForSeconds(0.5f);

        // Dialogue sequence
        int inspected = AAH_GameManager.Instance != null ? AAH_GameManager.Instance.patientsInspected : 0;
        int anomalies = AAH_GameManager.Instance != null ? AAH_GameManager.Instance.anomaliesDetected : 0;
        int coins = inspected * 10 + anomalies * 25;

        string[] lines = new string[]
        {
            "<i>Dr. Mupple enters with a clipboard...</i>",
            $"\"Ahem. Good evening. Let me review today's shift report.\"",
            $"\"You handled <color=#FFD700>{inspected}</color> patient(s) and flagged <color=#FF4444>{anomalies}</color> anomaly case(s).\"",
            $"\"For your performance, you'll receive <color=#FFD700>{coins} Animal Coins</color>.\"",
            anomalies > 0
                ? "\"Good work spotting those irregularities. Stay sharp, doctor.\""
                : "\"All patients seemed normal today. Let's hope it stays that way.\""
        };

        foreach (string line in lines)
        {
            yield return StartCoroutine(ShowDialogueLine(line));
        }

        // Notify GameManager to show shift end panel
        if (AAH_GameManager.Instance != null)
        {
            // Panel is already shown by GameManager; just make sure
        }
    }

    IEnumerator ShowDialogueLine(string text)
    {
        if (dialoguePanel == null || dialogueText == null)
        {
            yield return new WaitForSeconds(lineDisplayTime);
            yield break;
        }

        dialoguePanel.SetActive(true);
        dialogueText.text = text;

        if (muppleVoice != null && !muppleVoice.isPlaying)
            muppleVoice.Play();

        yield return new WaitForSeconds(lineDisplayTime);
        dialoguePanel.SetActive(false);
        yield return new WaitForSeconds(0.3f);
    }
}
