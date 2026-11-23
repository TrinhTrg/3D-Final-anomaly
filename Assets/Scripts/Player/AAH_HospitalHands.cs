using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// The selected inventory slot and a carried body share the same hands.
[DisallowMultipleComponent]
public sealed class AAH_HospitalHands : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _reach = 8f;
    private SC_FPSController _player;
    private Camera _camera;
    private AAH_PlayerInventory _inventory;
    private AAH_RoomTreatmentSequence[] _rooms;
    private AAH_PatientActor _carried;
    private string _hint;
    internal static AAH_HospitalHands Local { get; private set; }
    public bool OwnsInteraction { get; private set; }
    public bool CarryingBody => _carried != null;
    private void Awake()
    {
        Local = this;
        _player = GetComponent<SC_FPSController>();
        _camera = _player != null ? _player.playerCamera : null;
        _inventory = GetComponentInChildren<AAH_PlayerInventory>(true);
        if (_inventory == null) _inventory = FindFirstObjectByType<AAH_PlayerInventory>();
        _rooms = FindObjectsByType<AAH_RoomTreatmentSequence>(FindObjectsSortMode.None);
        AAH_TrashCan.Bind(gameObject.scene);
    }

    private void Update()
    {
        _hint = null;
        OwnsInteraction = false;
        if (_camera == null || _player == null || !_player.canMove || Time.timeScale <= 0f ||
            Cursor.lockState != CursorLockMode.Locked || (AAH_GameManager.Instance != null && AAH_GameManager.Instance.shiftEnded)) return;
        var checkIn = AAH_CheckInSequence.Instance;
        if (checkIn != null && checkIn.BlocksWorldInteraction) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (CarryingBody && keyboard.gKey.wasPressedThisFrame)
        {
            DropBody();
            return;
        }
        if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out var hit,
                _reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) return;
        if (hit.collider.GetComponentInParent<AAH_TrashCan>() != null)
        {
            OwnsInteraction = true;
            string held = CarryingBody ? _carried.DisplayName : _inventory != null ? _inventory.SelectedItem?.DisplayName : null;
            if (string.IsNullOrEmpty(held)) return;
            _hint = "[E] Bỏ " + held + " vào thùng rác";
            if (keyboard.eKey.wasPressedThisFrame) Discard();
            return;
        }
        if (CarryingBody) return;
        var actor = hit.collider.GetComponentInParent<AAH_PatientActor>();
        if (actor == null || actor.State != AAH_PatientActor.PatientState.Dead || actor.BodyCollider != hit.collider) return;
        OwnsInteraction = true;
        _hint = "[E] Xách " + actor.DisplayName;
        if (!keyboard.eKey.wasPressedThisFrame) return;
        _carried = actor;
        actor.BodyCollider.enabled = false;
        if (_inventory != null) _inventory.SetBodyCarried(true);
        _hint = null;
    }

    private void LateUpdate()
    {
        if (!CarryingBody || _camera == null) return;
        Transform cameraTransform = _camera.transform;
        _carried.transform.SetPositionAndRotation(cameraTransform.TransformPoint(new Vector3(1.5f, -2.2f, 2.6f)),
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
    }

    private void DropBody()
    {
        Vector3 feet = transform.position;
        if (TryGetComponent<CharacterController>(out var capsule))
            feet.y = capsule.bounds.min.y;
        if (!NavMesh.SamplePosition(feet, out var origin, 3f, NavMesh.AllAreas)) return;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 desired = origin.position + forward * 2.5f;
        NavMeshHit hit;
        Vector3 floor = NavMesh.Raycast(origin.position, desired, out hit, NavMesh.AllAreas)
            ? Vector3.MoveTowards(hit.position, origin.position, 0.45f) : desired;
        if (!NavMesh.SamplePosition(floor, out hit, 1f, NavMesh.AllAreas)) return;
        _carried.transform.position = hit.position;
        _carried.BodyCollider.enabled = true;
        ReleaseHands();
    }

    private void Discard()
    {
        _hint = null;
        if (CarryingBody)
        {
            var body = _carried;
            ReleaseHands();
            body.gameObject.SetActive(false);
            Destroy(body.gameObject);
            return;
        }
        if (_inventory == null || _inventory.SelectedItem == null) return;
        foreach (var room in _rooms)
            if (room != null && room.DiscardSelectedDose(_inventory)) return;
        _inventory.RemoveAt(_inventory.SelectedIndex);
    }

    private void ReleaseHands()
    {
        _carried = null;
        if (_inventory != null) _inventory.SetBodyCarried(false);
    }

    private void OnGUI()
    {
        AAH_InteractionHints.Draw(_hint);
        if (CarryingBody && Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked)
            GUI.Label(new Rect(16f, Screen.height * 0.72f, 350f, 34f), "[G] Đặt " + _carried.DisplayName + " xuống");
    }

    private void OnDestroy()
    {
        if (Local == this) Local = null;
    }
}
