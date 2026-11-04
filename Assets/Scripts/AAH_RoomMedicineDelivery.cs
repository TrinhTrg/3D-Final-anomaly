using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AAH_RoomTreatmentSequence
{
    [System.Serializable]
    private class MedicineStock
    {
        public Texture2D image;
        public GameObject[] bottles;
    }
    [Header("Medicine delivery")]
    [SerializeField] private MedicineStock[] _medicineStocks;
    [SerializeField] private RectTransform _guideArrow;
    [SerializeField] private TMPro.TMP_Text _guideText;
    [SerializeField] private Transform _medicineHoldPoint;
    [SerializeField] private GameObject _treatmentPanel;
    [SerializeField] private UnityEngine.UI.Image _treatmentFill;
    [SerializeField] private TMPro.TMP_Text _treatmentStatus;
    [SerializeField] private Transform _hospitalExit;
    [SerializeField, Min(0.1f)] private float _treatmentDuration = 10f;
    private readonly List<MedicineOrder> _orders = new List<MedicineOrder>();
    private readonly List<GameObject> _pickupProxies = new List<GameObject>();
    private int _focusedOrder = -1;
    private GameObject _focusedBottle;
    private Coroutine _recovery;
    public int CollectedMedicineCount { get; private set; }
    public int DeliveredMedicineCount { get; private set; }
    public float TreatmentProgress { get; private set; }
    private class MedicineOrder
    {
        public string label;
        public MedicineStock stock;
        public bool collected;
        public bool delivered;
        public GameObject source;
        public GameObject held;
    }
    private MedicineDisplayEntry ResolveMedicine(string medicine)
    {
        if (_medicineCatalog == null || string.IsNullOrWhiteSpace(medicine)) return null;
        foreach (var entry in _medicineCatalog)
        {
            if (entry == null || string.IsNullOrEmpty(entry.aliases)) continue;
            foreach (var alias in entry.aliases.Split('|'))
                if (string.Equals(alias.Trim(), medicine.Trim(), System.StringComparison.OrdinalIgnoreCase)) return entry;
        }
        return null;
    }
    private void InitializeDelivery()
    {
        if (_guideArrow != null) _guideArrow.gameObject.SetActive(false);
        if (_guideText != null) _guideText.text = "";
        if (_treatmentPanel != null) _treatmentPanel.SetActive(false);
        if (_medicineStocks == null) return;
        foreach (var stock in _medicineStocks)
        {
            if (stock == null || stock.bottles == null) continue;
            foreach (var bottle in stock.bottles)
            {
                if (bottle == null) continue;
                var renderers = bottle.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var proxy = new GameObject("MedicinePickup");
                proxy.transform.SetParent(bottle.transform, false);
                proxy.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
                var collider = proxy.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                var scale = proxy.transform.lossyScale;
                collider.size = new Vector3((bounds.size.x + 0.12f) / Mathf.Abs(scale.x), (bounds.size.y + 0.12f) / Mathf.Abs(scale.y), (bounds.size.z + 0.12f) / Mathf.Abs(scale.z));
                _pickupProxies.Add(proxy);
            }
        }
    }
    private void BeginMedicineDelivery()
    {
        _orders.Clear(); CollectedMedicineCount = 0; DeliveredMedicineCount = 0;
        if (_medicines != null)
            foreach (var medicine in _medicines)
            {
                if (string.IsNullOrWhiteSpace(medicine)) continue;
                var entry = ResolveMedicine(medicine);
                MedicineStock stock = null;
                if (entry != null && _medicineStocks != null)
                    foreach (var candidate in _medicineStocks) if (candidate != null && candidate.image == entry.image) { stock = candidate; break; }
                if (stock == null || stock.bottles == null || stock.bottles.Length == 0)
                {
                    Debug.LogError("No shelf stock configured for prescribed medicine: " + medicine, this);
                    if (_guideText != null) _guideText.text = "Thiếu thuốc trên kệ: " + medicine;
                    return;
                }
                _orders.Add(new MedicineOrder { label = entry.label, stock = stock });
            }
        Stage = TreatmentStage.CollectingMedicine;
        if (_orders.Count == 0) StartRecovery();
    }
    private bool SelectDeliveryTarget()
    {
        _focusedOrder = -1; _focusedBottle = null;
        if (_playerCamera == null || _patient == null || _patient.State != AAH_PatientActor.PatientState.AtBed || Time.timeScale <= 0f || (_checkIn != null && _checkIn.Busy)) return false;
        RaycastHit hit;
        if (!Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hit, _rayDistance, ~0, QueryTriggerInteraction.Collide)) return false;
        var position = _player != null ? _player.position : _playerCamera.transform.position;
        var nearest = hit.collider.ClosestPoint(position); nearest.y = position.y;
        if (Vector3.Distance(position, nearest) > _interactionDistance) return false;
        if (hit.collider == _patientCollider) return CollectedMedicineCount > DeliveredMedicineCount;
        for (int i = 0; i < _orders.Count; i++)
        {
            var order = _orders[i];
            if (order.collected) continue;
            foreach (var bottle in order.stock.bottles)
                if (bottle != null && bottle.activeInHierarchy && (hit.transform == bottle.transform || hit.transform.IsChildOf(bottle.transform)))
                { _focusedOrder = i; _focusedBottle = bottle; return true; }
        }
        return false;
    }
    private void HandleMedicineInteraction()
    {
        if (!SelectDeliveryTarget()) return;
        if (_focusedOrder >= 0)
        {
            var order = _orders[_focusedOrder];
            order.source = _focusedBottle;
            if (_medicineHoldPoint != null)
            {
                var held = Instantiate(_focusedBottle, _medicineHoldPoint);
                held.name = "HeldMedicine_" + order.label;
                foreach (var collider in held.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var renderers = held.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                held.transform.localScale *= 0.45f / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                held.transform.position += _medicineHoldPoint.position - bounds.center;
                held.transform.localPosition += Vector3.right * CollectedMedicineCount * 0.22f;
                order.held = held;
            }
            order.source.SetActive(false); order.collected = true; CollectedMedicineCount++;
        }
        else
        {
            foreach (var order in _orders)
                if (order.collected && !order.delivered)
                {
                    order.delivered = true; DeliveredMedicineCount++;
                    if (order.held != null) Destroy(order.held);
                    break;
                }
            if (DeliveredMedicineCount == _orders.Count) StartRecovery();
        }
    }
    private void UpdateDelivery()
    {
        bool active = Stage == TreatmentStage.CollectingMedicine;
        if (_guideArrow != null) _guideArrow.gameObject.SetActive(active);
        if (!active) return;
        Vector3 target = _patient.VisualBounds.center;
        string instruction = "Về phòng 01, đặt thuốc cho bệnh nhân";
        foreach (var order in _orders)
        {
            if (order.collected) continue;
            float best = float.PositiveInfinity;
            foreach (var bottle in order.stock.bottles)
            {
                if (bottle == null || !bottle.activeInHierarchy) continue;
                float distance = (_player.position - bottle.transform.position).sqrMagnitude;
                if (distance < best) { best = distance; target = bottle.transform.position; }
            }
            instruction = "Lấy " + order.label + " trên kệ thuốc";
            break;
        }
        if (_guideArrow != null)
        {
            var direction = target - _playerCamera.transform.position;
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(_playerCamera.transform.forward, Vector3.up), Vector3.ProjectOnPlane(direction, Vector3.up), Vector3.up);
            _guideArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
        if (_guideText != null) _guideText.text = instruction + "\nĐã lấy: " + CollectedMedicineCount + "/" + _orders.Count + " | Đã đặt: " + DeliveredMedicineCount + "/" + _orders.Count;
        if (_prompt != null) _prompt.text = IsFocused ? (_focusedOrder >= 0 ? "[E] Lấy " + _orders[_focusedOrder].label : "[E] Đặt thuốc / Chữa bệnh") : "";
        SetOutline(_patientOutline, IsFocused && _focusedOrder < 0 && CollectedMedicineCount > DeliveredMedicineCount);
    }
    private void StartRecovery()
    {
        Stage = TreatmentStage.Treating;
        ShowMedicineImages(false);
        if (_medicineText != null) _medicineText.text = "";
        if (_guideArrow != null) _guideArrow.gameObject.SetActive(false);
        if (_guideText != null) _guideText.text = "";
        SetOutline(_patientOutline, false);
        _recovery = StartCoroutine(RecoveryRoutine());
    }
    private IEnumerator RecoveryRoutine()
    {
        TreatmentProgress = 0f;
        if (_treatmentPanel != null) _treatmentPanel.SetActive(true);
        float elapsed = 0f;
        while (elapsed < _treatmentDuration)
        {
            TreatmentProgress = Mathf.Clamp01(elapsed / _treatmentDuration);
            if (_treatmentFill != null) _treatmentFill.fillAmount = TreatmentProgress;
            if (_treatmentStatus != null) _treatmentStatus.SetText("ĐANG ĐIỀU TRỊ\n{0:0}%", TreatmentProgress * 100f);
            yield return null; elapsed += Time.deltaTime;
        }
        TreatmentProgress = 1f;
        if (_treatmentFill != null) _treatmentFill.fillAmount = 1f;
        if (_treatmentStatus != null) _treatmentStatus.text = "HOÀN TẤT\n100%";
        yield return null;
        if (_treatmentPanel != null) _treatmentPanel.SetActive(false);
        if (_patientCollider != null) _patientCollider.enabled = false;
        Stage = TreatmentStage.Discharged;
        if (_patient != null) _patient.LeaveHospital(_hospitalExit);
        _recovery = null;
    }
    private void DisableDelivery()
    {
        if (_guideArrow != null) _guideArrow.gameObject.SetActive(false);
        if (_guideText != null) _guideText.text = "";
        if (_recovery != null) { StopCoroutine(_recovery); _recovery = null; }
    }
}
