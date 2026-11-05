using UnityEngine;

/// <summary>Displays the selected item's model under a camera-relative anchor (bottom-right of view).</summary>
public class AAH_EquippedItemView : MonoBehaviour
{
    [SerializeField] private AAH_PlayerInventory _inventory;
    [Tooltip("Child of the player camera, positioned toward the bottom-right of the view.")]
    [SerializeField] private Transform _anchor;
    [Tooltip("Camera that owns the anchor. Used to keep the item at the same screen position for any FOV/aspect.")]
    [SerializeField] private Camera _camera;
    [Tooltip("Viewport position of the item centre (0,0 bottom-left, 1,1 top-right).")]
    [SerializeField] private Vector2 _viewportPosition = new Vector2(0.8f, 0.2f);
    [SerializeField, Min(0.1f)] private float _distance = 0.6f;

    private AAH_ItemDefinition _shownItem;

    private void LateUpdate()
    {
        if (_camera == null || _anchor == null) return;
        // Uses the real projection (physical camera / gate fit / any aspect), not just fieldOfView.
        _anchor.position = _camera.ViewportToWorldPoint(new Vector3(_viewportPosition.x, _viewportPosition.y, _distance));
    }
    private GameObject _instance;
    public GameObject CurrentInstance => _instance;

    private void OnEnable()
    {
        if (_inventory == null) return;
        _inventory.SelectionChanged += HandleSelectionChanged;
        _inventory.InventoryChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (_inventory == null) return;
        _inventory.SelectionChanged -= HandleSelectionChanged;
        _inventory.InventoryChanged -= Refresh;
    }
    private void HandleSelectionChanged(int index) { Refresh(); }

    private void Refresh()
    {
        var item = _inventory != null ? _inventory.SelectedItem : null;
        if (item == _shownItem && (_instance != null || item == null || item.EquippedModel == null)) return;
        if (_instance != null) Destroy(_instance);
        _instance = null; _shownItem = item;
        if (item == null || item.EquippedModel == null || _anchor == null) return;

        _instance = Instantiate(item.EquippedModel, _anchor);
        _instance.name = "Equipped_" + item.ItemId;
        _instance.transform.localRotation = item.EquippedRotation;
        foreach (var c in _instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        var renderers = _instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        foreach (var r in renderers) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (largest > 0f) _instance.transform.localScale *= item.EquippedSize / largest;
        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        _instance.transform.position += _anchor.position - bounds.center;
    }
}
