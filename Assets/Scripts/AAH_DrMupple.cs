using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
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
    public float moveSpeed = 9f; // Fallback when the patient queue is absent.

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
    private float _walkingSpeed;
    private Vector3 _groundPosition;
    private Vector3 _pivotAboveGround;
    private Quaternion _uprightRotation;
    private Vector3 _authoredForward;
    private Vector3 _heading;

    // The current Mupple mesh faces screen-right when the navigation heading faces the desk.
    private const float MuppleFacingOffset = 90f;

    [Tooltip("Extra yaw in degrees if the imported mesh faces sideways in its standing pose.")]
    [SerializeField] private float _modelYawCorrection;

    private bool PrepareStandingPose()
    {
        var model = muppleModel.transform;
        _uprightRotation = model.rotation;

        _authoredForward = Vector3.ProjectOnPlane(model.forward, Vector3.up);
        if (_authoredForward.sqrMagnitude < 0.001f)
            _authoredForward = Vector3.Cross(model.right, Vector3.up);

        foreach (var animator in model.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid) continue;
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (left == null || right == null) continue;
            Vector3 forward = Vector3.Cross(right.position - left.position, Vector3.up);
            if (forward.sqrMagnitude > 0.001f) _authoredForward = forward;
        }
        _authoredForward.Normalize();
        _heading = _authoredForward;

        Bounds bounds = default;
        bool foundMesh = false;
        foreach (var renderer in _renderers)
        {
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.gameObject.activeInHierarchy) continue;
            if (foundMesh) bounds.Encapsulate(renderer.bounds);
            else { bounds = renderer.bounds; foundMesh = true; }
        }
        if (!foundMesh) return false;
        _groundPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        _pivotAboveGround = model.position - _groundPosition;
        return true;
    }

    private void PlaceAt(Vector3 ground, Vector3 facing)
    {
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.000001f) _heading = facing.normalized;
        float yaw = Vector3.SignedAngle(_authoredForward, _heading, Vector3.up) + MuppleFacingOffset + _modelYawCorrection;
        var turn = Quaternion.AngleAxis(yaw, Vector3.up);
        _groundPosition = ground;
        muppleModel.transform.SetPositionAndRotation(ground + turn * _pivotAboveGround, turn * _uprightRotation);
    }

    private bool TryRoute(Transform target, float startRadius, out NavMeshPath path)
    {
        path = new NavMeshPath();
        var filter = new NavMeshQueryFilter { agentTypeID = _storyAgentTypeId, areaMask = NavMesh.AllAreas };
        if (!NavMesh.SamplePosition(_groundPosition, out var start, startRadius, filter) ||
            !NavMesh.SamplePosition(target.position, out var end, 2f, filter) ||
            Vector3.ProjectOnPlane(end.position - target.position, Vector3.up).sqrMagnitude > 1f ||
            !NavMesh.CalculatePath(start.position, end.position, filter, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError("Mupple cannot reach " + target.name + ". Check the marker and baked NavMesh.", this);
            return false;
        }
        PlaceAt(start.position, _heading);
        return true;
    }

    private IEnumerator WalkRoute(NavMeshPath path)
    {
        foreach (var corner in path.corners)
        {
            while ((_groundPosition - corner).sqrMagnitude > 0.000001f)
            {
                if (_storyDoors != null)
                    foreach (var door in _storyDoors)
                        if (door != null && Vector3.Distance(_groundPosition, door.ApproachPosition) < 9f)
                            door.RequestOpen();
                var direction = corner - _groundPosition;
                var next = Vector3.MoveTowards(_groundPosition, corner, _walkingSpeed * Time.deltaTime);
                PlaceAt(next, direction);
                yield return null;
            }
        }
    }

    private IEnumerator StoreRoomExit()
    {
        if (muppleModel == null || _storeRoomDestination == null)
        { Debug.LogError("Mupple store-room destination is not assigned.", this); yield break; }
        if (!TryRoute(_storeRoomDestination, 2f, out var path)) yield break;
        yield return WalkRoute(path);
        HasEnteredStoreRoom = true;
        muppleModel.SetActive(false);
    }

    void Start()
    {
        if (muppleModel != null)
        {
            var body = muppleModel.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;
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
        return PlayStoryEntrance(dialogue, data, data != null ? data.Mupple : null, () => Day1DialogueCompleted?.Invoke());
    }
    public IEnumerator PlayDay2Entrance(Transform player, AAH_CheckInSequence dialogue, AAH_Day1Dialogue data, System.Action done)
    {
        var lines = new[] {
            new AAH_Day1Dialogue.Line("day2.mupple.log", "Đưa tôi xem sổ trực một chút. Ừ… đủ lượt rồi."),
            new AAH_Day1Dialogue.Line("day2.mupple.voice", "Lúc nãy tôi nghe tiếng cậu gọi ngoài cổng. Nhưng cậu vẫn đứng ở quầy, đúng chứ?"),
            new AAH_Day1Dialogue.Line("day2.mupple.end", "Tối nay cứ để cửa quầy đóng. Cậu bàn giao ca được rồi.")
        };
        return PlayStoryEntrance(dialogue, data, lines, done);
    }
    private IEnumerator PlayStoryEntrance(AAH_CheckInSequence dialogue, AAH_Day1Dialogue data, AAH_Day1Dialogue.Line[] lines, System.Action done)
    {
        if (activated) yield break;
        if (muppleModel == null || dialogue == null || data == null)
        { Debug.LogError("Day 1 Mupple references missing.", this); yield break; }

        var arrivals = FindFirstObjectByType<AAH_PatientArrivalController>();
        Transform receptionStand = arrivals != null && arrivals.StandPoint != null
            ? arrivals.StandPoint : dialogue.patientStandPoint;
        if (receptionStand == null)
        { Debug.LogError("Mupple needs the patient stand point at Reception.", this); yield break; }
        _walkingSpeed = Mathf.Max(0.1f, arrivals != null ? arrivals.WalkingSpeed : moveSpeed);
        activated = true;
        yield return new WaitForSeconds(walkInDelay);
        muppleModel.SetActive(true);
        SetModelVisible(true);
        if (!PrepareStandingPose())
        { Debug.LogError("Mupple model has no visible mesh.", this); yield break; }
        if (!TryRoute(receptionStand, 12f, out var path)) yield break;
        yield return WalkRoute(path);
        PlaceAt(_groundPosition, dialogue.DeskForward(receptionStand));
        yield return dialogue.ShowStoryDialogue("Bác sĩ Mupple", data, lines);
        done?.Invoke();
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
