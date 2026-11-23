using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AAH_InteractionHints
{
    public static void Center(TMP_Text text)
    {
        if (text == null) return;
        CenterRect(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
    }

    private static void CenterRect(RectTransform rect)
    {
        if (rect == null) return;
        var canvas = rect.GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.WorldSpace) return;
        var root = (RectTransform)canvas.rootCanvas.transform;
        if (rect == root) return;
        Vector2 size = rect.rect.size;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        if (rect.parent != root) rect.position = root.TransformPoint(root.rect.center);
    }

    private static void CenterPanel(GameObject panel)
    {
        if (panel == null) return;
        CenterRect(panel.transform as RectTransform);
        foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
            Center(label);
    }

    public static void CenterScenePrompts(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var x in root.GetComponentsInChildren<AAH_PatientInspection>(true)) CenterPanel(x.uiPromptObject);
            foreach (var x in root.GetComponentsInChildren<AAH_ReceptionDesk>(true)) CenterPanel(x.uiBellPrompt);
            foreach (var x in root.GetComponentsInChildren<AAH_AnomalyDetection>(true)) CenterPanel(x.uiDetectPrompt);
            foreach (var ui in root.GetComponentsInChildren<Canvas>(true))
            {
                if (ui.renderMode == RenderMode.WorldSpace) continue;
                var prompt = ui.transform.Find("DoorPrompt");
                if (prompt != null) CenterPanel(prompt.gameObject);
            }
            foreach (var x in root.GetComponentsInChildren<Door>(true)) CenterPanel(x.intText);
            foreach (var x in root.GetComponentsInChildren<KeyScript>(true)) CenterPanel(x.inticon);
            foreach (var x in root.GetComponentsInChildren<LightScript>(true)) CenterPanel(x.inticon);
        }
    }

    private static GUIStyle _style;
    public static void Draw(string text)
    {
        if (Time.timeScale <= 0f || string.IsNullOrEmpty(text) || Cursor.lockState != CursorLockMode.Locked) return;
        if (_style == null) _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
        _style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.023f), 15, 28);
        float width = _style.CalcSize(new GUIContent(text)).x + 24f;
        GUI.Box(new Rect((Screen.width - width) / 2f, (Screen.height - 44f) / 2f, width, 44f), text, _style);
    }
}
