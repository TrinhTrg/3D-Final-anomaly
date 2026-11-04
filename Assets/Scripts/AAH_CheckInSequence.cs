using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý toàn bộ quy trình Check-in bệnh nhân tại Quầy Lễ Tân:
/// 1. Chuông gọi bệnh nhân (Bell) -> NPC tiến vào bấm chuông thoại "I'm Mai Yamada, I'm here for my appointment..."
/// 2. Bảng kẹp giấy (Clipboard): Mở UI giấy tờ, ký xác nhận.
/// 3. Máy ảnh trên chân máy (Camera): Flash, chụp ảnh bệnh nhân, ảnh Polaroid hiện dần trong 3s rồi cố định trên HUD.
/// 4. Máy tính (Computer): Tiến trình "REGISTERING..." chạy thanh progress bar.
/// 5. Máy in (Printer): Nhấn E in ra thẻ/giấy chỉ định phòng (tổng 6 phòng).
/// 6. Hoàn tất: Bắn sự kiện OnPatientCheckedIn -> Bệnh nhân chuyển sang Leaving.
/// </summary>
public class AAH_CheckInSequence : MonoBehaviour
{
    public static AAH_CheckInSequence Instance { get; private set; }

    public enum CheckInStep
    {
        Idle_WaitingBell,
        PatientApproaching,
        PatientDialogue,
        Step1_Clipboard,
        Step2_CameraTripod,
        Step3_ComputerRegister,
        Step4_PrinterRoomCard,
        Completed_Leaving
    }

    [Header("Current State")]
    public CheckInStep currentStep = CheckInStep.Idle_WaitingBell;

    [Header("Interactable Steps (Quầy Lễ Tân)")]
    public AAH_InteractableStep stepBell;
    public AAH_InteractableStep stepClipboard;
    public AAH_InteractableStep stepCameraTripod;
    public AAH_InteractableStep stepComputer;
    public AAH_InteractableStep stepPrinter;

    [Header("Patient Info")]
    public string patientName = "Mai Yamada";
    public string patientDialogue = "I'm Mai Yamada. I'm here for my appointment...";
    public Transform patientStandPoint;
    public Transform patientSpawnPoint;
    public Transform patientExitPoint;
    public GameObject activePatientObject;

    [Header("UI - Prompt & Dialogue")]
    public TextMeshProUGUI txtInteractionPrompt;
    public GameObject panelDialogue;
    public TextMeshProUGUI txtDialogueContent;

    [Header("UI - Step 1: Clipboard")]
    public GameObject panelClipboard;
    public TextMeshProUGUI txtClipboardPatientName;
    public Button btnSignClipboard;

    [Header("UI - Step 2: Camera & Polaroid")]
    public GameObject panelFlash;          // Trắng loá màn hình
    public GameObject cardPolaroidHUD;     // Thẻ ảnh Polaroid cố định trên HUD
    public Image imgPolaroidPhoto;        // Ảnh bên trong Polaroid
    public TextMeshProUGUI txtPolaroidName;
    public Camera photoCaptureCamera;     // Camera chụp ảnh bệnh nhân (RenderTexture)

    [Header("UI - Step 3: Computer")]
    public GameObject panelComputerRegister;
    public Slider sliderRegisterProgress;
    public TextMeshProUGUI txtRegisterStatus;
    [Header("Reception monitor")]
    [SerializeField] private UnityEngine.UI.Image _receptionProgress;
    [SerializeField] private TMPro.TMP_Text _receptionStatus;

    [Header("UI - Step 4: Printer")]
    public GameObject panelPrinterCard;
    public TextMeshProUGUI txtAssignedRoom;
    public GameObject worldPrintedCardObject; // Object tờ giấy chìa ra ở khe máy in

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip soundBell;
    public AudioClip soundCameraShutter;
    public AudioClip soundPrinter;
    public AudioClip soundComputerBeep;

    // Events
    public event Action<string, int> OnPatientCheckedIn;

    [Header("Patient and room integration")]
    public AAH_RoomRegistry roomRegistry;
    public SC_FPSController playerController;
    public Transform bellVisual;
    public TMPro.TextMeshProUGUI txtQueueSummary;
    [Min(0.1f)] public float dialogueDuration = 4f;
    [Min(0.1f)] public float photoDevelopDuration = 3f;
    [Min(0.1f)] public float registerDuration = 2.5f;
    [Min(0.1f)] public float printDuration = 1.5f;
    private AAH_PatientActor _patient;
    private AAH_RoomRegistry.RoomSlot _reservedRoom;
    private bool _busy, _modal, _sentCompletion, _savedCanMove;
    private CursorLockMode _savedCursor;
    private bool _savedVisible;
    private Texture2D _photoTexture;
    private Sprite _photoSprite;
    private Coroutine _operation;
    private Vector3 _clipboardDeskPosition;
    public bool Busy => _busy || _modal;
    public bool HasPatient => _patient != null;
    public AAH_PatientActor CurrentPatient => _patient;
    public bool ReadyForPatient => isActiveAndEnabled && _patient == null && roomRegistry != null && roomRegistry.FreeCount > 0;

    void Awake()
    {
        Instance = this;
        if (stepClipboard != null) _clipboardDeskPosition = stepClipboard.transform.position;
    }
    void OnEnable()
    {
        if (stepClipboard != null) stepClipboard.OnInteracted += HandleClipboardInteracted;
        if (stepCameraTripod != null) stepCameraTripod.OnInteracted += HandleCameraInteracted;
        if (stepComputer != null) stepComputer.OnInteracted += HandleComputerInteracted;
        if (stepPrinter != null) stepPrinter.OnInteracted += HandlePrinterInteracted;
        if (btnSignClipboard != null) btnSignClipboard.onClick.AddListener(ConfirmSignClipboard);
    }
    void Start() { HidePanels(); ResetSteps(); SetStep(CheckInStep.Idle_WaitingBell); }
    private void HidePanels()
    {
        foreach (var panel in new[] { panelDialogue, panelClipboard, panelFlash, panelComputerRegister, panelPrinterCard, worldPrintedCardObject })
            if (panel != null) panel.SetActive(false);
        if (stepClipboard != null) stepClipboard.gameObject.SetActive(false);
    }
    private void ResetSteps()
    {
        if (_receptionProgress != null) _receptionProgress.fillAmount = 0f;
        if (_receptionStatus != null) _receptionStatus.text = "CHỜ ĐĂNG KÝ\n0%";
        foreach (var step in new[] { stepBell, stepClipboard, stepCameraTripod, stepComputer, stepPrinter })
            if (step != null) step.ResetForPatient();
    }
    public bool BeginPatient(AAH_PatientActor patient)
    {
        if (!ReadyForPatient || patient == null) return false;
        _reservedRoom = roomRegistry.Reserve(patient.PatientId);
        if (_reservedRoom == null) return false;
        _patient = patient; activePatientObject = patient.gameObject;
        patientName = patient.DisplayName; patientDialogue = patient.Dialogue; _sentCompletion = false;
        ResetSteps(); HidePanels(); SetStep(CheckInStep.PatientApproaching);
        patient.ArrivedAtDesk += HandlePatientArrived; patient.NavigationFailed += HandleNavigationFailed;
        return true;
    }
    public void SetStep(CheckInStep step)
    {
        currentStep = step;
        if (stepBell != null) stepBell.SetStepActive(false); // NPC, not player, rings the bell.
        if (stepClipboard != null) stepClipboard.SetStepActive(!Busy && step == CheckInStep.Step1_Clipboard);
        if (stepCameraTripod != null) stepCameraTripod.SetStepActive(!Busy && step == CheckInStep.Step2_CameraTripod);
        if (stepComputer != null) stepComputer.SetStepActive(!Busy && step == CheckInStep.Step3_ComputerRegister);
        if (stepPrinter != null) stepPrinter.SetStepActive(!Busy && step == CheckInStep.Step4_PrinterRoomCard);
    }
    private void Update()
    {
        if (txtQueueSummary != null && roomRegistry != null)
            txtQueueSummary.text = roomRegistry.FreeCount == 0 && _patient == null ? "Đang chờ phòng trống (6/6)" : "Tiếp nhận: " + (_patient != null ? patientName : "Chờ bệnh nhân") + " | Phòng trống: " + roomRegistry.FreeCount + "/6";
        if (_patient != null && _sentCompletion && _patient.IsAwayFrom(patientStandPoint.position)) ClearDesk();
    }
    private void HandlePatientArrived(AAH_PatientActor patient)
    {
        if (patient != _patient || currentStep != CheckInStep.PatientApproaching) return;
        _busy = true; _operation = StartCoroutine(PatientDialogueRoutine());
    }
    private IEnumerator PatientDialogueRoutine()
    {
        PlaySound(soundBell);
        if (bellVisual != null)
        {
            Vector3 scale = bellVisual.localScale;
            bellVisual.localScale = new Vector3(scale.x, scale.y * 0.85f, scale.z);
            yield return new WaitForSeconds(0.15f); bellVisual.localScale = scale;
        }
        _patient.SetDeskState(AAH_PatientActor.PatientState.Dialogue); SetStep(CheckInStep.PatientDialogue);
        if (panelDialogue != null) panelDialogue.SetActive(true);
        if (txtDialogueContent != null) txtDialogueContent.text = "<b>" + patientName + "</b>\nI'm " + patientName + ". " + patientDialogue;
        yield return new WaitForSeconds(dialogueDuration);
        if (panelDialogue != null) panelDialogue.SetActive(false);
        _patient.SetDeskState(AAH_PatientActor.PatientState.CheckingIn);
        if (stepClipboard != null)
        {
            stepClipboard.gameObject.SetActive(true);
            Vector3 handoverStart = _patient.transform.position + Vector3.up * 3f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                stepClipboard.transform.position = Vector3.Lerp(handoverStart, _clipboardDeskPosition, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            stepClipboard.transform.position = _clipboardDeskPosition;
        }
        _busy = false; SetStep(CheckInStep.Step1_Clipboard); _operation = null;
    }
    private bool Accept(CheckInStep expected) => !Busy && _patient != null && currentStep == expected;
    private void HandleClipboardInteracted()
    {
        if (!Accept(CheckInStep.Step1_Clipboard)) return;
        if (panelClipboard == null || btnSignClipboard == null) { Debug.LogError("Clipboard UI is not wired.", this); return; }
        _modal = true; SetStep(currentStep); panelClipboard.SetActive(true);
        if (txtClipboardPatientName != null) txtClipboardPatientName.text = "HỒ SƠ TIẾP NHẬN\n\nBệnh nhân: " + patientName + "\nMã hồ sơ: " + _patient.PatientId + "\nLịch hẹn: Khám ban đầu\n\nXem và ký xác nhận hồ sơ.";
        _savedCursor = Cursor.lockState; _savedVisible = Cursor.visible;
        if (playerController != null) { _savedCanMove = playerController.canMove; playerController.canMove = false; }
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    private void RestoreModal()
    {
        if (!_modal) return;
        _modal = false; if (panelClipboard != null) panelClipboard.SetActive(false);
        if (playerController != null) playerController.canMove = _savedCanMove;
        Cursor.lockState = _savedCursor; Cursor.visible = _savedVisible;
    }
    public void CancelClipboard() { RestoreModal(); SetStep(currentStep); }
    public void ConfirmSignClipboard()
    {
        if (!_modal || _patient == null || currentStep != CheckInStep.Step1_Clipboard) return;
        RestoreModal(); stepClipboard.MarkCompleted(); SetStep(CheckInStep.Step2_CameraTripod);
    }
    private void HandleCameraInteracted()
    {
        if (!Accept(CheckInStep.Step2_CameraTripod)) return;
        _busy = true; SetStep(currentStep); _operation = StartCoroutine(CapturePhotoRoutine());
    }
    private IEnumerator CapturePhotoRoutine()
    {
        PlaySound(soundCameraShutter);
        bool captured = false;
        try { CaptureSnapshot(); captured = true; }
        catch (Exception e) { Debug.LogError("Patient photo failed: " + e.Message, this); }
        if (!captured) { _busy = false; SetStep(currentStep); _operation = null; yield break; }
        if (panelFlash != null) { panelFlash.SetActive(true); yield return new WaitForSeconds(0.12f); panelFlash.SetActive(false); }
        cardPolaroidHUD.SetActive(true); txtPolaroidName.text = patientName; imgPolaroidPhoto.color = Color.black;
        float elapsed = 0f;
        while (elapsed < photoDevelopDuration) { elapsed += Time.deltaTime; imgPolaroidPhoto.color = Color.Lerp(Color.black, Color.white, elapsed / photoDevelopDuration); yield return null; }
        imgPolaroidPhoto.color = Color.white; stepCameraTripod.MarkCompleted(); _busy = false;
        SetStep(CheckInStep.Step3_ComputerRegister); _operation = null;
    }
    private void CaptureSnapshot()
    {
        if (photoCaptureCamera == null || imgPolaroidPhoto == null || cardPolaroidHUD == null || txtPolaroidName == null) throw new InvalidOperationException("Photo UI/camera missing");
        var bounds = _patient.VisualBounds;
        photoCaptureCamera.transform.position = bounds.center + patientStandPoint.forward * 4.5f + Vector3.up * 0.6f;
        _patient.FacePhotoCamera(photoCaptureCamera.transform.position);
        bounds = _patient.VisualBounds;
        photoCaptureCamera.transform.LookAt(bounds.center + Vector3.up * 0.7f);
        photoCaptureCamera.fieldOfView = 65f;
        var oldTarget = photoCaptureCamera.targetTexture; var oldActive = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(512, 512, 24);
        Texture2D texture = null;
        try
        {
            photoCaptureCamera.targetTexture = rt; photoCaptureCamera.Render(); RenderTexture.active = rt;
            texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
        }
        catch { if (texture != null) Destroy(texture); throw; }
        finally { photoCaptureCamera.targetTexture = oldTarget; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(rt); }
        ReleasePhoto(); _photoTexture = texture;
        _photoSprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
        imgPolaroidPhoto.sprite = _photoSprite;
    }
    private void ReleasePhoto()
    {
        if (_photoSprite != null) Destroy(_photoSprite);
        if (_photoTexture != null) Destroy(_photoTexture);
        _photoSprite = null; _photoTexture = null;
    }
    private void HandleComputerInteracted()
    {
        if (!Accept(CheckInStep.Step3_ComputerRegister)) return;
        _busy = true; SetStep(currentStep); _operation = StartCoroutine(RegisteringRoutine());
    }
    private IEnumerator RegisteringRoutine()
    {
        panelComputerRegister.SetActive(true); txtRegisterStatus.text = "REGISTERING..."; sliderRegisterProgress.value = 0;
        PlaySound(soundComputerBeep);
        float elapsed = 0f;
        while (elapsed < registerDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / registerDuration);
            sliderRegisterProgress.value = progress;
            if (_receptionProgress != null) _receptionProgress.fillAmount = progress;
            if (_receptionStatus != null) _receptionStatus.SetText("ĐANG ĐĂNG KÝ\n{0:0}%", progress * 100f);
            yield return null;
        }
        if (_receptionProgress != null) _receptionProgress.fillAmount = 1f;
        if (_receptionStatus != null) _receptionStatus.text = "ĐĂNG KÝ HOÀN TẤT\n100%";
        txtRegisterStatus.text = "REGISTERED"; yield return new WaitForSeconds(0.7f);
        panelComputerRegister.SetActive(false); stepComputer.MarkCompleted(); _busy = false;
        SetStep(CheckInStep.Step4_PrinterRoomCard); _operation = null;
    }
    private void HandlePrinterInteracted()
    {
        if (!Accept(CheckInStep.Step4_PrinterRoomCard) || _reservedRoom == null) return;
        _busy = true; SetStep(currentStep); _operation = StartCoroutine(PrintRoutine());
    }
    private IEnumerator PrintRoutine()
    {
        PlaySound(soundPrinter);
        worldPrintedCardObject.SetActive(true); panelPrinterCard.SetActive(true);
        txtAssignedRoom.text = "THẺ KHÁM BỆNH\n" + patientName + "\n" + _patient.PatientId + "\nPHÒNG SỐ " + _reservedRoom.roomNumber;
        var worldLabel = worldPrintedCardObject.GetComponentInChildren<TMPro.TextMeshPro>();
        if (worldLabel != null) worldLabel.text = "ROOM " + _reservedRoom.roomNumber + "\n" + patientName;
        yield return new WaitForSeconds(printDuration);
        stepPrinter.MarkCompleted(); _busy = false; SetStep(CheckInStep.Completed_Leaving);
        if (!_sentCompletion) { _sentCompletion = true; OnPatientCheckedIn?.Invoke(patientName, _reservedRoom.roomNumber); _patient.LeaveForRoom(_reservedRoom); }
        yield return new WaitForSeconds(2f); panelPrinterCard.SetActive(false); _operation = null;
    }
    private void ClearDesk()
    {
        if (_patient != null) { _patient.ArrivedAtDesk -= HandlePatientArrived; _patient.NavigationFailed -= HandleNavigationFailed; }
        _patient = null; activePatientObject = null; _reservedRoom = null;
        if (stepClipboard != null) stepClipboard.gameObject.SetActive(false);
        SetStep(CheckInStep.Idle_WaitingBell);
    }
    private void HandleNavigationFailed(AAH_PatientActor patient)
    {
        if (patient != _patient) return;
        if (!_sentCompletion) roomRegistry.ReleaseRoom(patient.PatientId);
        if (_operation != null) StopCoroutine(_operation);
        _operation = null; _busy = false; RestoreModal(); HidePanels(); ClearDesk();
    }
    private void PlaySound(AudioClip clip) { if (clip != null && audioSource != null) audioSource.PlayOneShot(clip); }
    void OnDisable()
    {
        StopAllCoroutines(); _operation = null; _busy = false; RestoreModal(); HidePanels();
        if (stepClipboard != null) stepClipboard.OnInteracted -= HandleClipboardInteracted;
        if (stepCameraTripod != null) stepCameraTripod.OnInteracted -= HandleCameraInteracted;
        if (stepComputer != null) stepComputer.OnInteracted -= HandleComputerInteracted;
        if (stepPrinter != null) stepPrinter.OnInteracted -= HandlePrinterInteracted;
        if (btnSignClipboard != null) btnSignClipboard.onClick.RemoveListener(ConfirmSignClipboard);
        if (_patient != null && !_sentCompletion && roomRegistry != null) roomRegistry.ReleaseRoom(_patient.PatientId);
        ClearDesk(); ResetSteps();
        if (Instance == this) Instance = null;
    }
    void OnDestroy() { ReleasePhoto(); }
}
