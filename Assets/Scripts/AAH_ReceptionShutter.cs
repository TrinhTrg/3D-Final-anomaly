using System;
using UnityEngine;

public sealed class AAH_ReceptionShutter : MonoBehaviour
{
    [Header("Curtain")]
    [SerializeField] private Transform[] _slats;
    [SerializeField] private Transform _bottomRail;
    [SerializeField] private BoxCollider _blocker;
    [SerializeField, Min(0.01f)] private float _width = 15.8f;
    [SerializeField, Min(0.01f)] private float _height = 5.5f;
    [SerializeField, Min(0.01f)] private float _thickness = 0.08f;
    [Header("Motion")]
    [SerializeField, Min(0.1f)] private float _travelDuration = 2f;
    [SerializeField] private bool _startClosed;
    private float _from, _to, _elapsed;
    private bool _closed;
    public bool IsClosed => _closed;
    public bool IsMoving { get; private set; }
    public float Closure { get; private set; }
    // A future authoritative network adapter can call SetClosed on replicated state.
    public event Action<bool> TargetChanged;
    private void Awake() { SnapTo(_startClosed); }
    public void Toggle() { SetClosed(!_closed); }
    public void SetClosed(bool closed)
    {
        if (_closed == closed) return;
        _closed = closed;
        _from = Closure; _to = closed ? 1f : 0f; _elapsed = 0f;
        IsMoving = !Mathf.Approximately(_from, _to);
        TargetChanged?.Invoke(closed);
    }
    public void SnapTo(bool closed)
    {
        _closed = closed; IsMoving = false;
        Closure = closed ? 1f : 0f;
        _from = _to = Closure; _elapsed = 0f;
        ApplyCurtain();
    }
    private void Update()
    {
        if (!IsMoving) return;
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _travelDuration);
        Closure = Mathf.Lerp(_from, _to, t * t * (3f - 2f * t));
        if (t >= 1f) { Closure = _to; IsMoving = false; }
        ApplyCurtain();
    }
    private void ApplyCurtain()
    {
        float length = _height * Closure;
        if (_slats != null && _slats.Length > 0)
        {
            float pitch = _height / _slats.Length;
            for (int i = 0; i < _slats.Length; i++)
            {
                var slat = _slats[i];
                if (slat == null) continue;
                float exposed = Mathf.Clamp(length - i * pitch, 0f, pitch);
                bool visible = exposed > 0.0001f;
                if (slat.gameObject.activeSelf != visible) slat.gameObject.SetActive(visible);
                if (!visible) continue;
                slat.localPosition = new Vector3(0f, -(i * pitch + exposed * 0.5f), 0f);
                slat.localScale = new Vector3(_width, exposed, _thickness);
            }
        }
        if (_bottomRail != null) _bottomRail.localPosition = new Vector3(0f, -length, 0f);
        if (_blocker != null)
        {
            _blocker.enabled = length > 0.0001f;
            _blocker.center = new Vector3(0f, -length * 0.5f, 0f);
            _blocker.size = new Vector3(_width, Mathf.Max(length, 0.001f), _thickness);
        }
    }
}
