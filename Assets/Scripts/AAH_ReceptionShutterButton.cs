using UnityEngine;

public sealed class AAH_ReceptionShutterButton : MonoBehaviour
{
    [Header("Existing button interaction")]
    [SerializeField] private Collider _buttonCollider;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _player;
    [SerializeField] private AAH_CheckInSequence _checkIn;
    [SerializeField, Min(0.1f)] private float _interactionDistance = 5f;
    [SerializeField, Min(0.1f)] private float _rayDistance = 10f;
    [SerializeField] private UnityEngine.Events.UnityEvent _onPressed = new UnityEngine.Events.UnityEvent();
    private UnityEngine.InputSystem.InputAction _interact;
    public UnityEngine.Events.UnityEvent OnPressed => _onPressed;
    public bool IsFocused => CanPress();
    private void Awake()
    {
        _interact = new UnityEngine.InputSystem.InputAction("ReceptionShutter", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        _interact.performed += HandlePress;
    }
    private void OnEnable() { _interact?.Enable(); }
    private bool CanPress()
    {
        if (_buttonCollider == null || _playerCamera == null || Time.timeScale <= 0f || (_checkIn != null && _checkIn.Busy)) return false;
        RaycastHit hit;
        if (!Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hit, _rayDistance, ~0, QueryTriggerInteraction.Collide) || hit.collider != _buttonCollider) return false;
        Vector3 position = _player != null ? _player.position : _playerCamera.transform.position;
        Vector3 nearest = hit.collider.ClosestPoint(position); nearest.y = position.y;
        return Vector3.Distance(position, nearest) <= _interactionDistance;
    }
    private void HandlePress(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (CanPress()) _onPressed.Invoke();
    }
    private void OnDisable() { _interact?.Disable(); }
    private void OnDestroy()
    {
        if (_interact == null) return;
        _interact.performed -= HandlePress; _interact.Dispose();
    }
}
