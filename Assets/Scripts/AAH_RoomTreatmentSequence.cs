using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AAH_RoomTreatmentSequence : MonoBehaviour
{
    public enum TreatmentStage { WaitingForBed, Patient, Scan, Computer, Analyzing, Completed, CollectingMedicine, Treating, Discharged }
    [Header("Room integration")]
    [SerializeField, Min(1)] private int _roomNumber = 1;
    [Tooltip("Leave empty to accept any patient assigned to this room. Room 1 retains PAT-001.")]
    [SerializeField] private string _patientId = "PAT-001";
    [SerializeField] private AAH_CheckInSequence _checkIn;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _player;
    [SerializeField] private Renderer _scan;
    [SerializeField] private Collider _scanScreenCollider;
    [SerializeField] private Renderer _computer;
    [SerializeField] private Collider _computerScreenCollider;
    [SerializeField] private Material _outlineMaterial;
    [SerializeField, Min(0.1f)] private float _interactionDistance = 5f;
    [SerializeField, Min(0.1f)] private float _rayDistance = 12f;
    [Header("Analysis display")]
    [SerializeField, Min(0.1f)] private float _analysisDuration = 5f;
    [SerializeField] private UnityEngine.UI.Image _progressFill;
    [SerializeField] private TMPro.TMP_Text _computerStatus;
    [SerializeField] private TMPro.TMP_Text _medicineText;
    [SerializeField] private TMPro.TMP_Text _prompt;
    // Generated from the current patient's illness, never serialized per room.
    private string[] _medicines = System.Array.Empty<string>();
    [System.Serializable]
    private class MedicineDisplayEntry
    {
        public AAH_PatientActor.IllnessType illness;
        public string aliases;
        public string label;
        public Texture2D image;
    }
    [Header("TV medicine images")]
    [Tooltip("Rooms 2–5 read the canonical catalog on Room 1; screen references remain local.")]
    [SerializeField] private AAH_RoomTreatmentSequence _medicineCatalogSource;
    [SerializeField] private MedicineDisplayEntry[] _medicineCatalog;
    private MedicineDisplayEntry[] MedicineCatalog => _medicineCatalogSource != null && _medicineCatalogSource != this
        ? _medicineCatalogSource._medicineCatalog : _medicineCatalog;
    [SerializeField] private UnityEngine.UI.RawImage[] _medicineImages;
    [SerializeField] private TMPro.TMP_Text[] _medicineLabels;
    private AAH_PatientActor _patient;
    private Collider _patientCollider;
    private UnityEngine.InputSystem.InputAction _interact;
    private readonly List<GameObject> _patientOutline = new List<GameObject>();
    private readonly List<GameObject> _scanOutline = new List<GameObject>();
    private readonly List<GameObject> _computerOutline = new List<GameObject>();
    private Coroutine _analysis;
    public TreatmentStage Stage { get; private set; }
    public float Progress { get; private set; }
    public bool IsFocused { get; private set; }
    public AAH_PatientActor CurrentPatient => _patient;
    public event System.Action<AAH_PatientActor> PatientRecovered;
    public event System.Action<AAH_PatientActor> ExaminationStarted;

    private void Awake()
    {
        AAH_InteractionHints.Center(_prompt);
        _interact = new UnityEngine.InputSystem.InputAction("Room" + _roomNumber.ToString("00") + "Treatment", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
        _interact.performed += HandleInteract;
        BuildOutline(new[] { _scan }, _scanOutline);
        BuildOutline(new[] { _computer }, _computerOutline);
        Stage = TreatmentStage.WaitingForBed;
        if (_progressFill != null) _progressFill.fillAmount = 0f;
        if (_computerStatus != null) _computerStatus.text = "CHỜ PHÂN TÍCH";
        if (_medicineText != null) _medicineText.text = "PHÒNG " + _roomNumber.ToString("00") + "\nCHỜ KẾT QUẢ PHÂN TÍCH";
        ShowMedicineImages(false);
        InitializeDelivery();
    }
    private void OnEnable()
    {
        _interact?.Enable();
        if (_checkIn != null) _checkIn.OnPatientCheckedIn += HandleCheckedIn;
    }
    private void HandleCheckedIn(string patientName, int roomNumber)
    {
        if (roomNumber != _roomNumber || _checkIn == null) return;
        var actor = _checkIn.CurrentPatient;
        if (actor == null || actor == _patient || ((AAH_GameManager.Instance == null || AAH_GameManager.Instance.currentDay != 2) && !string.IsNullOrEmpty(_patientId) && actor.PatientId != _patientId)) return;
        if (Stage != TreatmentStage.WaitingForBed && Stage != TreatmentStage.Discharged) return;
        ResetPatientDelivery();
        foreach (var outline in _patientOutline) if (outline != null) Destroy(outline);
        _patientOutline.Clear();
        if (_patientCollider != null) Destroy(_patientCollider.gameObject);
        _patientCollider = null;
        _patient = actor;
        _medicines = System.Array.Empty<string>();
        Progress = 0f; IsFocused = false; Stage = TreatmentStage.WaitingForBed;
        SetOutline(_scanOutline, false); SetOutline(_computerOutline, false);
        ShowMedicineImages(false);
        if (_progressFill != null) _progressFill.fillAmount = 0f;
        if (_computerStatus != null) _computerStatus.text = "CHỜ PHÂN TÍCH";
        if (_medicineText != null) _medicineText.text = actor.DisplayName + " | " + actor.PatientId + "\nCHỜ KẾT QUẢ PHÂN TÍCH";
        if (_prompt != null) _prompt.text = "";
    }
    private void Update()
    {
        if (_patient != null && Stage == TreatmentStage.WaitingForBed && _patient.State == AAH_PatientActor.PatientState.AtBed && _patient.RoomNumber == _roomNumber)
            PreparePatient();
        IsFocused = SelectTarget();
        SetOutline(_patientOutline, Stage == TreatmentStage.Patient && IsFocused);
        SetOutline(_scanOutline, Stage == TreatmentStage.Scan);
        SetOutline(_computerOutline, Stage == TreatmentStage.Computer);
        if (_prompt != null)
            _prompt.text = IsFocused ? (Stage == TreatmentStage.Patient ? "[E] Khám bệnh nhân" : Stage == TreatmentStage.Scan ? "[E] Phân tích bệnh" : "[E] Phân tích thuốc") : "";
        UpdateDelivery();
    }
    public void RemoveDeceased(AAH_PatientActor actor)
    {
        if (_patient != actor) return;
        if (_analysis != null) { StopCoroutine(_analysis); _analysis = null; }
        ResetPatientDelivery();
        foreach (var outline in _patientOutline) if (outline != null) Destroy(outline);
        _patientOutline.Clear();
        if (_patientCollider != null) { _patientCollider.enabled = false; Destroy(_patientCollider.gameObject); }
        _patientCollider = null;
        _patient = null;
        Progress = 0f;
        IsFocused = false;
        Stage = TreatmentStage.WaitingForBed;
        SetOutline(_scanOutline, false);
        SetOutline(_computerOutline, false);
        ShowMedicineImages(false);
        if (_progressFill != null) _progressFill.fillAmount = 0f;
        if (_computerStatus != null) _computerStatus.text = "CHỜ PHÂN TÍCH";
        if (_medicineText != null) _medicineText.text = "CHỜ BỆNH NHÂN";
        if (_prompt != null) _prompt.text = "";
    }

    private void PreparePatient()
    {
        // Actor root remains at the navigation endpoint; imported visual moves onto the bed.
        // Fit the selection volume to the actual lying patient's world bounds, not the actor pivot.
        var bounds = _patient.VisualBounds;
        var proxy = new GameObject("Room01_PatientInteraction");
        proxy.transform.SetParent(_patient.transform, true);
        proxy.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
        var box = proxy.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = bounds.size + Vector3.one * 0.12f;
        _patientCollider = box;
        BuildOutline(_patient.GetComponentsInChildren<Renderer>(true), _patientOutline);
        Stage = TreatmentStage.Patient;
    }
    private bool SelectTarget()
    {
        if (Stage == TreatmentStage.CollectingMedicine) return SelectDeliveryTarget();
        if (_playerCamera == null || _patient == null || _patient.State != AAH_PatientActor.PatientState.AtBed || Time.timeScale <= 0f
            || (_checkIn != null && _checkIn.BlocksWorldInteraction) || Stage < TreatmentStage.Patient || Stage > TreatmentStage.Computer) return false;
        RaycastHit hit;
        if (!Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hit, _rayDistance, ~0, QueryTriggerInteraction.Collide)) return false;
        bool matches = Stage == TreatmentStage.Patient ? hit.collider == _patientCollider
            : Stage == TreatmentStage.Scan ? hit.collider == _scanScreenCollider || (_scan != null && (hit.collider.transform == _scan.transform || hit.collider.transform.IsChildOf(_scan.transform)))
            : hit.collider == _computerScreenCollider || (_computer != null && (hit.collider.transform == _computer.transform || hit.collider.transform.IsChildOf(_computer.transform)));
        if (!matches) return false;
        var position = _player != null ? _player.position : _playerCamera.transform.position;
        return WithinInteractionDistance(hit, position);
    }
    private bool WithinInteractionDistance(RaycastHit hit, Vector3 position)
    {
        Collider collider = hit.collider;
        bool supportsClosestPoint = collider is BoxCollider || collider is SphereCollider ||
            collider is CapsuleCollider || (collider is MeshCollider mesh && mesh.convex);
        var nearest = supportsClosestPoint ? collider.ClosestPoint(position) : hit.point;
        nearest.y = position.y;
        return Vector3.Distance(position, nearest) <= _interactionDistance;
    }
    private void HandleInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (Stage == TreatmentStage.CollectingMedicine) { HandleMedicineInteraction(); return; }
        if (!SelectTarget()) return;
        if (Stage == TreatmentStage.Patient)
        {
            Stage = TreatmentStage.Scan;
            ExaminationStarted?.Invoke(_patient);
        }
        else if (Stage == TreatmentStage.Scan) Stage = TreatmentStage.Computer;
        else if (Stage == TreatmentStage.Computer)
        {
            Stage = TreatmentStage.Analyzing;
            _analysis = StartCoroutine(Analyze());
        }
        SetOutline(_patientOutline, false);
        SetOutline(_scanOutline, Stage == TreatmentStage.Scan);
        SetOutline(_computerOutline, Stage == TreatmentStage.Computer);
        if (_prompt != null) _prompt.text = "";
    }
    private IEnumerator Analyze()
    {
        Progress = 0f; UpdateProgress();
        float elapsed = 0f;
        while (elapsed < _analysisDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Progress = Mathf.Clamp01(elapsed / _analysisDuration); UpdateProgress();
        }
        Progress = 1f; UpdateProgress(); Stage = TreatmentStage.Completed;
        if (_computerStatus != null) _computerStatus.text = "HOÀN TẤT 100%";
        bool hasPrescription = BuildPatientPrescription();
        if (_medicineText != null)
        {
            var text = new System.Text.StringBuilder("THUỐC CHO BỆNH NHÂN\n");
            text.Append(_patient.DisplayName).Append(" | ").Append(_patient.PatientId);
            if (!hasPrescription) text.Append("\nChưa có thuốc cho chẩn đoán này.");
            _medicineText.text = text.ToString();
        }
        ShowMedicineImages(hasPrescription);
        // A missing diagnosis must never silently start recovery with zero medicine.
        if (hasPrescription) BeginMedicineDelivery();
        _analysis = null;
    }
    private bool BuildPatientPrescription()
    {
        _medicines = System.Array.Empty<string>();
        if (_patient == null || _patient.Illness == AAH_PatientActor.IllnessType.Unassigned) return false;
        // The existing eye-drop entry handles both Dry Eyes and Day 1's Eye Pain.
        var illness = _patient.Illness == AAH_PatientActor.IllnessType.EyePain
            ? AAH_PatientActor.IllnessType.DryEyes : _patient.Illness;
        var catalog = MedicineCatalog;
        if (catalog == null) return false;
        foreach (var entry in catalog)
        {
            if (entry == null || entry.illness != illness || entry.image == null || string.IsNullOrWhiteSpace(entry.label)) continue;
            _medicines = new[] { entry.label };
            return true;
        }
        return false;
    }
    private void ShowMedicineImages(bool show)
    {
        if (_medicineImages == null) return;
        int count = 0;
        if (show && _medicines != null)
            foreach (var medicine in _medicines) if (!string.IsNullOrWhiteSpace(medicine)) count++;
        int visibleCount = Mathf.Min(count, _medicineImages.Length);
        int itemIndex = 0;
        for (int i = 0; i < _medicineImages.Length; i++)
        {
            var image = _medicineImages[i];
            if (image == null) continue;
            bool visible = i < visibleCount;
            image.gameObject.SetActive(visible);
            var label = _medicineLabels != null && i < _medicineLabels.Length ? _medicineLabels[i] : null;
            if (label != null) label.gameObject.SetActive(visible);
            if (!visible)
            {
                image.texture = null; image.enabled = false;
                if (label != null) label.text = "";
                continue;
            }
            while (string.IsNullOrWhiteSpace(_medicines[itemIndex])) itemIndex++;
            string medicine = _medicines[itemIndex++];
            MedicineDisplayEntry entry = ResolveMedicine(medicine);
            image.texture = entry != null ? entry.image : null;
            image.enabled = image.texture != null;
            // Center one medicine; arrange longer prescriptions in rows without stretching images.
            int columns = Mathf.Min(visibleCount, 5);
            int rows = Mathf.CeilToInt(visibleCount / (float)columns);
            var canvasRect = image.canvas.transform as RectTransform;
            float width = canvasRect.rect.width * 0.9f;
            float height = canvasRect.rect.height * 0.66f;
            float cellWidth = width / columns;
            float cellHeight = height / rows;
            float imageHeight = cellHeight * 0.72f;
            float aspect = image.texture != null ? image.texture.width / (float)image.texture.height : 0.5f;
            float imageWidth = Mathf.Min(imageHeight * aspect, cellWidth * 0.8f);
            imageHeight = imageWidth / aspect;
            int row = i / columns;
            int rowCount = Mathf.Min(columns, visibleCount - row * columns);
            float x = (i % columns - (rowCount - 1) * 0.5f) * cellWidth;
            float y = height * 0.5f - (row + 0.5f) * cellHeight - canvasRect.rect.height * 0.08f;
            image.rectTransform.sizeDelta = new Vector2(imageWidth, imageHeight);
            image.rectTransform.anchoredPosition = new Vector2(x, y);
            if (label != null)
            {
                label.text = entry != null ? entry.label : medicine;
                label.rectTransform.sizeDelta = new Vector2(cellWidth * 0.94f, cellHeight * 0.18f);
                label.rectTransform.anchoredPosition = new Vector2(x, y - cellHeight * 0.43f);
            }
        }
    }
    private void UpdateProgress()
    {
        if (_progressFill != null) _progressFill.fillAmount = Progress;
        if (_computerStatus != null) _computerStatus.SetText("PHÂN TÍCH THUỐC\n{0:0}%", Progress * 100f);
    }
    private void BuildOutline(Renderer[] sources, List<GameObject> output)
    {
        if (_outlineMaterial == null) return;
        foreach (var source in sources)
        {
            if (source == null) continue;
            Mesh mesh = null;
            var skinned = source as SkinnedMeshRenderer;
            var filter = source.GetComponent<MeshFilter>();
            if (skinned != null) mesh = skinned.sharedMesh;
            else if (filter != null) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            var go = new GameObject("TreatmentOutline");
            go.transform.SetParent(source.transform, false);
            Renderer outline;
            if (skinned != null)
            {
                var renderer = go.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh; renderer.bones = skinned.bones; renderer.rootBone = skinned.rootBone;
                renderer.localBounds = skinned.localBounds; renderer.updateWhenOffscreen = true; outline = renderer;
            }
            else { go.AddComponent<MeshFilter>().sharedMesh = mesh; outline = go.AddComponent<MeshRenderer>(); }
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = _outlineMaterial;
            outline.sharedMaterials = materials;
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; outline.receiveShadows = false;
            go.SetActive(false); output.Add(go);
        }
    }
    private static void SetOutline(List<GameObject> objects, bool visible)
    { foreach (var go in objects) if (go != null && go.activeSelf != visible) go.SetActive(visible); }
    private void OnDisable()
    {
        DisableDelivery();
        _interact?.Disable();
        if (_checkIn != null) _checkIn.OnPatientCheckedIn -= HandleCheckedIn;
        if (_analysis != null) { StopCoroutine(_analysis); _analysis = null; Stage = TreatmentStage.Computer; }
        SetOutline(_patientOutline, false); SetOutline(_scanOutline, false); SetOutline(_computerOutline, false);
        if (_prompt != null) _prompt.text = "";
    }
    private void OnDestroy()
    {
        ClearMedicineItems();
        if (_interact != null) { _interact.performed -= HandleInteract; _interact.Dispose(); }
        foreach (var list in new[] { _patientOutline, _scanOutline, _computerOutline }) foreach (var go in list) if (go != null) Destroy(go);
    }
}
