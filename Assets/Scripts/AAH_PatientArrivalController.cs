using System.Collections;
using UnityEngine;

public class AAH_PatientArrivalController : MonoBehaviour
{
    [Header("Queue")]
    [SerializeField] private AAH_CheckInSequence _sequence;
    [SerializeField] private AAH_RoomRegistry _rooms;
    [SerializeField] private AAH_PatientActor.PatientProfile[] _patients;
    [SerializeField] private Transform _spawn;
    [SerializeField] private Transform _entrance;
    [SerializeField] private Transform _stand;
    [SerializeField] private AAH_PatientDoorAccess[] _doors;
    [SerializeField, Min(0f)] private float _arrivalDelay = 4f;
    [SerializeField, Min(0.1f)] private float _walkingSpeed = 6f;
    [SerializeField] private int _agentTypeId;
    private int _nextPatient;
    private void Start()
    {
        string error;
        if (_sequence == null || _rooms == null || !_rooms.ValidateConfiguration(out error) || _spawn == null || _entrance == null || _stand == null)
        { Debug.LogError("Patient arrival configuration is incomplete.", this); enabled = false; return; }
        StartCoroutine(QueueRoutine());
    }
    private IEnumerator QueueRoutine()
    {
        yield return new WaitForSeconds(_arrivalDelay);
        while (_nextPatient < _patients.Length)
        {
            if (!_sequence.ReadyForPatient) { yield return null; continue; }
            var profile = _patients[_nextPatient];
            if (profile == null || profile.model == null) { Debug.LogError("Missing patient model at queue index " + _nextPatient, this); yield break; }
            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(_spawn.position, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            { Debug.LogError("Outside spawn is not on baked NavMesh.", this); yield break; }
            var go = new GameObject("CheckInPatient_" + (_nextPatient + 1) + "_" + profile.displayName);
            go.transform.SetParent(transform); go.transform.SetPositionAndRotation(hit.position, _spawn.rotation);
            go.SetActive(false);
            var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.agentTypeID = _agentTypeId;
            agent.radius = 1f; agent.height = 5.4f; agent.speed = _walkingSpeed; agent.acceleration = 12f;
            agent.angularSpeed = 180f; agent.stoppingDistance = 0.15f; agent.baseOffset = 0f;
            var actor = go.AddComponent<AAH_PatientActor>();
            actor.Initialize(profile, "PAT-" + (_nextPatient + 1).ToString("D3"), _rooms, _doors);
            go.SetActive(true);
            if (!_sequence.BeginPatient(actor)) { Destroy(go); yield return null; continue; }
            _nextPatient++; actor.Approach(_entrance, _stand);
            while (_sequence.HasPatient) yield return null;
            yield return new WaitForSeconds(_arrivalDelay);
        }
    }
    private void OnDisable() { StopAllCoroutines(); }
}
