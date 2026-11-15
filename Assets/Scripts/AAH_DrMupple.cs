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
    private Renderer[] _renderers;
    private Collider[] _colliders;
    [Header("Story navigation")]
    [SerializeField] private int _storyAgentTypeId;
    [SerializeField] private AAH_PatientDoorAccess[] _storyDoors;
    [SerializeField] private Transform _storeRoomDestination;
    private bool _storeExitStarted;
    public bool StoreExitStarted => _storeExitStarted;
    public bool HasEnteredStoreRoom { get; private set; }
    public void TriggerStoreRoomExit()
    {
        if (_storeExitStarted || HasEnteredStoreRoom) return;
        _storeExitStarted = true;
        StartCoroutine(StoreRoomExit());
    }
    private IEnumerator StoreRoomExit()
    {
        if (muppleModel == null || _storeRoomDestination == null)
        { Debug.LogError("Mupple store-room destination is not assigned.", this); yield break; }
        var filter = new UnityEngine.AI.NavMeshQueryFilter { agentTypeID = _storyAgentTypeId, areaMask = UnityEngine.AI.NavMesh.AllAreas };
        UnityEngine.AI.NavMeshHit start, destination;
        if (!UnityEngine.AI.NavMesh.SamplePosition(muppleModel.transform.position, out start, 2f, filter)
            || !UnityEngine.AI.NavMesh.SamplePosition(_storeRoomDestination.position, out destination, 1f, filter))
        { Debug.LogError("Mupple store-room route is off the hospital NavMesh.", this); yield break; }
        var path = new UnityEngine.AI.NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(start.position, destination.position, filter, path)
            || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        { Debug.LogError("Mupple has no complete path into store_room.", this); yield break; }
        // Continue from the current NPC pose: do not snap or alter imported child transforms.
        foreach (var corner in path.corners)
        {
            while (Vector3.Distance(muppleModel.transform.position, corner) > 0.1f)
            {
                if (_storyDoors != null) foreach (var door in _storyDoors)
                    if (door != null && Vector3.Distance(muppleModel.transform.position, door.ApproachPosition) < 9f) door.RequestOpen();
                Vector3 direction = corner - muppleModel.transform.position; direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f) muppleModel.transform.rotation = Quaternion.LookRotation(direction);
                muppleModel.transform.position = Vector3.MoveTowards(muppleModel.transform.position, corner, Mathf.Max(0.1f, moveSpeed) * Time.deltaTime);
                yield return null;
            }
        }
        HasEnteredStoreRoom = true;
        muppleModel.SetActive(false);
    }

    void Start()
    {
        if (muppleModel != null)
        {
            _renderers = muppleModel.GetComponentsInChildren<Renderer>(true);
            _colliders = muppleModel.GetComponentsInChildren<Collider>(true);
            if (muppleModel == gameObject) SetModelVisible(false);
            else muppleModel.SetActive(false);
        }
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    private void SetModelVisible(bool visible)
    {
        if (_renderers != null) foreach (var renderer in _renderers) renderer.enabled = visible;
        if (_colliders != null) foreach (var collider in _colliders) collider.enabled = visible;
    }
    public IEnumerator PlayDay1Entrance(Transform player, AAH_CheckInSequence dialogue, AAH_Day1Dialogue data)
    {
        if (activated) yield break;
        activated = true;
        yield return new WaitForSeconds(walkInDelay);
        if (muppleModel == null || player == null) { Debug.LogError("Day 1 Muple references missing.", this); yield break; }
        var filter = new UnityEngine.AI.NavMeshQueryFilter { agentTypeID = _storyAgentTypeId, areaMask = UnityEngine.AI.NavMesh.AllAreas };
        UnityEngine.AI.NavMeshHit start, destination;
        Vector3 target = player.position + player.forward * 5f;
        if (!UnityEngine.AI.NavMesh.SamplePosition(muppleModel.transform.position, out start, 12f, filter)
            || !UnityEngine.AI.NavMesh.SamplePosition(target, out destination, 6f, filter))
        { Debug.LogError("Day 1 Muple entrance is off NavMesh.", this); yield break; }
        var path = new UnityEngine.AI.NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(start.position, destination.position, filter, path)
            || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        { Debug.LogError("Day 1 Muple cannot reach player.", this); yield break; }
        muppleModel.SetActive(true); SetModelVisible(true);
        muppleModel.transform.position = start.position;
        foreach (var corner in path.corners)
        {
            while (Vector3.Distance(muppleModel.transform.position, corner) > 0.1f)
            {
                if (_storyDoors != null) foreach (var door in _storyDoors)
                    if (door != null && Vector3.Distance(muppleModel.transform.position, door.ApproachPosition) < 9f) door.RequestOpen();
                muppleModel.transform.position = Vector3.MoveTowards(muppleModel.transform.position, corner, Mathf.Max(0.1f, moveSpeed) * Time.deltaTime);
                yield return null;
            }
        }
        Vector3 facing = player.position - muppleModel.transform.position; facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f) muppleModel.transform.rotation = Quaternion.LookRotation(facing);
        yield return dialogue.ShowStoryDialogue("Bác sĩ Muple", data, data.Mupple);
        Day1DialogueCompleted?.Invoke();
    }
    public event System.Action Day1DialogueCompleted;

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
            SetModelVisible(true);

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
