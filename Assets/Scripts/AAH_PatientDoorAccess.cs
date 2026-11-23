using UnityEngine;

public class AAH_PatientDoorAccess : MonoBehaviour, IAAH_PlayerDoor
{
    [Header("Door geometry")]
    [SerializeField] private Transform _panel;
    [SerializeField] private Transform _hinge;
    [SerializeField] private AAH_AutoDoor _existingDoor;
    [SerializeField] private float _angle = 90f;
    [SerializeField, Min(1f)] private float _speed = 100f;
    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private float _currentAngle;
    [Header("NPC auto opening")]
    [Tooltip("Turn OFF on the Reception door. Player E interaction still works.")]
    [SerializeField] private bool _allowNpcAutoOpen = true;
    [Tooltip("Close this long after the LAST nearby NPC open request, not the first.")]
    [SerializeField, Min(0.2f)] private float _autoCloseDelay = 2f;
    private bool _manualOpen;
    private float _npcOpenUntil = float.NegativeInfinity;
    private bool WantsOpen => _manualOpen || (_allowNpcAutoOpen && Time.time < _npcOpenUntil);
    public bool IsClear => _existingDoor != null ? _existingDoor.IsOpen : Mathf.Abs(_currentAngle) >= Mathf.Abs(_angle) - 2f;
    // Opening intent is used so E can reverse a door even while it moves.
    public bool IsOpen => _existingDoor != null ? _existingDoor.IsOpen : WantsOpen;
    public Transform Panel => _existingDoor != null ? _existingDoor.Panel : _panel;
    public bool CanPlayerToggle => _existingDoor != null ? _existingDoor.CanPlayerToggle : _panel != null && _hinge != null;
    public void ToggleFromPlayer()
    {
        if (!CanPlayerToggle) return;
        if (_existingDoor != null) { _existingDoor.ToggleFromPlayer(); return; }
        if (WantsOpen) RequestClose();
        else _manualOpen = true;
    }
    public void RequestClose()
    {
        if (_existingDoor != null) { _existingDoor.RequestClose(); return; }
        // A nearby patient keeps the door clear until their last request expires.
        _manualOpen = false;
    }
    public Vector3 ApproachPosition => _hinge != null ? _hinge.position : transform.position;
    private void Awake()
    {
        if (_panel != null) { _closedPosition = _panel.position; _closedRotation = _panel.rotation; }
    }
    public void RequestOpen()
    {
        // Existing patient and doctor scripts refresh this while near the door.
        // Repeated requests EXTEND the same deadline (no queued coroutines).
        if (!_allowNpcAutoOpen) return;
        _npcOpenUntil = Time.time + Mathf.Max(0.2f, _autoCloseDelay);
        if (_existingDoor != null) _existingDoor.OpenForPatient();
    }
    private void Update()
    {
        if (_existingDoor != null || _panel == null || _hinge == null) return;
        float targetAngle = WantsOpen ? _angle : 0f;
        if (Mathf.Approximately(_currentAngle, targetAngle)) return;
        _currentAngle = Mathf.MoveTowards(_currentAngle, targetAngle, _speed * Time.deltaTime);
        var turn = Quaternion.AngleAxis(_currentAngle, _hinge.up);
        _panel.SetPositionAndRotation(_hinge.position + turn * (_closedPosition - _hinge.position), turn * _closedRotation);
    }
}
