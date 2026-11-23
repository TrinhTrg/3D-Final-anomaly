using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class AAH_PlayerDoorInteractor : MonoBehaviour
{
    [Serializable]
    public sealed class DoorBinding
    {
        public AAH_PatientDoorAccess controller;
        public Collider[] colliders;
    }

    [SerializeField] private SC_FPSController player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AAH_CheckInSequence checkIn;
    [SerializeField] private AAH_CheckInInteractionController checkInInteraction;
    [SerializeField, Min(0.1f)] private float interactionDistance = 7f;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private bool showPrompt = true;
    [Tooltip("Optional additional colliders for these doors.")]
    [SerializeField] private DoorBinding[] doors = Array.Empty<DoorBinding>();

    private readonly Dictionary<Collider, IAAH_PlayerDoor> targets = new Dictionary<Collider, IAAH_PlayerDoor>();
    private RaycastHit[] hits = new RaycastHit[48];
    private AAH_RoomTreatmentSequence[] treatmentRooms = Array.Empty<AAH_RoomTreatmentSequence>();
    private IAAH_PlayerDoor focused;
    private GUIStyle promptStyle;

    // A missing Reception component must not disable the room doors.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ListenForHospital()
    {
        SceneManager.sceneLoaded -= AttachToHospitalPlayer;
        SceneManager.sceneLoaded += AttachToHospitalPlayer;
    }

    private static void AttachToHospitalPlayer(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Hospital_day1" && scene.name != "Hospital_day2") return;
        foreach (var fps in FindObjectsByType<SC_FPSController>(FindObjectsSortMode.None))
        {
            if (fps.gameObject.scene == scene &&
                fps.GetComponentInChildren<AAH_PlayerDoorInteractor>(true) == null)
                fps.gameObject.AddComponent<AAH_PlayerDoorInteractor>();
        }
    }

    // Aiming through a window in the moving leaf should still select the door.
    // Intersect its local mesh bounds without adding colliders to the scene.
    private readonly Dictionary<MeshCollider, IAAH_PlayerDoor> doorMeshes =
        new Dictionary<MeshCollider, IAAH_PlayerDoor>();

    private void Start()
    {
        if (player == null) player = GetComponentInParent<SC_FPSController>();
        if (playerCamera == null && player != null) playerCamera = player.playerCamera;
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (checkIn == null) checkIn = FindFirstObjectByType<AAH_CheckInSequence>();
        if (checkInInteraction == null) checkInInteraction = FindFirstObjectByType<AAH_CheckInInteractionController>();
        treatmentRooms = FindObjectsByType<AAH_RoomTreatmentSequence>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var door in FindObjectsByType<AAH_PatientDoorAccess>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (door.gameObject.scene == gameObject.scene && door.Panel != null)
                Register(door, door.Panel.GetComponentsInChildren<Collider>(true));
        foreach (var door in FindObjectsByType<AAH_AutoDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (door.gameObject.scene == gameObject.scene && door.Panel != null)
                Register(door, door.Panel.GetComponentsInChildren<Collider>(true));
        if (doors != null)
            foreach (var binding in doors)
                if (binding != null && binding.controller != null && binding.colliders != null)
                    Register(binding.controller, binding.colliders);
    }

    private void Register(IAAH_PlayerDoor door, Collider[] colliders)
    {
        foreach (var col in colliders)
        {
            if (col == null) continue;
            targets[col] = door;
            if (col is MeshCollider mesh && mesh.sharedMesh != null)
                doorMeshes[mesh] = door;
        }
    }

    private void LateUpdate()
    {
        focused = null;
        if (AAH_HospitalHands.Local != null && AAH_HospitalHands.Local.OwnsInteraction) return;
        if (player == null || !player.isActiveAndEnabled || !player.canMove || playerCamera == null ||
            Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked) return;
        if (checkIn != null && checkIn.BlocksWorldInteraction) return;
        if (checkInInteraction != null && checkInInteraction.isActiveAndEnabled && checkInInteraction.FocusedTarget != null) return;

        foreach (var room in treatmentRooms)
            if (room != null && room.isActiveAndEnabled && room.IsFocused) return;

        Ray aim = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count;
        while ((count = Physics.RaycastNonAlloc(aim, hits, interactionDistance, raycastLayers,
                    QueryTriggerInteraction.Collide)) == hits.Length)
            Array.Resize(ref hits, hits.Length * 2);

        float nearest = interactionDistance;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.distance > nearest || hit.transform.IsChildOf(player.transform)) continue;
            targets.TryGetValue(hit.collider, out var door);
            if (hit.collider.isTrigger && door == null) continue;
            nearest = hit.distance;
            focused = door;
        }
        foreach (var entry in doorMeshes)
        {
            MeshCollider mesh = entry.Key;
            if (mesh == null || !mesh.enabled || !mesh.gameObject.activeInHierarchy ||
                (raycastLayers.value & (1 << mesh.gameObject.layer)) == 0) continue;
            var tr = mesh.transform;
            var localDirection = tr.InverseTransformVector(aim.direction);
            float scale = localDirection.magnitude;
            if (scale <= Mathf.Epsilon) continue;
            var ray = new Ray(tr.InverseTransformPoint(aim.origin), localDirection / scale);
            var bounds = mesh.sharedMesh.bounds;
            float localDistance = 0f;
            if (!bounds.Contains(ray.origin) && !bounds.IntersectRay(ray, out localDistance)) continue;
            float distance = localDistance / scale;
            if (distance >= nearest) continue;
            nearest = distance;
            focused = entry.Value;
        }
        if (!(focused is MonoBehaviour component) || component == null || !component.isActiveAndEnabled || !focused.CanPlayerToggle)
        {
            focused = null;
            return;
        }
#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        bool pressed = Input.GetKeyDown(KeyCode.E);
#else
        bool pressed = false;
#endif
        if (pressed) focused.ToggleFromPlayer();
    }

    private void OnDisable() { focused = null; }
    private void OnGUI()
    {
        if (!showPrompt || focused == null || Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked) return;
        if (promptStyle == null) promptStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
        promptStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.025f), 16, 28);
        GUI.Box(new Rect((Screen.width - 260f) / 2f, (Screen.height - 44f) * 0.5f, 260f, 44f),
            focused.IsOpen ? "[E] Đóng cửa" : "[E] Mở cửa", promptStyle);
    }
}

public interface IAAH_PlayerDoor
{
    bool IsOpen { get; }
    bool CanPlayerToggle { get; }
    void ToggleFromPlayer();
}
