using UnityEngine;

public class AAH_AutoDoor : MonoBehaviour
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

    private UnityEngine.InputSystem.InputAction _interactAction;
    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private Vector3 _hingePosition;
    private Vector3 _hingeAxis;
    private float _currentAngle;
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    public bool CanInteract => !_isOpen && _player != null && _interactionPoint != null
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
        _interactAction = new UnityEngine.InputSystem.InputAction("OpenReceptionDoor",
            UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        _interactAction.performed += OnInteract;
        if (_prompt != null) _prompt.SetActive(false);
    }

    private void OnEnable()
    {
        _interactAction?.Enable();
    }

    private void Update()
    {
        if (_prompt != null && _prompt.activeSelf != CanInteract)
            _prompt.SetActive(CanInteract);

        if (!_isOpen) return;
        _currentAngle = Mathf.MoveTowards(_currentAngle, _openAngle, _openSpeed * Time.deltaTime);
        Quaternion rotation = Quaternion.AngleAxis(_currentAngle, _hingeAxis);
        _door.SetPositionAndRotation(_hingePosition + rotation * (_closedPosition - _hingePosition),
            rotation * _closedRotation);
    }

    private void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!CanInteract) return;
        _isOpen = true;
        if (_prompt != null) _prompt.SetActive(false);
    }

    private void OnDisable()
    {
        _interactAction?.Disable();
        if (_prompt != null) _prompt.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_interactAction == null) return;
        _interactAction.performed -= OnInteract;
        _interactAction.Dispose();
    }
}

