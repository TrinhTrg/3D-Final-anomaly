#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// AAH_UIBuilder — Editor tool to auto-create and wire all UI elements.
/// Access via: Tools → AAH → Setup UI Canvas
/// Must be run AFTER the HospitalDay1 scene is built and open.
/// </summary>
public class AAH_UIBuilder : EditorWindow
{
    [MenuItem("Tools/AAH/Setup UI Canvas")]
    public static void SetupUI()
    {
        // Verify we have the right components in scene
        AAH_GameManager gm = Object.FindFirstObjectByType<AAH_GameManager>();
        if (gm == null)
        {
            EditorUtility.DisplayDialog("AAH UI Builder",
                "ERROR: AAH_GameManager not found in scene!\n\n" +
                "Please run 'Tools → AAH → Build Hospital Day 1 Scene' first,\n" +
                "then open the HospitalDay1 scene.",
                "OK");
            return;
        }

        // ── CREATE CANVAS ──────────────────────────────────────────
        // Remove old UI setup note
        GameObject oldNote = GameObject.Find("_UI_SETUP_REQUIRED");
        if (oldNote != null) DestroyImmediate(oldNote);

        // Main HUD Canvas
        GameObject canvasGO = new GameObject("HUD_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── TOP-LEFT: Shift Info ────────────────────────────────────
        TextMeshProUGUI txtShift = CreateTMP("TXT_ShiftLabel", canvasGO.transform,
            new Vector2(20, -20), new Vector2(400, 40),
            "SHIFT 1", 28, TextAlignmentOptions.TopLeft, Color.white,
            TextAnchor.UpperLeft);

        TextMeshProUGUI txtObjective = CreateTMP("TXT_Objective", canvasGO.transform,
            new Vector2(20, -65), new Vector2(500, 60),
            "Inspect patients at Reception\nInspected: 0/3", 18, TextAlignmentOptions.TopLeft,
            new Color(0.8f, 0.85f, 1f),
            TextAnchor.UpperLeft);

        // ── TOP-RIGHT: Counters ─────────────────────────────────────
        TextMeshProUGUI txtPatientCount = CreateTMP("TXT_PatientCount", canvasGO.transform,
            new Vector2(-20, -20), new Vector2(300, 35),
            "Patients: 0/3", 22, TextAlignmentOptions.TopRight, Color.white,
            TextAnchor.UpperRight);

        TextMeshProUGUI txtAnomalyCount = CreateTMP("TXT_AnomalyCount", canvasGO.transform,
            new Vector2(-20, -55), new Vector2(300, 35),
            "Anomalies: 0", 22, TextAlignmentOptions.TopRight, new Color(1f, 0.4f, 0.4f),
            TextAnchor.UpperRight);

        // ── BOTTOM-RIGHT: CCTV Status ───────────────────────────────
        TextMeshProUGUI txtCameraStatus = CreateTMP("TXT_CameraStatus", canvasGO.transform,
            new Vector2(-20, 20), new Vector2(300, 35),
            "[C] CCTV: OFF", 20, TextAlignmentOptions.BottomRight, new Color(0.5f, 1f, 0.5f),
            TextAnchor.LowerRight);

        // ── BOTTOM-CENTER: Controls Help ────────────────────────────
        CreateTMP("TXT_Controls", canvasGO.transform,
            new Vector2(0, 20), new Vector2(600, 35),
            "WASD: Move | Shift: Sprint | E: Inspect | C: CCTV", 14,
            TextAlignmentOptions.Bottom, new Color(0.7f, 0.7f, 0.7f),
            TextAnchor.LowerCenter);

        // ── CENTER: Interaction Prompt ──────────────────────────────
        GameObject promptGO = CreatePanel("Panel_InteractPrompt", canvasGO.transform,
            new Vector2(0, -80), new Vector2(350, 50),
            new Color(0, 0, 0, 0.7f), TextAnchor.MiddleCenter);
        TextMeshProUGUI txtPrompt = CreateTMPChild("TXT_Prompt", promptGO.transform,
            "Press  E  to Inspect", 22, TextAlignmentOptions.Center, Color.yellow);
        promptGO.SetActive(false);

        // ── CENTER: Inspection Result Panel ─────────────────────────
        GameObject resultPanel = CreatePanel("Panel_InspectResult", canvasGO.transform,
            new Vector2(0, 50), new Vector2(500, 250),
            new Color(0.05f, 0.05f, 0.1f, 0.92f), TextAnchor.MiddleCenter);
        TextMeshProUGUI txtResult = CreateTMPChild("TXT_Result", resultPanel.transform,
            "INSPECTION REPORT\n\n...", 20, TextAlignmentOptions.Center, Color.white);
        resultPanel.SetActive(false);

        // ── CENTER: CCTV Detect Prompt ──────────────────────────────
        GameObject detectPrompt = CreatePanel("Panel_DetectPrompt", canvasGO.transform,
            new Vector2(0, -140), new Vector2(400, 50),
            new Color(0, 0.2f, 0, 0.8f), TextAnchor.MiddleCenter);
        CreateTMPChild("TXT_DetectPromptText", detectPrompt.transform,
            "Press  E  to Report Anomaly", 22, TextAlignmentOptions.Center,
            new Color(1f, 0.3f, 0.3f));
        detectPrompt.SetActive(false);

        // ── CENTER: Detection Result Panel ──────────────────────────
        GameObject detectResult = CreatePanel("Panel_DetectResult", canvasGO.transform,
            new Vector2(0, 120), new Vector2(450, 200),
            new Color(0.15f, 0.02f, 0.02f, 0.92f), TextAnchor.MiddleCenter);
        TextMeshProUGUI txtDetectResult = CreateTMPChild("TXT_DetectResult", detectResult.transform,
            "ANOMALY REPORTED", 20, TextAlignmentOptions.Center, Color.white);
        detectResult.SetActive(false);

        // ── CCTV Overlay (full-screen green tint) ───────────────────
        GameObject cctvOverlay = new GameObject("CCTV_Overlay");
        cctvOverlay.transform.SetParent(canvasGO.transform, false);
        RectTransform cctvRT = cctvOverlay.AddComponent<RectTransform>();
        cctvRT.anchorMin = Vector2.zero;
        cctvRT.anchorMax = Vector2.one;
        cctvRT.offsetMin = Vector2.zero;
        cctvRT.offsetMax = Vector2.zero;
        Image cctvImg = cctvOverlay.AddComponent<Image>();
        cctvImg.color = new Color(0f, 0.15f, 0f, 0.3f);
        cctvImg.raycastTarget = false;

        // CCTV scanline text
        CreateTMPChild("TXT_CCTVLabel", cctvOverlay.transform,
            "◉ REC  CCTV SURVEILLANCE", 16, TextAlignmentOptions.TopLeft,
            new Color(0.2f, 1f, 0.2f, 0.8f));
        cctvOverlay.SetActive(false);

        // ── Dialogue Panel (patient arrival / Dr. Mupple) ───────────
        GameObject dialoguePanel = CreatePanel("Panel_Dialogue", canvasGO.transform,
            new Vector2(0, -250), new Vector2(700, 80),
            new Color(0, 0, 0, 0.85f), TextAnchor.LowerCenter);
        TextMeshProUGUI txtDialogue = CreateTMPChild("TXT_Dialogue", dialoguePanel.transform,
            "...", 20, TextAlignmentOptions.Center, new Color(0.9f, 0.95f, 1f));
        dialoguePanel.SetActive(false);

        // ── SHIFT END Panel ─────────────────────────────────────────
        GameObject shiftEndPanel = CreatePanel("Panel_ShiftEnd", canvasGO.transform,
            new Vector2(0, 0), new Vector2(600, 450),
            new Color(0.02f, 0.02f, 0.05f, 0.95f), TextAnchor.MiddleCenter);
        TextMeshProUGUI txtShiftResult = CreateTMPChild("TXT_ShiftResult", shiftEndPanel.transform,
            "SHIFT COMPLETE", 22, TextAlignmentOptions.Center, Color.white);

        // Shift End Buttons
        CreateButton("BTN_Retry", shiftEndPanel.transform,
            new Vector2(0, -120), new Vector2(200, 45),
            "RETRY SHIFT", new Color(0.2f, 0.5f, 0.2f));
        CreateButton("BTN_MainMenu", shiftEndPanel.transform,
            new Vector2(0, -175), new Vector2(200, 45),
            "MAIN MENU", new Color(0.3f, 0.3f, 0.5f));
        CreateButton("BTN_Quit", shiftEndPanel.transform,
            new Vector2(0, -230), new Vector2(200, 45),
            "QUIT", new Color(0.5f, 0.2f, 0.2f));

        // Add ShiftEndUI component
        AAH_ShiftEndUI shiftEndUI = shiftEndPanel.AddComponent<AAH_ShiftEndUI>();
        shiftEndPanel.SetActive(false);

        // ── Queue Status ────────────────────────────────────────────
        TextMeshProUGUI txtQueueStatus = CreateTMP("TXT_QueueStatus", canvasGO.transform,
            new Vector2(20, -130), new Vector2(350, 30),
            "Patients waiting: 3", 16, TextAlignmentOptions.TopLeft,
            new Color(0.7f, 0.7f, 0.5f), TextAnchor.UpperLeft);

        // ════════════════════════════════════════════════════════════
        // WIRE REFERENCES
        // ════════════════════════════════════════════════════════════

        // Wire GameManager
        gm.txtShiftLabel = txtShift;
        gm.txtObjective = txtObjective;
        gm.txtPatientCount = txtPatientCount;
        gm.txtAnomalyCount = txtAnomalyCount;
        gm.panelShiftEnd = shiftEndPanel;
        gm.txtShiftResult = txtShiftResult;
        EditorUtility.SetDirty(gm);

        // Wire ReceptionDesk
        AAH_ReceptionDesk desk = Object.FindFirstObjectByType<AAH_ReceptionDesk>();
        if (desk != null)
        {
            desk.dialoguePanel = dialoguePanel;
            desk.dialogueText = txtDialogue;
            desk.txtQueueStatus = txtQueueStatus;

            // Wire patient objects array
            Transform patientsRoot = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name == "=== PATIENTS ===")
                {
                    patientsRoot = t;
                    break;
                }
            }
            if (patientsRoot != null && patientsRoot.childCount > 0)
            {
                desk.patientObjects = new GameObject[patientsRoot.childCount];
                for (int i = 0; i < patientsRoot.childCount; i++)
                {
                    desk.patientObjects[i] = patientsRoot.GetChild(i).gameObject;
                }
            }
            EditorUtility.SetDirty(desk);
        }

        // Wire each Patient's Inspection component
        AAH_PatientInspection[] inspections = Object.FindObjectsByType<AAH_PatientInspection>(FindObjectsSortMode.None);
        foreach (var insp in inspections)
        {
            insp.uiPromptObject = promptGO;
            insp.uiResultPanel = resultPanel;
            insp.uiResultText = txtResult;
            EditorUtility.SetDirty(insp);
        }

        // Wire AnomalyDetection (on Player)
        AAH_AnomalyDetection detection = Object.FindFirstObjectByType<AAH_AnomalyDetection>();
        if (detection != null)
        {
            detection.cctvOverlay = cctvOverlay;
            detection.txtCameraStatus = txtCameraStatus;
            detection.uiDetectPrompt = detectPrompt;
            detection.uiDetectResult = detectResult;
            detection.txtDetectResult = txtDetectResult;
            EditorUtility.SetDirty(detection);
        }

        // Wire Dr. Mupple
        AAH_DrMupple mupple = Object.FindFirstObjectByType<AAH_DrMupple>();
        if (mupple != null)
        {
            mupple.dialoguePanel = dialoguePanel;
            mupple.dialogueText = txtDialogue;
            EditorUtility.SetDirty(mupple);
        }

        // ── EVENT SYSTEM ────────────────────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── SAVE ────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AAH] ✅ UI Canvas created and ALL references wired automatically!");
        EditorUtility.DisplayDialog("AAH UI Builder",
            "✅ UI Canvas setup complete!\n\n" +
            "Created:\n" +
            "• HUD (Shift Label, Objective, Counters)\n" +
            "• Interaction Prompt panel\n" +
            "• Inspection Result panel\n" +
            "• CCTV Overlay + Detection panels\n" +
            "• Dialogue panel\n" +
            "• Shift End panel with buttons\n\n" +
            "All references wired to GameManager,\n" +
            "Patient Inspections, Reception Desk, etc.\n\n" +
            "➡️ Press PLAY to test the game!",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────
    // UI HELPER FUNCTIONS
    // ─────────────────────────────────────────────────────────────────

    static TextMeshProUGUI CreateTMP(string name, Transform parent,
        Vector2 anchoredPos, Vector2 size, string text,
        float fontSize, TextAlignmentOptions alignment, Color color,
        TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();

        // Set anchors based on TextAnchor
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                break;
            case TextAnchor.UpperRight:
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                break;
            case TextAnchor.LowerLeft:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                break;
            case TextAnchor.LowerRight:
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                break;
            case TextAnchor.LowerCenter:
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0);
                break;
            default: // MiddleCenter
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;

        return tmp;
    }

    static GameObject CreatePanel(string name, Transform parent,
        Vector2 anchoredPos, Vector2 size, Color bgColor, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();

        switch (anchor)
        {
            case TextAnchor.MiddleCenter:
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
            case TextAnchor.LowerCenter:
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0);
                break;
            default:
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        return go;
    }

    static TextMeshProUGUI CreateTMPChild(string name, Transform parent,
        string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(15, 10);
        rt.offsetMax = new Vector2(-15, -10);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;

        return tmp;
    }

    static void CreateButton(string name, Transform parent,
        Vector2 anchoredPos, Vector2 size, string label, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor = bgColor * 0.7f;
        btn.colors = colors;

        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
#endif
