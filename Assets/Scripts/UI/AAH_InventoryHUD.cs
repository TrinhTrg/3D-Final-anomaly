using UnityEngine;

/// <summary>Displays the local player's five inventory slots; clicking a slot selects it.</summary>
public class AAH_InventoryHUD : MonoBehaviour
{
    [System.Serializable]
    private class SlotView
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image frame;
        public UnityEngine.UI.Image icon;
    }

    [SerializeField] private AAH_PlayerInventory _inventory;
    [SerializeField] private SlotView[] _slots = new SlotView[AAH_PlayerInventory.SlotCount];
    [SerializeField] private Color _normalFrame = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private Color _selectedFrame = new Color(0.25f, 0.65f, 1f, 1f);

    private void Awake()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            int index = i;
            if (_slots[i] != null && _slots[i].button != null) _slots[i].button.onClick.AddListener(() => { if (_inventory != null) _inventory.Select(index); });
        }
    }
    private void OnEnable()
    {
        if (_inventory == null) return;
        _inventory.InventoryChanged += Refresh;
        _inventory.SelectionChanged += HandleSelectionChanged;
        Refresh();
    }
    private void OnDisable()
    {
        if (_inventory == null) return;
        _inventory.InventoryChanged -= Refresh;
        _inventory.SelectionChanged -= HandleSelectionChanged;
    }
    private void HandleSelectionChanged(int index) { Refresh(); }

    private void Refresh()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot == null) continue;
            var item = _inventory.GetItem(i);
            if (slot.icon != null)
            {
                slot.icon.sprite = item != null ? item.Icon : null;
                slot.icon.enabled = item != null && item.Icon != null;
            }
            if (slot.frame != null) slot.frame.color = i == _inventory.SelectedIndex ? _selectedFrame : _normalFrame;
        }
    }
}
