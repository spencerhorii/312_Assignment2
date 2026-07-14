using System;
using UnityEngine;

/// <summary>
/// Tracks which day (Level) the player is currently on. Persists across scene loads.
/// AdvanceDay() will be called later by the player's bed/sleep interaction to progress
/// to the next day. NPCs read CurrentDay to pick their dialogue/delivery/merchant settings
/// for the day.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Tooltip("The day the game starts on.")]
    [SerializeField] private int startingDay = 1;

    public int CurrentDay { get; private set; }

    /// <summary>Fired whenever the day changes (including once on startup with the starting day).</summary>
    public event Action<int> OnDayChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentDay = startingDay;
    }

    private void Start()
    {
        OnDayChanged?.Invoke(CurrentDay);
    }

    /// <summary>Advances to the next day. Intended to be called from the player's sleep/bed interaction.</summary>
    public void AdvanceDay()
    {
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);
    }

    /// <summary>Directly sets the current day - useful for debugging/testing specific days.</summary>
    public void SetDay(int day)
    {
        CurrentDay = day;
        OnDayChanged?.Invoke(CurrentDay);
    }
}