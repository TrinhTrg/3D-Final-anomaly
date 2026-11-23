using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class AAH_PatientActor : MonoBehaviour
{
    public enum PatientState { Approaching, WaitingAtDesk, Dialogue, CheckingIn, Leaving, AtBed, Blocked, Dead, Hunting, ReturningToBed }
    public enum IllnessType
    {
        Unassigned, Dehydration, Bruises, Fever, Stomachache, CommonCold,
        DryEyes, SkinRash, Bleeding, Headache, Hypoglycemia, EyePain
    }
    private static readonly IllnessType[] RandomIllnesses =
    {
        IllnessType.Dehydration, IllnessType.Bruises, IllnessType.Fever,
        IllnessType.Stomachache, IllnessType.CommonCold, IllnessType.DryEyes,
        IllnessType.SkinRash, IllnessType.Bleeding, IllnessType.Headache,
        IllnessType.Hypoglycemia
    };
    [Header("Patient diagnosis")]
    [SerializeField] private IllnessType _illness = IllnessType.Unassigned;
    public IllnessType Illness => _illness;

    public void AssignIllness(int day, int patientNumber)
    {
        if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
        if (patientNumber < 1) throw new ArgumentOutOfRangeException(nameof(patientNumber));
        // Assignment belongs to this patient and never changes with its room or state.
        if (_illness != IllnessType.Unassigned) return;
        if (day == 2 && DisplayName == "Polly") { _illness = IllnessType.Headache; return; }
        if (day == 1 && patientNumber <= 3)
        {
            _illness = patientNumber == 1 ? IllnessType.Stomachache
                : patientNumber == 2 ? IllnessType.EyePain : IllnessType.Headache;
            return;
        }
        _illness = RandomIllnesses[UnityEngine.Random.Range(0, RandomIllnesses.Length)];
    }

    [Serializable]
    public class PatientProfile
    {
        public GameObject model;
        public string displayName = "Mai Yamada";
        [Tooltip("Anomalous visitors must be refused at reception; normal Day 1 profiles remain false.")]
        public bool isAnomaly;
        [Tooltip("Yaw correction for imported model facing direction")]
        public float facingCorrection = 180f;
        [Tooltip("Stable string-table key for future localization; dialogue is the Vietnamese fallback.")]
        public string dialogueKey = "patients.check_in.default";
        [TextArea] public string dialogue = "Xin chào, tôi đến khám theo lịch hẹn.";
    }
    [SerializeField, Min(0.1f)] private float _height = 5.4f;
    [SerializeField, Min(1f)] private float _travelTimeout = 90f;
    [SerializeField, Min(1f)] private float _doorDistance = 9f;
    private UnityEngine.AI.NavMeshAgent _agent;
    private Transform _visual;
    private Vector3 _uprightModelPosition;
    private AAH_RoomRegistry.RoomSlot _homeRoom;
    private Transform _queuedDischarge;
    [SerializeField, Min(0.1f)] private float _meleeReach = 2.4f;
    private const float HuntRefresh = 0.25f;
    private const float StrikeWindup = 0.35f;
    private Renderer[] _doorPanels;
    private Quaternion _uprightModelRotation;
    private Renderer[] _renderers;
    private AAH_PatientDoorAccess[] _doors;
    private AAH_RoomRegistry _registry;
    private bool _moveFailed;
    public string PatientId { get; private set; }
    public string DisplayName { get; private set; }
    public string Dialogue { get; private set; }
    public string DialogueKey { get; private set; }
    public bool IsAnomaly { get; private set; }
    public bool IsRejected { get; private set; }
    public PatientState State { get; private set; }
    public int RoomNumber { get; private set; }
    public bool InsideHospital { get; private set; }
    public bool HasVacatedDesk { get; private set; }
    public Bounds VisualBounds { get { var b = _renderers[0].bounds; foreach (var r in _renderers) b.Encapsulate(r.bounds); return b; } }
    public event Action<AAH_PatientActor> ArrivedAtDesk;
    public event Action<AAH_PatientActor> NavigationFailed;
    public event Action<AAH_PatientActor> LeftHospital;
    public bool HoldDischarge { get; set; }
    private void Awake() { _agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); }
    public void Initialize(PatientProfile profile, string id, AAH_RoomRegistry registry, AAH_PatientDoorAccess[] doors)
    {
        PatientId = id; DisplayName = profile.displayName; Dialogue = profile.dialogue; _registry = registry; _doors = doors;
        DialogueKey = profile.dialogueKey; IsAnomaly = profile.isAnomaly; IsRejected = false;
        if (IsAnomaly && !string.IsNullOrWhiteSpace(Dialogue))
        {
            string firstWord = Dialogue.Trim().Split(' ')[0].TrimEnd('.', ',', '…');
            Dialogue = firstWord + "… " + Dialogue;
        }
        _visual = Instantiate(profile.model, transform).transform;
        float yaw = profile.facingCorrection;
        // Polly's face and torso meshes provide a stable reference even when the import root is rotated.
        if (string.Equals(DisplayName, "Polly", StringComparison.OrdinalIgnoreCase))
            yaw = PollyYaw(yaw);
        _visual.localRotation = Quaternion.Euler(0f, yaw, 0f) * _visual.localRotation;
        _uprightModelRotation = _visual.localRotation;
        _visual.localPosition = Vector3.zero; _renderers = _visual.GetComponentsInChildren<Renderer>(true);
        if (_renderers.Length == 0) throw new InvalidOperationException("Patient model has no renderers.");
        // Imported GLB objects already carry their source-axis conversion. Normalize size, not their rotation.
        var b = VisualBounds; _visual.localScale *= _height / Mathf.Max(0.1f, b.size.y);
        b = VisualBounds; _visual.position += transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
        _uprightModelPosition = _visual.localPosition;
        foreach (var c in _visual.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var a in _visual.GetComponentsInChildren<Animator>(true))
        {
            a.applyRootMotion = false;
            a.enabled = false;
        }
    }
    private float PollyYaw(float fallback)
    {
        Vector3? face = null, body = null;
        foreach (var filter in _visual.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.TryGetComponent<MeshRenderer>(out var renderer)) continue;
            var material = renderer.sharedMaterial;
            if (material == null) continue;
            var center = transform.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
            if (material.name.StartsWith("Face1Mtl", StringComparison.OrdinalIgnoreCase)) face = center;
            if (material.name.StartsWith("Body1Mtl", StringComparison.OrdinalIgnoreCase)) body = center;
        }
        if (!face.HasValue || !body.HasValue) return fallback;
        var forward = Vector3.ProjectOnPlane(face.Value - body.Value, Vector3.up);
        return forward.sqrMagnitude > 0.000001f ? Vector3.SignedAngle(forward, Vector3.forward, Vector3.up) : fallback;
    }
    public void FacePhotoCamera(Vector3 cameraPosition)
    {
        if (_visual == null || State == PatientState.AtBed || State == PatientState.Leaving || ThreatPending) return;
        Vector3 direction = cameraPosition - VisualBounds.center;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        _agent.updateRotation = false;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        _visual.localRotation = _uprightModelRotation;
    }
    private Vector3 _outsidePosition;
    private Transform _entrance;
    public void WalkOutFromDesk()
    {
        if (IsRejected) return;
        IsRejected = true;
        StartCoroutine(RefusedExit());
    }
    private IEnumerator RefusedExit()
    {
        State = PatientState.Leaving;
        _agent.updateRotation = true;
        yield return MoveTo(_entrance.position);
        if (!_moveFailed) { InsideHospital = false; yield return MoveTo(_outsidePosition); }
        LeftHospital?.Invoke(this);
        gameObject.SetActive(false);
    }
    public void Approach(Transform entrance, Transform stand)
    {
        _outsidePosition = transform.position;
        _entrance = entrance;
        StartCoroutine(ApproachRoutine(entrance, stand));
    }
    private IEnumerator ApproachRoutine(Transform entrance, Transform stand)
    {
        State = PatientState.Approaching;
        yield return MoveTo(entrance.position);
        if (_moveFailed) yield break;
        InsideHospital = true;
        yield return MoveTo(stand.position);
        if (_moveFailed) yield break;
        _agent.updateRotation = false;
        transform.rotation = stand.rotation;
        State = PatientState.WaitingAtDesk; ArrivedAtDesk?.Invoke(this);
    }
    public void SetDeskState(PatientState state) { if (!ThreatPending && State != PatientState.Dead && State != PatientState.Leaving && State != PatientState.AtBed) State = state; }
    private void Update()
    {
        if (State == PatientState.Dead || _doors == null || State == PatientState.AtBed) return;
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
            CheckRoomThreshold();
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
        _enteringRoom = room;
        yield return MoveTo(room.bedApproach.position);
        if (_moveFailed) yield break;
        MarkRoomEntered();
        _homeRoom = room;
        LieOnBed(room);
        _registry.Occupy(PatientId, room.roomNumber);
    }

    private void LieOnBed(AAH_RoomRegistry.RoomSlot room)
    {
        _agent.enabled = false;
        _agent.updatePosition = true;
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
        State = PatientState.AtBed;
    }
    public void LeaveHospital(Transform exit)
    {
        if (exit == null) return;
        if (State == PatientState.ReturningToBed) { _queuedDischarge = exit; return; }
        if (State != PatientState.AtBed || ThreatPending) return;
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
        LeftHospital?.Invoke(this);
        gameObject.SetActive(false);
    }
    private AAH_RoomRegistry.RoomSlot _enteringRoom;
    private void CheckRoomThreshold()
    {
        if (_enteringRoom == null) return;
        Vector3 door = _enteringRoom.door.ApproachPosition;
        Vector3 intoRoom = Vector3.ProjectOnPlane(_enteringRoom.bedApproach.position - door, Vector3.up);
        if (intoRoom.sqrMagnitude > 0.01f && Vector3.Dot(transform.position - door, intoRoom.normalized) >= 0.25f)
            MarkRoomEntered();
    }

    private void MarkRoomEntered()
    {
        _enteringRoom = null;
        if (HasEnteredExamRoom) return;
        HasEnteredExamRoom = true;
        if (IsAnomaly && AAH_GameManager.Instance != null && AAH_GameManager.Instance.currentDay == 2)
        {
            ThreatPending = true;
            StartCoroutine(AttackAfterAdmission());
        }
    }

    public bool HasEnteredExamRoom { get; private set; }
    public bool ThreatPending { get; private set; }
    public BoxCollider BodyCollider { get; private set; }

    internal bool IsLivingTarget(MonoBehaviour target)
    {
        if (target == null || !target.isActiveAndEnabled || target.gameObject.scene != gameObject.scene)
            return false;
        if (target is AAH_PlayerSanity player) return !player.IsDepleted;
        return target is AAH_PatientActor patient && patient != this && patient.InsideHospital
            && patient.State != PatientState.Dead && !string.IsNullOrEmpty(patient.PatientId);
    }

    internal static Bounds TargetBounds(MonoBehaviour target)
    {
        if (target is AAH_PatientActor patient) return patient.VisualBounds;
        var body = target.GetComponent<CharacterController>();
        return body != null && body.enabled ? body.bounds : new Bounds(target.transform.position, Vector3.zero);
    }

    private IEnumerator AttackAfterAdmission()
    {
        yield return new WaitForSeconds(15f); // Measured from the room threshold.
        var manager = AAH_GameManager.Instance;
        // Arrival can still own the agent when the admission timer expires.
        while (manager != null && !manager.shiftEnded && State == PatientState.Leaving)
            yield return null;

        if (manager != null && !manager.shiftEnded && State == PatientState.AtBed)
            yield return Hunt(manager);

        ThreatPending = false;
    }

    private IEnumerator Hunt(AAH_GameManager manager)
    {
        // The actor root is still at the last walkable bedside point. Only its visual was put on the bed.
        _agent.enabled = true;
        if (!_agent.isOnNavMesh)
        {
            _agent.enabled = false;
            Debug.LogWarning("Anomaly cannot leave its bed: bedside point is off the NavMesh.", this);
            yield break;
        }

        _agent.isStopped = true;
        _agent.ResetPath();
        var bedside = transform.position;
        var bedsideRotation = transform.rotation;
        _agent.updatePosition = false;
        _agent.updateRotation = true;
        State = PatientState.Hunting;

        var panels = new System.Collections.Generic.List<Renderer>();
        if (_doors != null)
            foreach (var door in _doors)
                if (door != null && door.Panel != null)
                    panels.AddRange(door.Panel.GetComponentsInChildren<Renderer>(true));
        _doorPanels = panels.ToArray();

        var lyingPosition = _visual.localPosition;
        var lyingRotation = _visual.localRotation;
        const float riseDuration = 0.65f;
        for (float elapsed = 0f; elapsed < riseDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration);
            _visual.localPosition = Vector3.Lerp(lyingPosition, _uprightModelPosition, t);
            _visual.localRotation = Quaternion.Slerp(lyingRotation, _uprightModelRotation, t);
            yield return null;
        }
        _visual.localPosition = _uprightModelPosition;
        _visual.localRotation = _uprightModelRotation;

        MonoBehaviour target = null;
        float nextRefresh = 0f;
        while (manager != null && !manager.shiftEnded && _agent.isOnNavMesh)
        {
            if (Time.timeScale <= 0f) { yield return null; continue; }
            FollowAgentSafely();
            if (Time.time >= nextRefresh)
            {
                target = manager.NearestHuntTarget(this);
                if (target != null) ApproachTarget(target);
                else _agent.isStopped = true;
                nextRefresh = Time.time + HuntRefresh;
            }
            if (CanStrike(target))
            {
                _agent.isStopped = true;
                Vector3 facing = Vector3.ProjectOnPlane(TargetBounds(target).center - transform.position, Vector3.up);
                if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(facing);
                _visual.localRotation = Quaternion.Euler(12f, 0f, 0f) * _uprightModelRotation;
                yield return new WaitForSeconds(StrikeWindup);
                _visual.localRotation = _uprightModelRotation;
                // Recheck THIS target after the windup: it may have fled, died or closed a door.
                if (manager != null && manager.TryResolveAttack(this, target)) break;
                nextRefresh = 0f;
            }
            yield return null;
        }

        if (manager == null || manager.shiftEnded || !_agent.isOnNavMesh) yield break;
        // Keep the original room reservation and restore the treatment/discharge workflow after one victim.
        State = PatientState.ReturningToBed;
        ThreatPending = false;
        nextRefresh = 0f;
        while (!manager.shiftEnded && _agent.isOnNavMesh)
        {
            FollowAgentSafely();
            if (Vector3.Distance(transform.position, bedside) <= _agent.stoppingDistance + 0.4f)
            {
                transform.rotation = bedsideRotation;
                LieOnBed(_homeRoom);
                if (_queuedDischarge != null)
                {
                    var exit = _queuedDischarge;
                    _queuedDischarge = null;
                    LeaveHospital(exit);
                }
                yield break;
            }
            if (Time.time >= nextRefresh)
            {
                RouteTo(bedside);
                nextRefresh = Time.time + HuntRefresh;
            }
            yield return null;
        }
    }

    private void ApproachTarget(MonoBehaviour target)
    {
        // A bed patient's navigation root is beside the bed; aim for the patient-facing side if walkable.
        if (target is AAH_PatientActor patient && patient.State == PatientState.AtBed)
        {
            var nearBody = patient.VisualBounds.ClosestPoint(transform.position);
            nearBody.y = patient.transform.position.y;
            if (RouteTo(nearBody)) return;
        }
        RouteTo(target.transform.position);
    }

    private bool RouteTo(Vector3 destination)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };
        if (NavMesh.SamplePosition(destination, out var point, _height, filter) && _agent.SetDestination(point.position))
        {
            _agent.isStopped = false;
            return true;
        }
        _agent.isStopped = true;
        return false;
    }

    // Doors in these scenes can move independently of the baked NavMesh.
    private bool DoorBetween(Vector3 start, Vector3 end)
    {
        foreach (var panel in _doorPanels)
        {
            if (panel == null || !panel.enabled || !panel.gameObject.activeInHierarchy) continue;
            var a = panel.transform.InverseTransformPoint(start);
            var b = panel.transform.InverseTransformPoint(end);
            var bounds = panel.localBounds;
            if (bounds.Contains(a) || bounds.Contains(b)) return true;
            if (bounds.IntersectRay(new Ray(a, b - a), out var distance) && distance <= (b - a).magnitude)
                return true;
        }
        return false;
    }

    private void FollowAgentSafely()
    {
        var next = _agent.nextPosition;
        Vector3 delta = next - transform.position;
        if (delta.sqrMagnitude < 0.000001f) return;
        var chest = transform.position + Vector3.up * (_height * 0.5f);
        var probe = delta + delta.normalized * _agent.radius;
        if (DoorBetween(chest, chest + probe) ||
            Physics.Raycast(chest, probe.normalized, out var hit, probe.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<AAH_PlayerSanity>() == null &&
                hit.collider.GetComponentInParent<AAH_PatientActor>() != this)
        {
            _agent.nextPosition = transform.position;
            _agent.velocity = Vector3.zero;
            return;
        }
        transform.position = next;
    }

    internal bool CanStrike(MonoBehaviour target)
    {
        if (State != PatientState.Hunting || !ThreatPending || Time.timeScale <= 0f ||
            !_agent.enabled || !_agent.isOnNavMesh || !IsLivingTarget(target)) return false;
        Bounds bounds = TargetBounds(target);
        Vector3 start = transform.position;
        start.y = Mathf.Clamp(bounds.center.y, start.y + _height * 0.25f, start.y + _height * 0.75f);
        Vector3 contact = bounds.ClosestPoint(start);
        if ((contact - start).sqrMagnitude > _meleeReach * _meleeReach) return false;
        Vector3 end = bounds.center;
        if (DoorBetween(start, end)) return false;
        return !Obstructed(start, end, target.transform) && !Obstructed(end, start, target.transform);
    }

    private bool Obstructed(Vector3 start, Vector3 end, Transform target)
    {
        if (!Physics.Linecast(start, end, out var hit, ~0, QueryTriggerInteraction.Ignore)) return false;
        return !hit.collider.transform.IsChildOf(target) && !hit.collider.transform.IsChildOf(transform);
    }

    public void FallToFloor()
    {
        if (State == PatientState.Dead) return;
        State = PatientState.Dead;
        StopAllCoroutines();
        ThreatPending = false;
        if (_agent != null) _agent.enabled = false;
        if (_registry != null) _registry.ReleaseRoom(PatientId);
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var floor, 3f, UnityEngine.AI.NavMesh.AllAreas))
            transform.position = floor.position;
        _visual.localRotation = Quaternion.Euler(-90f, 0f, 0f) * _uprightModelRotation;
        var bounds = VisualBounds;
        _visual.position += transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        bounds = VisualBounds;
        var hitbox = new GameObject("BodyPickup");
        hitbox.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
        hitbox.transform.SetParent(transform, true);
        BodyCollider = hitbox.AddComponent<BoxCollider>();
        BodyCollider.size = Vector3.Max(bounds.size, Vector3.one * 0.2f);
        BodyCollider.isTrigger = true;
    }

    public bool IsAwayFrom(Vector3 point) => Vector3.Distance(transform.position, point) > 4f;
    private void OnDisable() { StopAllCoroutines(); }
}
