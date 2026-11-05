using UnityEngine;

public class AAH_CoffeeMachine : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Collider _machineCollider;
    [SerializeField] private AAH_CheckInSequence _checkIn;
    [SerializeField, Min(0.1f)] private float _interactionDistance = 5f;
    [SerializeField, Min(0.1f)] private float _holdDuration = 1f;
    [SerializeField, Min(0.1f)] private float _brewDuration = 180f;
    [Header("Display")]
    [SerializeField] private RectTransform _statusPanel;
    [SerializeField] private TMPro.TMP_Text _statusText;
    [SerializeField] private TMPro.TMP_Text _interactionPrompt;
    [SerializeField] private UnityEngine.UI.Image _progressFill;
    [Header("Inventory")]
    [SerializeField] private AAH_PlayerInventory _inventory;
    [SerializeField] private AAH_ItemDefinition _coffeeItem;
    private UnityEngine.InputSystem.InputAction _takeCoffee;
    private float _holdElapsed;
    private float _inventoryFullTimer;
    private int _lastSecond = -1;
    public bool IsReady { get; private set; } = true;
    public bool IsFocused { get; private set; }
    public float RemainingSeconds { get; private set; }
    public float HoldProgress => Mathf.Clamp01(_holdElapsed / _holdDuration);
    public int CupsTaken { get; private set; }
    private void Awake()
    {
        _takeCoffee = new UnityEngine.InputSystem.InputAction("TakeCoffee", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        _takeCoffee.started += HandleHoldStarted;
        _takeCoffee.canceled += HandleHoldCanceled;
        UpdateStatus();
    }
    private void OnEnable() { _takeCoffee?.Enable(); }
    private void HandleHoldStarted(UnityEngine.InputSystem.InputAction.CallbackContext context) { _holdElapsed = 0f; }
    private void HandleHoldCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context) { _holdElapsed = 0f; }
    private bool CanFocus()
    {
        if (_playerCamera == null || _machineCollider == null || Time.timeScale <= 0f || (_checkIn != null && _checkIn.Busy)) return false;
        RaycastHit hit;
        return Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hit, _interactionDistance, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider == _machineCollider;
    }
    private void Update()
    {
        IsFocused = CanFocus();
        if (_inventoryFullTimer > 0f) _inventoryFullTimer -= Time.deltaTime;
        if (!IsReady)
        {
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            if (RemainingSeconds <= 0f) { IsReady = true; _holdElapsed = 0f; }
        }
        if (IsReady && IsFocused && _takeCoffee.IsPressed())
        {
            _holdElapsed += Time.deltaTime;
            if (_holdElapsed >= _holdDuration) TakeCoffee();
        }
        else _holdElapsed = 0f;
        if (_interactionPrompt != null)
        {
            _interactionPrompt.gameObject.SetActive(IsFocused);
            if (IsFocused)
            {
                if (_inventoryFullTimer > 0f) _interactionPrompt.text = "Túi đồ đã đầy (5/5)";
                else if (IsReady) _interactionPrompt.SetText("Giữ [E] lấy coffee  {0:0}%", HoldProgress * 100f);
                else _interactionPrompt.SetText("Đang pha coffee — còn {0:0}s", Mathf.CeilToInt(RemainingSeconds));
            }
        }
        if (_progressFill != null) _progressFill.fillAmount = IsReady ? HoldProgress : 1f - RemainingSeconds / _brewDuration;
        int second = Mathf.CeilToInt(RemainingSeconds);
        if (second != _lastSecond) UpdateStatus();
    }
    private void LateUpdate()
    {
        if (_statusPanel != null && _playerCamera != null)
            _statusPanel.rotation = Quaternion.LookRotation(_statusPanel.position - _playerCamera.transform.position, Vector3.up);
    }
    private void TakeCoffee()
    {
        if (!IsReady) return;
        _holdElapsed = 0f;
        // Only start brewing when the cup actually went into the inventory.
        if (_inventory == null || !_inventory.TryAdd(_coffeeItem)) { _inventoryFullTimer = 2f; return; }
        CupsTaken++;
        IsReady = false; RemainingSeconds = _brewDuration;
        UpdateStatus();
    }
    private void UpdateStatus()
    {
        _lastSecond = Mathf.CeilToInt(RemainingSeconds);
        if (_statusText == null) return;
        if (IsReady) _statusText.text = "coffee: <color=#39FF14>ready</color>";
        else _statusText.SetText("coffee: brewing {0:0}s", _lastSecond);
    }
    private void OnDisable()
    {
        _takeCoffee?.Disable(); _holdElapsed = 0f;
        if (_interactionPrompt != null) _interactionPrompt.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (_takeCoffee == null) return;
        _takeCoffee.started -= HandleHoldStarted; _takeCoffee.canceled -= HandleHoldCanceled;
        _takeCoffee.Dispose();
    }
}
