#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Tool thiết lập tự động toàn bộ quy trình Check-in:
/// - Tạo vật thể tương tác: Chuông, Clipboard, Máy ảnh chân máy, Máy tính, Máy in.
/// - Tạo các vòng viền xanh (Highlight Ring) tại chân mỗi vật thể.
/// - Thiết lập Canvas UI: Thoại bệnh nhân, Bảng Clipboard, Thẻ Polaroid ảnh mờ dần, Thanh tiến trình Máy tính, Giấy in phòng số (1-6).
/// - Liên kết với AAH_CheckInSequence.
/// </summary>
public class AAH_CheckInSetupTool : EditorWindow
{
    [MenuItem("Tools/AAH/🛎️ Thiết Lập Quy Trình Check-in Quầy Lễ Tân")]
    public static void SetupCheckInAll()
    {
        // An installed NPC-driven system must not be replaced by the old player-relative setup.
        foreach (var sceneRoot in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var installed = sceneRoot.GetComponentInChildren<AAH_PatientArrivalController>(true);
            if (installed == null) continue;
            var installedSequence = installed.GetComponent<AAH_CheckInSequence>();
            string error = null;
            if (installedSequence == null || installedSequence.roomRegistry == null || !installedSequence.roomRegistry.ValidateConfiguration(out error))
            {
                Debug.LogError("Check-in room configuration requires repair: " + error, installed);
                return;
            }
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var surface in root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true))
                    surface.BuildNavMesh();
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("NPC check-in already configured. Navigation refreshed; no duplicate equipment created.");
            return;
        }
        // 1. TÌM VỊ TRÍ QUẦY LỄ TÂN HOẶC PLAYER
        GameObject player = GameObject.Find("Player");
        Vector3 basePos = new Vector3(26f, -18f, 14.5f);
        if (player != null)
        {
            basePos = player.transform.position;
        }

        // Tạo nhóm chứa các điểm check-in
        GameObject checkInRoot = GameObject.Find("=== CHECKIN_SYSTEM ===");
        if (checkInRoot == null) checkInRoot = new GameObject("=== CHECKIN_SYSTEM ===");

        AAH_CheckInSequence sequence = checkInRoot.GetComponent<AAH_CheckInSequence>();
        if (sequence == null) sequence = checkInRoot.AddComponent<AAH_CheckInSequence>();

        // Điểm đặt vật dụng trên mặt bàn quầy (đứng trước mặt Player)
        Vector3 deskForward = player != null ? player.transform.forward : Vector3.forward;
        Vector3 deskRight = player != null ? player.transform.right : Vector3.right;
        Vector3 deskCenter = basePos + deskForward * 1.5f;

        // 2. TẠO CÁC OBJECT TƯƠNG TÁC
        // A. Chuông
        sequence.stepBell = CreateInteractableItem("Bell_Interactable", 
            deskCenter - deskRight * 1.2f + Vector3.up * 0.1f, 
            "Nhấn [E] để Bấm Chuông gọi bệnh nhân", checkInRoot.transform, 
            "Assets/Models/Equipment/shutter_button.glb");

        // B. Bảng kẹp giấy (Clipboard)
        sequence.stepClipboard = CreateInteractableItem("Clipboard_Interactable", 
            deskCenter - deskRight * 0.5f + Vector3.up * 0.1f, 
            "Nhấn [E] để xem & ký Bảng kẹp giấy", checkInRoot.transform, 
            "Assets/Models/Equipment/notepad.glb");

        // C. Máy ảnh chân máy (Tripod Camera)
        sequence.stepCameraTripod = CreateInteractableItem("CameraTripod_Interactable", 
            deskCenter + deskRight * 1.3f, 
            "Nhấn [E] để Chụp ảnh bệnh nhân", checkInRoot.transform, 
            "Assets/Models/Equipment/camera.glb");

        // D. Máy tính
        sequence.stepComputer = CreateInteractableItem("Computer_Interactable", 
            deskCenter + deskRight * 0.2f + Vector3.up * 0.1f, 
            "Nhấn [E] để Đăng ký trên Máy tính", checkInRoot.transform, null);

        // E. Máy in
        sequence.stepPrinter = CreateInteractableItem("Printer_Interactable", 
            deskCenter + deskRight * 0.8f + Vector3.up * 0.1f, 
            "Nhấn [E] để In thẻ phòng khám", checkInRoot.transform, 
            "Assets/Models/Equipment/printers_4.glb");

        // Điểm đứng của bệnh nhân (bên ngoài quầy)
        GameObject standPoint = GameObject.Find("Patient_CheckIn_StandPoint");
        if (standPoint == null) standPoint = new GameObject("Patient_CheckIn_StandPoint");
        standPoint.transform.position = deskCenter + deskForward * 1.5f;
        standPoint.transform.rotation = Quaternion.LookRotation(-deskForward);
        sequence.patientStandPoint = standPoint.transform;

        // 3. TẠO HOẶC LẤY BỆNH NHÂN (Lấy model Hokaido hoặc Ron)
        GameObject patient = GameObject.Find("Patient_CheckIn_Actor");
        if (patient == null)
        {
            GameObject patientPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Animals/Patient/Hokaido.glb");
            if (patientPrefab != null)
            {
                patient = (GameObject)PrefabUtility.InstantiatePrefab(patientPrefab);
                patient.name = "Patient_CheckIn_Actor";
                patient.transform.position = standPoint.transform.position;
                patient.transform.rotation = standPoint.transform.rotation;
                patient.transform.SetParent(checkInRoot.transform);
            }
        }
        sequence.activePatientObject = patient;
        sequence.patientName = "Mai Yamada";
        sequence.patientDialogue = "I'm Mai Yamada. I'm here for my appointment...";

        // 4. THIẾT LẬP CANVAS VÀ CÁC PANEL UI
        SetupCheckInUI(sequence);

        // Lưu scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(checkInRoot.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công!", 
            "Đã thiết lập đầy đủ Quy trình Check-in Quầy Lễ Tân:\n\n" +
            "1. Chuông gọi bệnh nhân (Bell) & Thoại nhân vật\n" +
            "2. Bảng kẹp giấy (Clipboard)\n" +
            "3. Máy ảnh chân máy (Tripod) & Ảnh Polaroid hiện dần\n" +
            "4. Máy tính (Computer) với thanh tiến trình REGISTERING\n" +
            "5. Máy in (Printer) in thẻ phòng (1 - 6)\n" +
            "6. Vòng xanh viền tương tác tuần tự từng bước!", "Tuyệt vời");
    }

    private static AAH_InteractableStep CreateInteractableItem(string name, Vector3 pos, string prompt, Transform parent, string modelPath)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        // Thêm collider nếu chưa có
        if (go.GetComponent<Collider>() == null)
        {
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.radius = 0.5f;
            sc.isTrigger = true;
        }

        // Nạp model 3D trang trí nếu có
        if (!string.IsNullOrEmpty(modelPath) && go.transform.childCount == 0)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab != null)
            {
                GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                modelInstance.transform.SetParent(go.transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one * 0.8f;
            }
        }

        // Tạo vòng tròn viền xanh (Highlight Ring)
        Transform ringTrans = go.transform.Find("HighlightRing");
        GameObject ringGO = ringTrans != null ? ringTrans.gameObject : null;
        if (ringGO == null)
        {
            ringGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringGO.name = "HighlightRing";
            ringGO.transform.SetParent(go.transform);
            ringGO.transform.localPosition = new Vector3(0, -0.05f, 0);
            ringGO.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
            
            // Xóa collider của ring để không cản trở
            Collider c = ringGO.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

            // Gán material xanh ngọc dạ quang (Cyan/Green Glow)
            Renderer r = ringGO.GetComponent<Renderer>();
            if (r != null)
            {
                Material ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                ringMat.color = new Color(0f, 1f, 0.8f, 0.8f);
                r.material = ringMat;
            }
        }

        AAH_InteractableStep step = go.GetComponent<AAH_InteractableStep>();
        if (step == null) step = go.AddComponent<AAH_InteractableStep>();
        step.promptMessage = prompt;
        step.highlightRing = ringGO;
        step.interactionDistance = 2.5f;

        return step;
    }

    private static void SetupCheckInUI(AAH_CheckInSequence sequence)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("CheckIn_Canvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        Transform uiRoot = canvas.transform;

        // 1. Text Prompt Tương tác ở giữa cạnh dưới
        Transform promptTrans = uiRoot.Find("TXT_CheckInPrompt");
        if (promptTrans == null)
        {
            GameObject pGO = new GameObject("TXT_CheckInPrompt");
            pGO.transform.SetParent(uiRoot, false);
            TextMeshProUGUI tmp = pGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Nhấn [E] để tương tác";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;
            RectTransform rt = pGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.15f);
            rt.anchorMax = new Vector2(0.5f, 0.15f);
            rt.sizeDelta = new Vector2(700, 50);
            sequence.txtInteractionPrompt = tmp;
        }
        else
        {
            sequence.txtInteractionPrompt = promptTrans.GetComponent<TextMeshProUGUI>();
        }

        // 2. Thẻ ảnh Polaroid cố định trên HUD (Góc dưới bên phải)
        Transform polaroidTrans = uiRoot.Find("Card_Polaroid_HUD");
        if (polaroidTrans == null)
        {
            GameObject card = new GameObject("Card_Polaroid_HUD");
            card.transform.SetParent(uiRoot, false);
            Image bg = card.AddComponent<Image>();
            bg.color = Color.white; // Viền trắng Polaroid
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-30, 30);
            rt.sizeDelta = new Vector2(170, 210);

            // Ảnh bên trong Polaroid
            GameObject photo = new GameObject("Photo");
            photo.transform.SetParent(card.transform, false);
            Image photoImg = photo.AddComponent<Image>();
            photoImg.color = Color.black;
            RectTransform photoRt = photo.GetComponent<RectTransform>();
            photoRt.anchorMin = new Vector2(0.5f, 0.6f);
            photoRt.anchorMax = new Vector2(0.5f, 0.6f);
            photoRt.sizeDelta = new Vector2(145, 145);

            // Tên bệnh nhân dưới ảnh
            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(card.transform, false);
            TextMeshProUGUI nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Mai Yamada";
            nameTmp.fontSize = 14;
            nameTmp.color = Color.black;
            nameTmp.alignment = TextAlignmentOptions.Center;
            RectTransform nameRt = nameGO.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 0.1f);
            nameRt.anchorMax = new Vector2(0.5f, 0.1f);
            nameRt.sizeDelta = new Vector2(150, 30);

            sequence.cardPolaroidHUD = card;
            sequence.imgPolaroidPhoto = photoImg;
            sequence.txtPolaroidName = nameTmp;
            card.SetActive(false);
        }

        // 3. Panel Máy Tính Đăng Ký (Registering...)
        Transform compTrans = uiRoot.Find("Panel_ComputerRegister");
        if (compTrans == null)
        {
            GameObject compPanel = new GameObject("Panel_ComputerRegister");
            compPanel.transform.SetParent(uiRoot, false);
            Image bg = compPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.15f, 0.3f, 0.9f); // Nền xanh màn hình CRT máy tính
            RectTransform rt = compPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 200);

            // Chữ REGISTERING...
            GameObject txtGO = new GameObject("TXT_Status");
            txtGO.transform.SetParent(compPanel.transform, false);
            TextMeshProUGUI statusTmp = txtGO.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "REGISTERING...";
            statusTmp.fontSize = 28;
            statusTmp.color = Color.cyan;
            statusTmp.alignment = TextAlignmentOptions.Center;
            RectTransform txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchoredPosition = new Vector2(0, 35);
            txtRt.sizeDelta = new Vector2(350, 45);

            // Slider tiến trình
            GameObject sliderGO = new GameObject("Slider_Progress");
            sliderGO.transform.SetParent(compPanel.transform, false);
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            RectTransform sliderRt = sliderGO.GetComponent<RectTransform>();
            sliderRt.anchoredPosition = new Vector2(0, -30);
            sliderRt.sizeDelta = new Vector2(300, 20);

            sequence.panelComputerRegister = compPanel;
            sequence.txtRegisterStatus = statusTmp;
            sequence.sliderRegisterProgress = slider;
            compPanel.SetActive(false);
        }

        // 4. Panel Máy In Thẻ Phòng
        Transform printerTrans = uiRoot.Find("Panel_PrinterCard");
        if (printerTrans == null)
        {
            GameObject printPanel = new GameObject("Panel_PrinterCard");
            printPanel.transform.SetParent(uiRoot, false);
            Image bg = printPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            RectTransform rt = printPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(450, 220);

            GameObject txtGO = new GameObject("TXT_Room");
            txtGO.transform.SetParent(printPanel.transform, false);
            TextMeshProUGUI roomTmp = txtGO.AddComponent<TextMeshProUGUI>();
            roomTmp.text = "THẺ KHÁM BỆNH\nPhòng: PHÒNG SỐ 1";
            roomTmp.fontSize = 24;
            roomTmp.color = Color.white;
            roomTmp.alignment = TextAlignmentOptions.Center;
            RectTransform txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.sizeDelta = new Vector2(400, 160);

            sequence.panelPrinterCard = printPanel;
            sequence.txtAssignedRoom = roomTmp;
            printPanel.SetActive(false);
        }

        // 5. Flash Screen
        Transform flashTrans = uiRoot.Find("Panel_CameraFlash");
        if (flashTrans == null)
        {
            GameObject flash = new GameObject("Panel_CameraFlash");
            flash.transform.SetParent(uiRoot, false);
            Image img = flash.AddComponent<Image>();
            img.color = Color.white;
            RectTransform rt = flash.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            sequence.panelFlash = flash;
            flash.SetActive(false);
        }
    }
}
#endif

