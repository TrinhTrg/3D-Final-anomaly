using System;
using UnityEngine;

public class AAH_InteractableStep : MonoBehaviour
{
    [Header("Configuration")]
    public string promptMessage = "Nhấn [E] để tương tác";
    public KeyCode interactKey = KeyCode.E;
    public float interactionDistance = 2.5f;
    public Collider targetCollider;
    public Collider[] additionalColliders;
    [Header("Visual Feedback")]
    public GameObject highlightRing;
    private bool isStepActive;
    private bool isCompleted;
    public event Action OnInteracted;
    public bool IsStepActive => isStepActive && !isCompleted && isActiveAndEnabled;
    public void ResetForPatient() { isCompleted = false; isStepActive = false; UpdateRingVisual(); }
    public void SetStepActive(bool active) { isStepActive = active; UpdateRingVisual(); }
    public void MarkCompleted() { isCompleted = true; isStepActive = false; UpdateRingVisual(); }
    public void TriggerInteraction()
    {
        if (IsStepActive && Time.timeScale > 0f) OnInteracted?.Invoke();
    }
    private void UpdateRingVisual() { if (highlightRing != null) highlightRing.SetActive(IsStepActive); }
    private void OnEnable() { UpdateRingVisual(); }
    private void OnDisable() { if (highlightRing != null) highlightRing.SetActive(false); }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, interactionDistance); }
}

