using UnityEngine;

public class AAH_AutoDoor : MonoBehaviour, IAAH_PlayerDoor
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _door;
    [SerializeField] private Transform _interactionPoint;
    [SerializeField] private Transform _hinge;
    [SerializeField] private GameObject _prompt;

    [Header("Door settings")]
    [SerializeField, Min(0.1f)] private float _interactionDistance = 3f;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField, Min(1f)] private float _openSpeed = 100f;

    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private Vector3 _hingePosition;
    private Vector3 _hingeAxis;
    private float _currentAngle;
    private bool _isOpen;

    public Transform Panel => _door;
    public bool CanPlayerToggle => _door != null && _hinge != null;
    public bool IsOpen => _isOpen;
    public bool CanInteract => _player != null && _interactionPoint != null
        && Vector3.Distance(_player.position, _interactionPoint.position) <= _interactionDistance
        && Time.timeScale > 0f;

    private void Awake()
    {
        if (_door == null || _hinge == null || _player == null || _interactionPoint == null)
        {
            Debug.LogError("AAH_AutoDoor: Assign Player, Door, Interaction Point and Hinge in Inspector.", this);
            if (_prompt != null) _prompt.SetActive(false);
            enabled = false;
            return;
        }

        _closedPosition = _door.position;
        _closedRotation = _door.rotation;
        _hingePosition = _hinge.position;
        _hingeAxis = _hinge.up;
        if (_prompt != null) _prompt.SetActive(false);
    }

    private void Start()
    {
        // The reception door already references the player in the hospital scenes.
        var fps = _player.GetComponentInParent<SC_FPSController>();
        if (fps == null) fps = _player.GetComponentInChildren<SC_FPSController>();
        if (fps != null && fps.GetComponentInChildren<AAH_PlayerDoorInteractor>(true) == null)
            fps.gameObject.AddComponent<AAH_PlayerDoorInteractor>();
    }

    private void Update()
    {
        float targetAngle = _isOpen ? _openAngle : 0f;
        if (Mathf.Approximately(_currentAngle, targetAngle)) return;
        _currentAngle = Mathf.MoveTowards(_currentAngle, targetAngle, _openSpeed * Time.deltaTime);
        Quaternion rotation = Quaternion.AngleAxis(_currentAngle, _hingeAxis);
        _door.SetPositionAndRotation(_hingePosition + rotation * (_closedPosition - _hingePosition),
            rotation * _closedRotation);
    }

    public void OpenForPatient()
    {
        if (_door == null || _hinge == null) return;
        _isOpen = true;
        if (_prompt != null) _prompt.SetActive(false);
    }

    public void ToggleFromPlayer()
    {
        if (CanPlayerToggle) _isOpen = !_isOpen;
    }

    public void RequestClose() { _isOpen = false; }
}
