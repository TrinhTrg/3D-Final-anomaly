using UnityEngine;
using System.Collections.Generic;

public class AAH_CheckInInteractionController : MonoBehaviour
{
    [SerializeField] private AAH_CheckInSequence _sequence;
    [SerializeField] private Camera _camera;
    [SerializeField] private AAH_InteractableStep[] _targets;
    private readonly Dictionary<Collider, AAH_InteractableStep> _selection = new Dictionary<Collider, AAH_InteractableStep>();
    private const float DeskDeviceReach = 5f;
    private const float MinimumPhotoReach = 8f;
    private readonly List<MeshRenderer> _photoParts = new List<MeshRenderer>();
    private readonly List<MeshRenderer> _computerParts = new List<MeshRenderer>();
    private readonly List<MeshRenderer> _printerParts = new List<MeshRenderer>();
    private RectTransform _registrationDisplay;

    private UnityEngine.InputSystem.InputAction _interact, _cancel;
    public AAH_InteractableStep FocusedTarget { get; private set; }
    private void Awake()
    {
        foreach (var step in _targets)
        {
            if (step == null) continue;
            if (step.targetCollider != null) _selection[step.targetCollider] = step;
            if (step.additionalColliders != null)
                foreach (var collider in step.additionalColliders) if (collider != null) _selection[collider] = step;
            foreach (var collider in step.GetComponentsInChildren<Collider>(true)) _selection[collider] = step;
        }
        _interact = new UnityEngine.InputSystem.InputAction("CheckInInteract", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        _cancel = new UnityEngine.InputSystem.InputAction("CloseClipboard", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
        _interact.performed += Interact; _cancel.performed += Cancel;
    }
    private void Start()
    {
        if (_sequence == null) return;
        CollectParts(_sequence.stepCameraTripod, _photoParts);
        CollectParts(_sequence.stepComputer, _computerParts);
        CollectParts(_sequence.stepPrinter, _printerParts);
        FitPrinterRing();
    }

    private void CollectParts(AAH_InteractableStep step, List<MeshRenderer> parts)
    {
        if (step == null) return;
        void AddModel(Transform model)
        {
            foreach (var part in model.GetComponentsInChildren<MeshRenderer>(true))
                if (!parts.Contains(part)) parts.Add(part);
            if (step != _sequence.stepComputer) return;
            foreach (var canvas in model.GetComponentsInChildren<Canvas>(true))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.name == "Reception_RegistrationDisplay")
                    _registrationDisplay = canvas.transform as RectTransform;
        }

        if (step.targetCollider != null) AddModel(step.targetCollider.transform);
        if (step.additionalColliders != null)
            foreach (var part in step.additionalColliders)
                if (part != null) AddModel(part.transform);

        if (step == _sequence.stepCameraTripod) AddModel(step.transform);

        // The imported hospital stores pieces of one device on sibling nodes.
        Transform parent = step.targetCollider != null ? step.targetCollider.transform.parent : null;
        if (parent == null) return;
        Vector3 anchor = step.targetCollider.bounds.center;
        foreach (Transform sibling in parent)
        {
            string label = sibling.name;
            if (step == _sequence.stepCameraTripod)
            {
                // Some imports split the nearby tripod and camera housing into separate meshes.
                if (label.IndexOf("camera", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    label.IndexOf("tripod", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var part = sibling.GetComponentInChildren<MeshRenderer>();
                if (part == null || part.bounds.SqrDistance(anchor) > 1.5f * 1.5f) continue;
            }
            else if (step == _sequence.stepPrinter)
            {
                if (!label.StartsWith("printer", System.StringComparison.OrdinalIgnoreCase)) continue;
                var part = sibling.GetComponentInChildren<MeshRenderer>();
                if (part == null || part.bounds.SqrDistance(anchor) > 2.5f * 2.5f) continue;
            }
            else if (label != "computer_reception" && label != "computer_screen_reception") continue;
            AddModel(sibling);
        }
    }

    private void FitPrinterRing()
    {
        var printer = _sequence.stepPrinter;
        var computer = _sequence.stepComputer;
        if (printer == null || computer == null || computer.highlightRing == null) return;
        var style = computer.highlightRing.GetComponent<LineRenderer>();
        if (style == null) return;

        Bounds bounds = default;
        bool found = false;
        foreach (var part in _printerParts)
        {
            if (part == null || !part.enabled || !part.gameObject.activeInHierarchy) continue;
            if (found) bounds.Encapsulate(part.bounds);
            else { bounds = part.bounds; found = true; }
        }
        if (!found)
        {
            if (printer.targetCollider == null) return;
            bounds = printer.targetCollider.bounds;
        }

        if (printer.highlightRing == null)
            printer.highlightRing = Instantiate(computer.highlightRing, printer.transform);
        var ring = printer.highlightRing.GetComponent<LineRenderer>();
        if (ring == null) ring = printer.highlightRing.AddComponent<LineRenderer>();
        ring.sharedMaterial = style.sharedMaterial;
        ring.colorGradient = style.colorGradient;
        ring.widthCurve = style.widthCurve;
        ring.widthMultiplier = Mathf.Max(0.05f, style.widthMultiplier);
        ring.enabled = true;
        ring.loop = true;
        ring.useWorldSpace = false;
        ring.positionCount = 64;
        var center = bounds.center;
        center.y = bounds.min.y + Mathf.Max(0.12f, bounds.size.y * 0.25f);
        // Keep the front arc outside the casing and above the counter.
        float rx = bounds.extents.x * 1.42f + 0.12f;
        float rz = bounds.extents.z * 1.42f + 0.12f;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            var point = center + new Vector3(rx * Mathf.Cos(angle), 0f, rz * Mathf.Sin(angle));
            ring.SetPosition(i, ring.transform.InverseTransformPoint(point));
        }
        printer.highlightRing.SetActive(printer.IsStepActive);
    }

    private AAH_InteractableStep LookAtDevice(AAH_InteractableStep step, List<MeshRenderer> parts, Ray ray)
    {
        bool photo = step == _sequence.stepCameraTripod;
        float reach = photo ? Mathf.Max(MinimumPhotoReach, step.interactionDistance + 1f) : DeskDeviceReach;
        float rayLength = photo ? Mathf.Max(11f, reach + 1f) : DeskDeviceReach;
        Bounds focusedBounds = default;
        float surface = float.PositiveInfinity;
        foreach (var part in parts)
        {
            if (part == null || !part.enabled || !part.gameObject.activeInHierarchy) continue;
            // Ray tests in mesh space handle rotated, scaled hospital imports and hollow screens.
            var t = part.transform;
            var localRay = new Ray(t.InverseTransformPoint(ray.origin), t.InverseTransformVector(ray.direction));
            if (!part.localBounds.IntersectRay(localRay, out float along)) continue;
            var worldHit = t.TransformPoint(localRay.GetPoint(along));
            float distance = Vector3.Distance(ray.origin, worldHit);
            if (distance >= surface) continue;
            surface = distance;
            focusedBounds = part.bounds;
        }

        if (step == _sequence.stepComputer && _registrationDisplay != null && _registrationDisplay.gameObject.activeInHierarchy)
        {
            var plane = new Plane(_registrationDisplay.forward, _registrationDisplay.position);
            if (plane.Raycast(ray, out float distance) &&
                _registrationDisplay.rect.Contains(_registrationDisplay.InverseTransformPoint(ray.GetPoint(distance))))
                surface = Mathf.Min(surface, distance);
        }

        // A nearer wall or another prop blocks selection. Non-solid trigger volumes do not hide a machine.
        if (Physics.Raycast(ray, out var hit, rayLength, ~0, QueryTriggerInteraction.Ignore))
        {
            if (_selection.TryGetValue(hit.collider, out var owner) && owner == step)
            {
                if (hit.distance < surface)
                {
                    surface = hit.distance;
                    focusedBounds = hit.collider.bounds;
                }
            }
            else if (hit.distance + 0.005f < surface)
                return null;
        }
        if (surface > rayLength) return null;
        if (photo)
        {
            // Measure horizontal reach to the near side
            // of the part in focus, even when the ray passes through a gap in its mesh.
            Vector3 player = _sequence.playerController != null
                ? _sequence.playerController.transform.position : _camera.transform.position;
            Vector3 offset = focusedBounds.ClosestPoint(player) - player;
            offset.y = 0f;
            if (offset.sqrMagnitude > reach * reach) return null;
        }
        return step;
    }

    private void OnEnable() { _interact?.Enable(); _cancel?.Enable(); }
    private AAH_InteractableStep Select()
    {
        if (_sequence == null || _camera == null || _sequence.Busy || Time.timeScale <= 0f) return null;
        Ray aim = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (_sequence.stepCameraTripod != null && _sequence.stepCameraTripod.IsStepActive)
            return LookAtDevice(_sequence.stepCameraTripod, _photoParts, aim);
        if (_sequence.stepComputer != null && _sequence.stepComputer.IsStepActive)
            return LookAtDevice(_sequence.stepComputer, _computerParts, aim);
        if (_sequence.stepPrinter != null && _sequence.stepPrinter.IsStepActive)
            return LookAtDevice(_sequence.stepPrinter, _printerParts, aim);
        RaycastHit hit;
        if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, 10f, ~0, QueryTriggerInteraction.Collide)) return null;
        AAH_InteractableStep target;
        if (!_selection.TryGetValue(hit.collider, out target) || !target.IsStepActive) return null;
        // Hospital models are tall: camera-to-desk height must not consume interaction reach.
        Vector3 playerPosition = _sequence.playerController != null
            ? _sequence.playerController.transform.position : _camera.transform.position;
        bool supportsClosestPoint = hit.collider is BoxCollider || hit.collider is SphereCollider ||
            hit.collider is CapsuleCollider || (hit.collider is MeshCollider mesh && mesh.convex);
        Vector3 nearestPoint = supportsClosestPoint ? hit.collider.ClosestPoint(playerPosition) : hit.point;
        nearestPoint.y = playerPosition.y;
        if (Vector3.Distance(playerPosition, nearestPoint) > target.interactionDistance) return null;
        return target;
    }
    private void Update()
    {
        FocusedTarget = Select();
        if (_sequence != null && _sequence.txtInteractionPrompt != null)
            _sequence.txtInteractionPrompt.text = FocusedTarget != null ? "[E] " + FocusedTarget.promptMessage : "";
    }
    private void Interact(UnityEngine.InputSystem.InputAction.CallbackContext context)
    { var target = Select(); if (target != null) target.TriggerInteraction(); }
    private void Cancel(UnityEngine.InputSystem.InputAction.CallbackContext context)
    { if (_sequence != null && Time.timeScale > 0f) _sequence.CancelClipboard(); }
    private void OnDisable() { _interact?.Disable(); _cancel?.Disable(); }
    private void OnDestroy()
    {
        if (_interact != null) { _interact.performed -= Interact; _interact.Dispose(); }
        if (_cancel != null) { _cancel.performed -= Cancel; _cancel.Dispose(); }
    }
}
