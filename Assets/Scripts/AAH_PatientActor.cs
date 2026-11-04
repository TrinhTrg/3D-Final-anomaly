using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class AAH_PatientActor : MonoBehaviour
{
    public enum PatientState { Approaching, WaitingAtDesk, Dialogue, CheckingIn, Leaving, AtBed, Blocked }
    [Serializable]
    public class PatientProfile
    {
        public GameObject model;
        public string displayName = "Mai Yamada";
        [Tooltip("Yaw correction for imported model facing direction")]
        public float facingCorrection = 180f;
        [TextArea] public string dialogue = "I'm here for my appointment...";
    }
    [SerializeField, Min(0.1f)] private float _height = 5.4f;
    [SerializeField, Min(1f)] private float _travelTimeout = 90f;
    [SerializeField, Min(1f)] private float _doorDistance = 9f;
    private UnityEngine.AI.NavMeshAgent _agent;
    private Transform _visual;
    private Quaternion _uprightModelRotation;
    private Renderer[] _renderers;
    private AAH_PatientDoorAccess[] _doors;
    private AAH_RoomRegistry _registry;
    private bool _moveFailed;
    public string PatientId { get; private set; }
    public string DisplayName { get; private set; }
    public string Dialogue { get; private set; }
    public PatientState State { get; private set; }
    public int RoomNumber { get; private set; }
    public bool HasVacatedDesk { get; private set; }
    public Bounds VisualBounds { get { var b = _renderers[0].bounds; foreach (var r in _renderers) b.Encapsulate(r.bounds); return b; } }
    public event Action<AAH_PatientActor> ArrivedAtDesk;
    public event Action<AAH_PatientActor> NavigationFailed;
    private void Awake() { _agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); }
    public void Initialize(PatientProfile profile, string id, AAH_RoomRegistry registry, AAH_PatientDoorAccess[] doors)
    {
        PatientId = id; DisplayName = profile.displayName; Dialogue = profile.dialogue; _registry = registry; _doors = doors;
        _visual = Instantiate(profile.model, transform).transform;
        _visual.localRotation = Quaternion.Euler(0f, profile.facingCorrection, 0f) * _visual.localRotation;
        _uprightModelRotation = _visual.localRotation;
        _visual.localPosition = Vector3.zero; _renderers = _visual.GetComponentsInChildren<Renderer>(true);
        if (_renderers.Length == 0) throw new InvalidOperationException("Patient model has no renderers.");
        // Imported GLB objects already carry their source-axis conversion. Normalize size, not their rotation.
        var b = VisualBounds; _visual.localScale *= _height / Mathf.Max(0.1f, b.size.y);
        b = VisualBounds; _visual.position += transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var c in _visual.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var a in _visual.GetComponentsInChildren<Animator>(true))
        {
            a.applyRootMotion = false;
            a.enabled = false;
        }
    }
    public void FacePhotoCamera(Vector3 cameraPosition)
    {
        if (_visual == null || State == PatientState.AtBed || State == PatientState.Leaving) return;
        Vector3 direction = cameraPosition - VisualBounds.center;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        _agent.updateRotation = false;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        _visual.localRotation = _uprightModelRotation;
    }
    public void Approach(Transform entrance, Transform stand)
    { StartCoroutine(ApproachRoutine(entrance, stand)); }
    private IEnumerator ApproachRoutine(Transform entrance, Transform stand)
    {
        State = PatientState.Approaching;
        yield return MoveTo(entrance.position);
        if (_moveFailed) yield break;
        yield return MoveTo(stand.position);
        if (_moveFailed) yield break;
        transform.rotation = stand.rotation;
        State = PatientState.WaitingAtDesk; ArrivedAtDesk?.Invoke(this);
    }
    public void SetDeskState(PatientState state) { if (State != PatientState.Leaving && State != PatientState.AtBed) State = state; }
    private void Update()
    {
        if (_doors == null || State == PatientState.AtBed) return;
        foreach (var door in _doors) if (door != null && Vector3.Distance(transform.position, door.transform.position) < _doorDistance) door.RequestOpen();
    }
    private IEnumerator MoveTo(Vector3 position)
    {
        _moveFailed = false;
        UnityEngine.AI.NavMeshHit hit;
        if (!_agent.isOnNavMesh || !UnityEngine.AI.NavMesh.SamplePosition(position, out hit, 2f, _agent.areaMask))
        { Fail("Destination is off NavMesh: " + position); yield break; }
        _agent.isStopped = false;
        if (!_agent.SetDestination(hit.position)) { Fail("SetDestination rejected"); yield break; }
        float elapsed = 0f;
        // Give pathfinding at least one simulation frame before checking remainingDistance.
        yield return null;
        while (elapsed < _travelTimeout)
        {
            if (!_agent.pathPending)
            {
                if (_agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete) { Fail("Path is not complete to " + position); yield break; }
                if (_agent.remainingDistance <= _agent.stoppingDistance + 0.25f) { _agent.isStopped = true; yield break; }
            }
            elapsed += Time.deltaTime; yield return null;
        }
        Fail("Travel timed out to " + position);
    }
    private void Fail(string message)
    {
        _moveFailed = true; State = PatientState.Blocked;
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        Debug.LogError("Patient " + PatientId + " room " + RoomNumber + ": " + message, this);
        NavigationFailed?.Invoke(this);
    }
    public void LeaveForRoom(AAH_RoomRegistry.RoomSlot room)
    { RoomNumber = room.roomNumber; StartCoroutine(LeaveRoutine(room)); }
    private IEnumerator LeaveRoutine(AAH_RoomRegistry.RoomSlot room)
    {
        State = PatientState.Leaving;
        _agent.updateRotation = true;
        Vector3 deskPosition = transform.position;
        room.door.RequestOpen();
        yield return MoveTo(room.entry.position);
        HasVacatedDesk = Vector3.Distance(transform.position, deskPosition) > 4f;
        if (_moveFailed) yield break;
        while (!room.door.IsClear) yield return null;
        yield return MoveTo(room.bedApproach.position);
        if (_moveFailed) yield break;
        _agent.enabled = false;
        // bedPose.forward points toward the pillow. Canonical model face is +Z.
        // -90 degrees maps the face to +Y and the head to -Z (supine).
        Vector3 headDirection = Vector3.ProjectOnPlane(room.bedPose.forward, Vector3.up).normalized;
        _visual.rotation = Quaternion.LookRotation(-headDirection, Vector3.up)
            * Quaternion.Euler(-90f, 0f, 0f) * _uprightModelRotation;
        var b = VisualBounds;
        float bedLength = Mathf.Abs(headDirection.x) * room.bedRenderer.bounds.size.x
            + Mathf.Abs(headDirection.z) * room.bedRenderer.bounds.size.z;
        float patientLength = Mathf.Abs(headDirection.x) * b.size.x + Mathf.Abs(headDirection.z) * b.size.z;
        Vector3 centre = room.bedPose.position + headDirection * Mathf.Max(0f, (bedLength - patientLength) * 0.5f - 0.5f);
        _visual.position += new Vector3(centre.x - b.center.x, room.bedPose.position.y - b.min.y, centre.z - b.center.z);
        State = PatientState.AtBed; _registry.Occupy(PatientId, room.roomNumber);
    }
    public void LeaveHospital(Transform exit)
    {
        if (State != PatientState.AtBed || exit == null) return;
        StartCoroutine(DischargeRoutine(exit));
    }
    private IEnumerator DischargeRoutine(Transform exit)
    {
        State = PatientState.Leaving;
        _visual.localRotation = _uprightModelRotation;
        _visual.localPosition = Vector3.zero;
        var bounds = VisualBounds;
        _visual.position += transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        _agent.enabled = true;
        _agent.updateRotation = true;
        yield return MoveTo(exit.position);
        if (_moveFailed) yield break;
        if (_registry != null) _registry.ReleaseRoom(PatientId);
        gameObject.SetActive(false);
    }
    public bool IsAwayFrom(Vector3 point) => Vector3.Distance(transform.position, point) > 4f;
    private void OnDisable() { StopAllCoroutines(); }
}
