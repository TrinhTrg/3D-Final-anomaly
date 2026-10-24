using UnityEngine;

/// <summary>
/// AAH_AnimalPatient — Visual component for an animal patient in the hospital.
/// Handles the "twitching" anomaly visual cue and idle animation.
/// Attach to the patient root GameObject.
/// The actual inspection logic is on the child trigger GameObject (AAH_PatientInspection).
/// </summary>
public class AAH_AnimalPatient : MonoBehaviour
{
    [Header("Identity")]
    public string patientName = "Patient";
    public bool isAnomaly = false;

    [Header("Anomaly Visual Cues")]
    [Tooltip("If anomaly, this object is the 'third eye' or other visual cue. Hidden until CCTV or inspection.")]
    public GameObject anomalyCueRenderer;

    [Tooltip("If true, the patient will twitch periodically when anomaly is active.")]
    public bool enableTwitching = true;
    public float twitchInterval = 3.0f;
    public float twitchAmount = 5.0f;

    // Reference to inspection component (child)
    private AAH_PatientInspection inspectionComponent;
    private float twitchTimer;
    private Quaternion originalRotation;

    void Awake()
    {
        // Find inspection component on self or children
        inspectionComponent = GetComponentInChildren<AAH_PatientInspection>();
        if (inspectionComponent != null)
        {
            inspectionComponent.isAnomaly = isAnomaly;
            inspectionComponent.patientName = patientName;
            inspectionComponent.anomalyCueObject = anomalyCueRenderer;
        }

        originalRotation = transform.localRotation;

        // Hide anomaly cue initially
        if (anomalyCueRenderer != null)
            anomalyCueRenderer.SetActive(false);
    }

    void Update()
    {
        if (!isAnomaly || !enableTwitching) return;

        twitchTimer += Time.deltaTime;
        if (twitchTimer >= twitchInterval)
        {
            twitchTimer = 0f;
            StartCoroutine(TwitchCoroutine());
        }
    }

    System.Collections.IEnumerator TwitchCoroutine()
    {
        // Quick rotation snap
        transform.localRotation = Quaternion.Euler(
            originalRotation.eulerAngles.x,
            originalRotation.eulerAngles.y + Random.Range(-twitchAmount, twitchAmount),
            originalRotation.eulerAngles.z
        );
        yield return new WaitForSeconds(0.1f);
        transform.localRotation = originalRotation;
        yield return new WaitForSeconds(0.05f);
        transform.localRotation = Quaternion.Euler(
            originalRotation.eulerAngles.x,
            originalRotation.eulerAngles.y + Random.Range(-twitchAmount, twitchAmount),
            originalRotation.eulerAngles.z
        );
        yield return new WaitForSeconds(0.1f);
        transform.localRotation = originalRotation;
    }
}
