using UnityEngine;

public class AAH_PatientDoorAccess : MonoBehaviour
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
    private bool _requested;
    public bool IsClear => _existingDoor != null ? _existingDoor.IsOpen : Mathf.Abs(_currentAngle) >= Mathf.Abs(_angle) - 2f;
    public Vector3 ApproachPosition => _hinge != null ? _hinge.position : transform.position;
    private void Awake()
    {
        if (_panel != null) { _closedPosition = _panel.position; _closedRotation = _panel.rotation; }
    }
    public void RequestOpen()
    {
        _requested = true;
        if (_existingDoor != null) _existingDoor.OpenForPatient();
    }
    private void Update()
    {
        if (!_requested || _existingDoor != null || _panel == null || _hinge == null) return;
        _currentAngle = Mathf.MoveTowards(_currentAngle, _angle, _speed * Time.deltaTime);
        var turn = Quaternion.AngleAxis(_currentAngle, _hinge.up);
        _panel.SetPositionAndRotation(_hinge.position + turn * (_closedPosition - _hinge.position), turn * _closedRotation);
    }
}
