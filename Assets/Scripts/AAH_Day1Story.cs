using System.Collections;
using System.Collections.Generic;
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
    private const int Day1PatientCount = 3;
    private readonly HashSet<string> _recoveredPatientIds = new HashSet<string>();
    private bool _examined, _muppleEntranceStarted;
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
        _gameManager.SetDay1Objective("Ngày 1: Tiếp nhận và điều trị đủ 3 bệnh nhân.");
    }
    private void OnExamination(AAH_PatientActor patient)
    {
        if (!IsDay1 || _examined || patient == null || patient.DisplayName != "Polly") return;
        _examined = true; _polly = patient;
        _examination = StartCoroutine(ExaminationDialogue());
    }
    private IEnumerator ExaminationDialogue()
    {
        yield return _dialogue.ShowStoryDialogue("Polly", _data, _data.Examination);
        if (!_muppleEntranceStarted && !_recoveredPatientIds.Contains(_polly.PatientId))
            _gameManager.SetDay1Objective("Ngày 1: Phân tích và điều trị chứng đau đầu cho Polly.");
        _examination = null;
    }
    private void OnRecovery(AAH_PatientActor patient)
    {
        if (!IsDay1 || _muppleEntranceStarted || patient == null ||
            string.IsNullOrEmpty(patient.PatientId) || !_recoveredPatientIds.Add(patient.PatientId))
            return;

        if (patient == _polly)
        {
            patient.HoldDischarge = true;
            StartCoroutine(RecoveryDialogue());
        }

        if (_recoveredPatientIds.Count < Day1PatientCount)
        {
            _gameManager.SetDay1Objective($"Ngày 1: Đã điều trị {_recoveredPatientIds.Count}/{Day1PatientCount} bệnh nhân.");
            return;
        }

        _muppleEntranceStarted = true;
        _gameManager.SetDay1Objective("Ngày 1: Lắng nghe lời dặn của bác sĩ Mupple.");
        StartCoroutine(_mupple.PlayDay1Entrance(_player, _dialogue, _data));
    }
    private IEnumerator RecoveryDialogue()
    {
        while (_examination != null) yield return null;
        yield return _dialogue.ShowStoryDialogue("Polly", _data, _data.Recovery);
        if (_polly != null) _polly.HoldDischarge = false;
    }
    public void EndDay2Queue()
    {
        if (_gameManager == null || _gameManager.currentDay != 2 || _muppleEntranceStarted) return;
        if (_mupple == null || _data == null || _dialogue == null || _dialogue.panelDialogue == null || _dialogue.txtDialogueContent == null)
        {
            Debug.LogError("Thiếu tham chiếu Mupple hoặc phần hội thoại trong scene Ngày 2.", this);
            return;
        }
        _muppleEntranceStarted = true;
        _gameManager.SetDay1Objective("Ngày 2: Gặp bác sĩ Mupple ở quầy lễ tân.");
        StartCoroutine(_mupple.PlayDay2Entrance(_player, _dialogue, _data, FinishDay2));
    }
    private void FinishDay2()
    {
        if (Completed) return;
        Completed = true;
        _mupple.TriggerStoreRoomExit();
        _gameManager.CompleteDay2Story();
    }
    private void OnMuppleCompleted()
    {
        if (!IsDay1 || !_muppleEntranceStarted || Completed) return;
        Completed = true;
        _gameManager.CompleteDay1Story();
        _mupple.TriggerStoreRoomExit();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (_polly != null) _polly.HoldDischarge = false;
        if (_treatmentRooms != null) foreach (var room in _treatmentRooms)
        {
            if (room == null) continue;
            room.ExaminationStarted -= OnExamination;
            room.PatientRecovered -= OnRecovery;
        }
        if (_mupple != null) _mupple.Day1DialogueCompleted -= OnMuppleCompleted;
    }
}
