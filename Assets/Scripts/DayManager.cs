// using System;
// using UnityEngine;

// /// <summary>
// /// Tracks which day (Level) the player is currently on. Persists across scene loads.
// /// AdvanceDay() will be called later by the player's bed/sleep interaction to progress
// /// to the next day. NPCs read CurrentDay to pick their dialogue/delivery/merchant settings
// /// for the day.
// /// </summary>
// public class DayManager : MonoBehaviour
// {
//     public static DayManager Instance { get; private set; }

//     [Tooltip("The day the game starts on.")]
//     [SerializeField] private int startingDay = 1;

//     public int CurrentDay { get; private set; }

//     /// <summary>Fired whenever the day changes (including once on startup with the starting day).</summary>
//     public event Action<int> OnDayChanged;

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//         CurrentDay = startingDay;
//     }

//     private void Start()
//     {
//         OnDayChanged?.Invoke(CurrentDay);
//     }

//     /// <summary>Advances to the next day. Intended to be called from the player's sleep/bed interaction.</summary>
//     public void AdvanceDay()
//     {
//         CurrentDay++;
//         OnDayChanged?.Invoke(CurrentDay);
//     }

//     /// <summary>Directly sets the current day - useful for debugging/testing specific days.</summary>
//     public void SetDay(int day)
//     {
//         CurrentDay = day;
//         OnDayChanged?.Invoke(CurrentDay);
//     }
// }


using System;
using UnityEngine;

/// <summary>
/// Compatibility wrapper for older systems that still reference DayManager.
/// This class no longer owns the game's day state.
///
/// Instead, it mirrors the values stored in GameData and forwards GameData's
/// events so older scripts continue working without modification.
///
/// New systems should reference GameData directly.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Shared Game Data")]
    [SerializeField] private GameData gameData;

    public int CurrentDay { get; private set; }

    /// <summary>
    /// Legacy event forwarded from GameData.
    /// </summary>
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

        if (gameData == null)
        {
            Debug.LogError("[DayManager] No GameData asset assigned.");
            return;
        }

        // Synchronize immediately
        CurrentDay = gameData.CurrentDay;
    }

    private void OnEnable()
    {
        if (gameData != null)
            gameData.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        if (gameData != null)
            gameData.OnDayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        if (gameData != null)
        {
            CurrentDay = gameData.CurrentDay;
            OnDayChanged?.Invoke(CurrentDay);
        }
    }

    private void HandleDayChanged(int newDay)
    {
        CurrentDay = newDay;

        // Every new day begins every NPC back at sequence 1.
        gameData.ResetNPCSequences();

        // Notify any legacy listeners.
        OnDayChanged?.Invoke(newDay);
    }

    /// <summary>
    /// Legacy wrapper.
    /// Calls GameData instead of managing its own state.
    /// </summary>
    public void AdvanceDay()
    {
        if (gameData != null)
            gameData.AdvanceDay();
    }

    /// <summary>
    /// Legacy wrapper.
    /// </summary>
    public void SetDay(int day)
    {
        if (gameData != null)
            gameData.SetDay(day);
    }
}