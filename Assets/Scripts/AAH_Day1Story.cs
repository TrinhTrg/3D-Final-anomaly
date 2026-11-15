using System.Collections;
using UnityEngine;

public class AAH_Day1Story : MonoBehaviour
{
    [Header("Existing systems")]
    [SerializeField] private AAH_GameManager _gameManager;
    [SerializeField] private AAH_CheckInSequence _dialogue;
    [SerializeField] private AAH_RoomTreatmentSequence[] _treatmentRooms;
    [SerializeField] private AAH_DrMupple _mupple;
    [SerializeField] private Transform _player;
    [SerializeField] private AAH_Day1Dialogue _data;
    private AAH_PatientActor _polly;
    private Coroutine _examination;
    private bool _examined, _recovered, _departed;
    public bool Completed { get; private set; }
    private bool IsDay1 => _gameManager != null && _gameManager.currentDay == 1;

    private void OnEnable()
    {
        if (_treatmentRooms != null) foreach (var room in _treatmentRooms)
        {
            if (room == null) continue;
            room.ExaminationStarted += OnExamination;
            room.PatientRecovered += OnRecovery;
        }
        if (_mupple != null) _mupple.Day1DialogueCompleted += OnMuppleCompleted;
    }
    private void Start()
    {
        if (!IsDay1) return;
        if (_data == null || _dialogue == null || _dialogue.panelDialogue == null || _dialogue.txtDialogueContent == null || _mupple == null || _player == null)
        { Debug.LogError("Day 1 story references are incomplete.", this); enabled = false; return; }
        _gameManager.SetDay1Objective("Ngày 1: Tiếp nhận, khám và điều trị cho Polly.");
    }
    private void OnExamination(AAH_PatientActor patient)
    {
        if (!IsDay1 || _examined || patient == null || patient.DisplayName != "Polly") return;
        _examined = true; _polly = patient;
        _polly.LeftHospital += OnDeparture;
        _examination = StartCoroutine(ExaminationDialogue());
    }
    private IEnumerator ExaminationDialogue()
    {
        yield return _dialogue.ShowStoryDialogue("Polly", _data, _data.Examination);
        _gameManager.SetDay1Objective("Ngày 1: Phân tích và điều trị chứng đau đầu cho Polly.");
        _examination = null;
    }
    private void OnRecovery(AAH_PatientActor patient)
    {
        if (!IsDay1 || !_examined || _recovered || patient != _polly) return;
        _recovered = true;
        patient.HoldDischarge = true;
        StartCoroutine(RecoveryDialogue());
    }
    private IEnumerator RecoveryDialogue()
    {
        while (_examination != null) yield return null;
        yield return _dialogue.ShowStoryDialogue("Polly", _data, _data.Recovery);
        _gameManager.SetDay1Objective("Ngày 1: Chờ Polly rời bệnh viện.");
        if (_polly != null) _polly.HoldDischarge = false;
    }
    private void OnDeparture(AAH_PatientActor patient)
    {
        if (!_recovered || _departed || patient != _polly) return;
        _departed = true;
        _polly.LeftHospital -= OnDeparture;
        _gameManager.SetDay1Objective("Ngày 1: Lắng nghe lời dặn của bác sĩ Muple.");
        StartCoroutine(_mupple.PlayDay1Entrance(_player, _dialogue, _data));
    }
    private void OnMuppleCompleted()
    {
        if (!IsDay1 || !_departed || Completed) return;
        Completed = true;
        _gameManager.CompleteDay1Story();
        _mupple.TriggerStoreRoomExit();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (_polly != null) { _polly.HoldDischarge = false; _polly.LeftHospital -= OnDeparture; }
        if (_treatmentRooms != null) foreach (var room in _treatmentRooms)
        {
            if (room == null) continue;
            room.ExaminationStarted -= OnExamination;
            room.PatientRecovered -= OnRecovery;
        }
        if (_mupple != null) _mupple.Day1DialogueCompleted -= OnMuppleCompleted;
    }
}
