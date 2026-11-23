using System.Collections;
using UnityEngine;

public class AAH_PatientArrivalController : MonoBehaviour
{
    [Header("Queue")]
    [SerializeField] private AAH_GameManager _gameManager;
    [SerializeField] private AAH_CheckInSequence _sequence;
    [SerializeField] private AAH_RoomRegistry _rooms;
    [SerializeField] private AAH_PatientActor.PatientProfile[] _patients;
    [SerializeField] private Transform _spawn;
    [SerializeField] private Transform _entrance;
    [SerializeField] private Transform _stand;
    [SerializeField] private AAH_PatientDoorAccess[] _doors;
    [SerializeField, Min(0f)] private float _arrivalDelay = 4f;
    [SerializeField, Min(0.1f)] private float _walkingSpeed = 9f;
    [Tooltip("Hệ số tốc độ đi của bệnh nhân, áp dụng khi bệnh nhân xuất hiện.")]
    [SerializeField, Min(0.1f)] private float _patientSpeedMultiplier = 1.5f;
    [SerializeField] private int _agentTypeId;
    public Transform StandPoint => _stand;
    public float WalkingSpeed => _walkingSpeed;
    private int _nextPatient;
    private void Start()
    {
        string error;
        if (_gameManager == null || _gameManager.currentDay < 1 || _sequence == null || _rooms == null || !_rooms.ValidateConfiguration(out error) || _spawn == null || _entrance == null || _stand == null)
        { Debug.LogError("Patient arrival configuration is incomplete (day manager, room routes, or spawn references).", this); enabled = false; return; }
        if (_gameManager.currentDay == 2)
        {
            var roster = Resources.Load<AAH_Day2Visitors>("VetNightmare/Day2Visitors");
            var polly = _patients == null ? null : System.Array.Find(_patients, p => p != null && p.displayName == "Polly");
            if (roster == null || !roster.TryCreateQueue(polly, out _patients))
            {
                Debug.LogError("Ngày 2 thiếu Moratter, Polly hoặc hai model bệnh nhân. Kiểm tra danh sách Polly và chọn Tools > Vet Nightmare > Sync Day 2 visitors.", this);
                return;
            }
            _gameManager.BeginDay2(_patients.Length);
        }
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
            agent.radius = 1f; agent.height = 5.4f;
            agent.speed = _walkingSpeed * _patientSpeedMultiplier;
            agent.acceleration = 12f * _patientSpeedMultiplier;
            agent.angularSpeed = 180f; agent.stoppingDistance = 0.15f; agent.baseOffset = 0f;
            var actor = go.AddComponent<AAH_PatientActor>();
            actor.Initialize(profile, "PAT-" + (_nextPatient + 1).ToString("D3"), _rooms, _doors);
            actor.AssignIllness(_gameManager.currentDay, _nextPatient + 1);
            go.SetActive(true);
            if (!_sequence.BeginPatient(actor)) { Destroy(go); yield return null; continue; }
            string visitorId = actor.PatientId;
            _nextPatient++; actor.Approach(_entrance, _stand);
            while (_sequence.HasPatient) yield return null;
            if (_gameManager.currentDay == 2)
            {
                while (!_gameManager.HasDay2Outcome(visitorId) && !(actor != null && actor.IsAnomaly && actor.HasEnteredExamRoom))
                {
                    if (actor.State == AAH_PatientActor.PatientState.Blocked)
                    {
                        Debug.LogError("Ngày 2 dừng ở " + actor.DisplayName + ". Kiểm tra NavMesh và các điểm đến.", this);
                        yield break;
                    }
                    yield return null;
                }
                if (_nextPatient == _patients.Length)
                {
                    while (_gameManager.HasPendingAttack()) yield return null;
                    if (_gameManager.shiftEnded) yield break;
                    var story = FindFirstObjectByType<AAH_Day1Story>();
                    if (story != null) story.EndDay2Queue();
                    else Debug.LogError("Ngày 2 thiếu component AAH_Day1Story có tham chiếu tới bác sĩ.", this);
                    yield break;
                }
            }
            yield return new WaitForSeconds(_arrivalDelay);
        }
    }
    private void OnDisable() { StopAllCoroutines(); }
}
