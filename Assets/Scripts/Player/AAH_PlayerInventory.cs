using System;
using UnityEngine;

/// <summary>Five-slot inventory for one player. Selection drives the equipped view and HUD.</summary>
public class AAH_PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 5;

    [SerializeField] private AAH_PlayerSanity _sanity;
    [Tooltip("Player movement/look controller; look is paused while the cursor is released.")]
    [SerializeField] private SC_FPSController _controller;
    [SerializeField] private AAH_ItemDefinition[] _startingItems = new AAH_ItemDefinition[0];

    private readonly AAH_ItemDefinition[] _slots = new AAH_ItemDefinition[SlotCount];
    private UnityEngine.InputSystem.InputAction[] _selectActions;
    private UnityEngine.InputSystem.InputAction _useAction, _cursorAction;
    private bool _cursorReleased, _restoreCanMove;

    public int SelectedIndex { get; private set; } = 0;
    public AAH_ItemDefinition SelectedItem => _slots[SelectedIndex];
    public bool CursorReleased => _cursorReleased;
    public event Action InventoryChanged;
    public event Action<int> SelectionChanged;
    public event Action<AAH_ItemDefinition> ItemConsumed;

    public AAH_ItemDefinition GetItem(int index) => index >= 0 && index < SlotCount ? _slots[index] : null;
    public bool HasFreeSlot { get { foreach (var s in _slots) if (s == null) return true; return false; } }

    private void Awake()
    {
        _selectActions = new UnityEngine.InputSystem.InputAction[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            int index = i;
            _selectActions[i] = new UnityEngine.InputSystem.InputAction("SelectSlot" + (i + 1), UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/" + (i + 1));
            _selectActions[i].performed += _ => Select(index);
        }
        _useAction = new UnityEngine.InputSystem.InputAction("UseItem", UnityEngine.InputSystem.InputActionType.Button, "<Mouse>/leftButton");
        _useAction.performed += _ => { if (!_cursorReleased && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f) UseSelected(); };
        _cursorAction = new UnityEngine.InputSystem.InputAction("ReleaseCursor", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/leftAlt");
        _cursorAction.started += _ => SetCursorReleased(true);
        _cursorAction.canceled += _ => SetCursorReleased(false);
        foreach (var item in _startingItems) if (item != null) TryAdd(item);
    }

    private void OnEnable()
    {
        foreach (var a in _selectActions) a.Enable();
        _useAction.Enable(); _cursorAction.Enable();
    }
    private void OnDisable()
    {
        foreach (var a in _selectActions) a.Disable();
        _useAction.Disable(); _cursorAction.Disable();
        SetCursorReleased(false);
    }
    private void OnDestroy()
    {
        foreach (var a in _selectActions) a.Dispose();
        _useAction.Dispose(); _cursorAction.Dispose();
    }

    /// <summary>Holding Left Alt frees the cursor so slots can be clicked; look/move pause meanwhile.</summary>
    private void SetCursorReleased(bool released)
    {
        if (released == _cursorReleased) return;
        if (released)
        {
            // Respect modal UIs that already took control of the cursor.
            if (_controller == null || !_controller.canMove || Cursor.lockState != CursorLockMode.Locked) return;
            _cursorReleased = true; _restoreCanMove = true;
            _controller.canMove = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        else
        {
            _cursorReleased = false;
            if (!_restoreCanMove) return;
            _restoreCanMove = false;
            if (_controller != null) _controller.canMove = true;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
    }

    public void Select(int index)
    {
        if (index < 0 || index >= SlotCount || index == SelectedIndex) return;
        SelectedIndex = index;
        SelectionChanged?.Invoke(index);
    }

    /// <summary>Adds to the first empty slot. Returns false when full.</summary>
    public bool TryAdd(AAH_ItemDefinition item)
    {
        if (item == null) return false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i] != null) continue;
            _slots[i] = item;
            InventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public AAH_ItemDefinition RemoveAt(int index)
    {
        var item = GetItem(index);
        if (item == null) return null;
        _slots[index] = null;
        InventoryChanged?.Invoke();
        return item;
    }

    /// <summary>Consumes the selected item. Not consumed when it has no effect (e.g. sanity already full).</summary>
    public bool UseSelected()
    {
        var item = SelectedItem;
        if (item == null || !item.Consumable) return false;
        if (item.SanityRestore > 0)
        {
            if (_sanity == null || _sanity.ApplyChange(item.SanityRestore) <= 0) return false;
        }
        RemoveAt(SelectedIndex);
        ItemConsumed?.Invoke(item);
        return true;
    }
}
