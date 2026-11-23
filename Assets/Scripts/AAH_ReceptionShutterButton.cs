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
    private AAH_FocusOutline _outline;
    private Transform _appearance;
    private readonly RaycastHit[] _aimHits = new RaycastHit[32];
    public bool IsFocused => IsAimed();
    private AAH_ReceptionShutter _curtain;
    private bool _showHint;
    private void Update()
    {
        _showHint = IsAimed() && (_checkIn == null || !_checkIn.HasModalInteraction)
            && (AAH_GameManager.Instance == null || !AAH_GameManager.Instance.shiftEnded);
        _outline?.Show(_showHint);
    }
    private void OnGUI()
    {
        if (_showHint && _curtain != null)
            AAH_InteractionHints.Draw(_curtain.IsClosed ? "[E] Mở cửa sập" : "[E] Đóng cửa sập");
    }
    private void Awake()
    {
        _curtain = FindFirstObjectByType<AAH_ReceptionShutter>();
        if (_buttonCollider != null)
        {
            _appearance = _buttonCollider.transform.Find("ShutterButton_ReferenceAppearance");
            if (_appearance == null)
            {
                foreach (var mesh in FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (mesh.gameObject.scene != gameObject.scene || mesh.name != "ShutterButton_ReferenceAppearance") continue;
                    _appearance = mesh.transform;
                    break;
                }
            }
            var target = _appearance != null ? _appearance : _buttonCollider.transform;
            _outline = new AAH_FocusOutline(target);
            var filter = target.GetComponent<MeshFilter>();
            if (_appearance != null && filter != null && filter.sharedMesh != null && target.GetComponent<Collider>() == null)
            {
                var bounds = filter.sharedMesh.bounds;
                var hitbox = target.gameObject.AddComponent<BoxCollider>();
                hitbox.isTrigger = true;
                hitbox.center = bounds.center;
                hitbox.size = Vector3.Max(bounds.size, Vector3.one * 0.01f);
            }
        }
        _interact = new UnityEngine.InputSystem.InputAction("ReceptionShutter", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        _interact.performed += HandlePress;
    }
    private void OnEnable() { _interact?.Enable(); }
    private bool IsAimed()
    {
        if (_buttonCollider == null || _playerCamera == null || Time.timeScale <= 0f) return false;
        var ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out var hit, _rayDistance, ~0, QueryTriggerInteraction.Collide)) return false;
        if (hit.collider != _buttonCollider && hit.transform != _appearance)
        {
            // The moving curtain can cover its own switch collider. Other obstacles still block the ray.
            var curtain = hit.collider.GetComponentInParent<AAH_ReceptionShutter>();
            if (curtain == null) return false;
            var hits = _aimHits;
            int count = Physics.RaycastNonAlloc(ray, hits, _rayDistance, ~0, QueryTriggerInteraction.Collide);
            if (count == hits.Length)
            {
                hits = Physics.RaycastAll(ray, _rayDistance, ~0, QueryTriggerInteraction.Collide);
                count = hits.Length;
            }
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i];
                if (candidate.collider.GetComponentInParent<AAH_ReceptionShutter>() == curtain || candidate.distance >= closest) continue;
                closest = candidate.distance;
                hit = candidate;
            }
            if (hit.collider != _buttonCollider && hit.transform != _appearance) return false;
        }
        Vector3 position = _player != null ? _player.position : _playerCamera.transform.position;
        Vector3 nearest = hit.point; nearest.y = position.y;
        return Vector3.Distance(position, nearest) <= _interactionDistance;
    }
    private void HandlePress(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsAimed()) return;
        if (_checkIn != null && _checkIn.HasModalInteraction) return;
        if (AAH_GameManager.Instance != null && AAH_GameManager.Instance.shiftEnded) return;
        _onPressed.Invoke();
    }
    private void OnDisable() { _outline?.Show(false); _interact?.Disable(); }
    private void OnDestroy()
    {
        _outline?.Dispose();
        if (_interact == null) return;
        _interact.performed -= HandlePress; _interact.Dispose();
    }
}
