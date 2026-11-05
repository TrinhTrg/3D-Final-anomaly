using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Independent sanity value for one player. All changes go through ApplyChange so
/// clamping, depletion and HUD notifications stay consistent.
/// </summary>
public class AAH_PlayerSanity : MonoBehaviour
{
    public static readonly List<AAH_PlayerSanity> All = new List<AAH_PlayerSanity>();

    [Header("Range")]
    [SerializeField, Range(0, 100)] private int _startSanity = 100;
    [SerializeField, Min(1)] private int _maxSanity = 100;

    [Header("Passive reduction")]
    [Tooltip("Seconds of active, unpaused gameplay between passive losses.")]
    [SerializeField, Min(0.1f)] private float _passiveInterval = 50f;
    [SerializeField, Min(0)] private int _passiveAmount = 1;

    [Header("Anomaly check-in drain")]
    [Tooltip("Sanity lost per second while at least one checked-in anomaly is unresolved. Not specified yet.")]
    [SerializeField, Min(0f)] private float _anomalyDrainPerSecond = 0.5f;

    [Header("Red-Eyed Monster")]
    [SerializeField, Min(0)] private int _redEyeAmount = 5;
    [SerializeField, Min(0.1f)] private float _redEyeInterval = 2f;

    [Header("Events")]
    [Tooltip("Raised once when sanity reaches 0. Connect to a game-over or death system.")]
    [SerializeField] private UnityEvent _onSanityDepleted = new UnityEvent();

    private readonly HashSet<object> _anomalyDrains = new HashSet<object>();
    private readonly HashSet<object> _redEyeEncounters = new HashSet<object>();
    private float _passiveTimer, _anomalyAccumulator, _redEyeTimer;

    public int Current { get; private set; }
    public int Max => _maxSanity;
    public bool IsDepleted { get; private set; }
    public bool IsAnomalyDrainActive => _anomalyDrains.Count > 0;
    public bool IsRedEyeActive => _redEyeEncounters.Count > 0;
    /// <summary>(new value, applied delta)</summary>
    public event Action<int, int> SanityChanged;
    public event Action Depleted;

    private void Awake() { Current = Mathf.Clamp(_startSanity, 0, _maxSanity); }
    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private static bool GameplayActive
    {
        get
        {
            if (Time.timeScale <= 0f) return false;
            var manager = AAH_GameManager.Instance;
            return manager == null || (manager.shiftActive && !manager.shiftEnded);
        }
    }

    private void Update()
    {
        if (IsDepleted || !GameplayActive) return;
        float dt = Time.deltaTime;

        _passiveTimer += dt;
        if (_passiveTimer >= _passiveInterval) { _passiveTimer -= _passiveInterval; ApplyChange(-_passiveAmount); }

        if (IsAnomalyDrainActive)
        {
            _anomalyAccumulator += _anomalyDrainPerSecond * dt;
            int whole = Mathf.FloorToInt(_anomalyAccumulator);
            if (whole > 0) { _anomalyAccumulator -= whole; ApplyChange(-whole); }
        }

        if (IsRedEyeActive)
        {
            _redEyeTimer += dt;
            if (_redEyeTimer >= _redEyeInterval) { _redEyeTimer -= _redEyeInterval; ApplyChange(-_redEyeAmount); }
        }
    }

    /// <summary>Applies a clamped change. Reductions are ignored once depleted. Returns the applied delta.</summary>
    public int ApplyChange(int delta)
    {
        if (delta == 0 || (IsDepleted && delta < 0)) return 0;
        int previous = Current;
        Current = Mathf.Clamp(Current + delta, 0, _maxSanity);
        int applied = Current - previous;
        if (applied == 0) return 0;
        SanityChanged?.Invoke(Current, applied);
        if (Current == 0 && !IsDepleted)
        {
            IsDepleted = true;
            _anomalyDrains.Clear(); _redEyeEncounters.Clear();
            Depleted?.Invoke();
            _onSanityDepleted.Invoke();
        }
        return applied;
    }

    public void StartAnomalyDrain(object source) { if (source != null && !IsDepleted) _anomalyDrains.Add(source); }
    public void StopAnomalyDrain(object source)
    {
        if (source == null) return;
        _anomalyDrains.Remove(source);
        if (!IsAnomalyDrainActive) _anomalyAccumulator = 0f;
    }

    /// <summary>Repeated calls for the same encounter do not create extra timers.</summary>
    public void BeginRedEyeEncounter(object encounter)
    {
        if (encounter == null || IsDepleted) return;
        if (!IsRedEyeActive) _redEyeTimer = 0f;
        _redEyeEncounters.Add(encounter);
    }
    public void EndRedEyeEncounter(object encounter) { if (encounter != null) _redEyeEncounters.Remove(encounter); }
}
