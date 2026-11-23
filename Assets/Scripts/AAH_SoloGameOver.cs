using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AAH_SoloGameOver : MonoBehaviour
{
    private bool _loading;
    private float _previousTimeScale;
    private TMP_Text _message;
    private const string VolumeKey = "VetNightmare.MasterVolume";
    private RectTransform _actions, _settings;
    private Button _openSettings, _back;
    private TMP_Text _volumeLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreVolume()
    {
        if (PlayerPrefs.HasKey(VolumeKey))
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey));
    }

    private void Awake()
    {
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        var player = FindFirstObjectByType<SC_FPSController>();
        var inventory = FindFirstObjectByType<AAH_PlayerInventory>();
        if (inventory != null) inventory.enabled = false;
        if (player != null) player.canMove = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current == null)
        {
            var system = new GameObject("GameOverEvents", typeof(EventSystem));
            system.transform.SetParent(transform, false);
            system.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var overlay = new GameObject("GameOver", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var shade = new GameObject("Background", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(overlay.transform, false);
        var background = (RectTransform)shade.transform;
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = background.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0.045f, 0.052f, 0.063f, 0.96f);

        _actions = Element(background, "GameOverActions", 700f, 460f, 0f);
        Label(_actions, "GAME OVER", 56, 140f);
        _message = Label(_actions, "Bạn đã gục ngã trong ca trực.", 23, 80f);
        var restart = ButtonAt(_actions, "Chơi lại", 6f, () => Load(gameObject.scene.name));
        _openSettings = ButtonAt(_actions, "Cài đặt", -60f, () => ShowSettings(true));
        ButtonAt(_actions, "Về menu chính", -126f, () => Load("menu"));

        _settings = Element(background, "Settings", 700f, 460f, 0f);
        Label(_settings, "Cài đặt", 48, 120f);
        _volumeLabel = Label(_settings, "", 26, 55f);
        ButtonAt(_settings, "Giảm âm lượng", -12f, () => ChangeVolume(-0.1f));
        ButtonAt(_settings, "Tăng âm lượng", -76f, () => ChangeVolume(0.1f));
        _back = ButtonAt(_settings, "Quay lại", -140f, () => ShowSettings(false));
        _settings.gameObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(restart.gameObject);
    }

    private void ShowSettings(bool show)
    {
        _actions.gameObject.SetActive(!show);
        _settings.gameObject.SetActive(show);
        DisplayVolume();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(show ? _back.gameObject : _openSettings.gameObject);
    }

    private void ChangeVolume(float change)
    {
        AudioListener.volume = Mathf.Clamp01(AudioListener.volume + change);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
        DisplayVolume();
    }

    private void DisplayVolume()
    {
        _volumeLabel.text = "Âm lượng tổng " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
    }

    private static RectTransform Element(Transform parent, string name, float width, float height, float y)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0f, y);
        return rect;
    }
    private static TMP_Text Label(Transform parent, string text, float size, float y)
    {
        var label = Element(parent, "Label", 700f, 72f, y).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Button ButtonAt(Transform parent, string text, float y, UnityEngine.Events.UnityAction action)
    {
        var rect = Element(parent, text, 310f, 52f, y);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.20f, 0.31f, 0.37f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label(rect, text, 23f, 0f);
        return button;
    }

    private void Load(string scene)
    {
        if (_loading) return;
        bool inBuild = Application.CanStreamedLevelBeLoaded(scene);
#if UNITY_EDITOR
        string editorPath = scene == gameObject.scene.name ? gameObject.scene.path : "Assets/Scenes/" + scene + ".unity";
        bool editorScene = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(editorPath) != null;
#else
        bool editorScene = false;
#endif
        if (!inBuild && !editorScene)
        {
            _message.text = "Hãy bật scene " + scene + " trong Build Profiles → Scene List.";
            return;
        }
        _loading = true;
        Time.timeScale = 1f;
        try
        {
#if UNITY_EDITOR
            if (!inBuild)
            {
                var parameters = new LoadSceneParameters(LoadSceneMode.Single);
                if (UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(editorPath, parameters) != null) return;
            }
            else
#endif
            if (SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single) != null) return;
        }
        catch (System.Exception error) { Debug.LogError(error.Message, this); }
        _loading = false;
        Time.timeScale = 0f;
        _message.text = "Chưa tải được scene " + scene + ".";
    }

    private void OnDestroy()
    {
        if (!_loading) Time.timeScale = _previousTimeScale;
    }
}
