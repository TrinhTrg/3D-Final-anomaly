using UnityEngine;

/// <summary>Bottom-left brain HUD for the local player's sanity.</summary>
public class AAH_SanityHUD : MonoBehaviour
{
    [Tooltip("Local player's sanity component.")]
    [SerializeField] private AAH_PlayerSanity _sanity;
    [SerializeField] private TMPro.TMP_Text _percentText;
    [SerializeField] private TMPro.TMP_Text _statusText;
    [SerializeField] private TMPro.TMP_Text _floatingText;

    [Header("Colours")]
    [SerializeField] private Color _stable = new Color(0.35f, 1f, 0.25f);
    [SerializeField] private Color _stressed = new Color(1f, 0.85f, 0.15f);
    [SerializeField] private Color _critical = new Color(1f, 0.25f, 0.2f);
    [SerializeField] private Color _depleted = new Color(0.6f, 0.6f, 0.6f);

    [Header("Floating notification")]
    [SerializeField, Min(0.1f)] private float _floatDuration = 1.2f;
    [SerializeField] private float _floatRise = 40f;

    private float _floatTimer;
    private Vector2 _floatOrigin;

    private void Awake()
    {
        if (_floatingText != null) { _floatOrigin = _floatingText.rectTransform.anchoredPosition; _floatingText.gameObject.SetActive(false); }
    }
    private void OnEnable()
    {
        if (_sanity == null) return;
        _sanity.SanityChanged += HandleChanged;
        Show(_sanity.Current);
    }
    private void OnDisable() { if (_sanity != null) _sanity.SanityChanged -= HandleChanged; }
    // OnEnable may run before the player's Awake initialised sanity; Start runs after all Awakes.
    private void Start() { if (_sanity != null) Show(_sanity.Current); }

    private void HandleChanged(int value, int delta)
    {
        Show(value);
        if (_floatingText == null) return;
        _floatingText.text = (delta > 0 ? "+" : "") + delta + " Sanity";
        _floatingText.color = delta > 0 ? _stable : _critical;
        _floatingText.rectTransform.anchoredPosition = _floatOrigin;
        _floatingText.gameObject.SetActive(true);
        _floatTimer = _floatDuration;
    }

    private void Show(int value)
    {
        string status; Color color;
        if (value <= 0) { status = "Sanity Depleted"; color = _depleted; }
        else if (value <= 30) { status = "Critical"; color = _critical; }
        else if (value <= 70) { status = "Stressed"; color = _stressed; }
        else { status = "Stable"; color = _stable; }
        if (_percentText != null) { _percentText.SetText("{0:0}%", value); _percentText.color = color; }
        if (_statusText != null) { _statusText.text = status; _statusText.color = color; }
    }

    private void Update()
    {
        if (_floatTimer <= 0f || _floatingText == null) return;
        // Unscaled so notifications still clear while paused.
        _floatTimer -= Time.unscaledDeltaTime;
        float t = 1f - Mathf.Clamp01(_floatTimer / _floatDuration);
        _floatingText.rectTransform.anchoredPosition = _floatOrigin + Vector2.up * (_floatRise * t);
        var c = _floatingText.color; c.a = 1f - t; _floatingText.color = c;
        if (_floatTimer <= 0f) _floatingText.gameObject.SetActive(false);
    }
}
