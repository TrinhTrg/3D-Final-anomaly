using UnityEngine;
using System.Collections.Generic;

public class AAH_CheckInInteractionController : MonoBehaviour
{
    [SerializeField] private AAH_CheckInSequence _sequence;
    [SerializeField] private Camera _camera;
    [SerializeField] private AAH_InteractableStep[] _targets;
    private readonly Dictionary<Collider, AAH_InteractableStep> _selection = new Dictionary<Collider, AAH_InteractableStep>();
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
    private void OnEnable() { _interact?.Enable(); _cancel?.Enable(); }
    private AAH_InteractableStep Select()
    {
        if (_sequence == null || _camera == null || _sequence.Busy || Time.timeScale <= 0f) return null;
        RaycastHit hit;
        if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, 10f, ~0, QueryTriggerInteraction.Collide)) return null;
        AAH_InteractableStep target;
        if (!_selection.TryGetValue(hit.collider, out target) || !target.IsStepActive) return null;
        // Hospital models are tall: camera-to-desk height must not consume interaction reach.
        Vector3 playerPosition = _sequence.playerController != null
            ? _sequence.playerController.transform.position : _camera.transform.position;
        Vector3 nearestPoint = hit.collider.ClosestPoint(playerPosition);
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
