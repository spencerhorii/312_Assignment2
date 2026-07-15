using System;
using UnityEngine;

/// <summary>
/// Shared game-state data as a ScriptableObject asset. Any script in any scene can
/// reference this same asset directly in the Inspector — no singleton, no DontDestroyOnLoad,
/// no scene-load-order requirements.
///
/// SETUP: Right-click in the Project window -> Create -> Game Data -> Game Data.
/// This creates a .asset file. Drag that SAME asset into the GameData field on every
/// script (in every scene) that needs to read or write shared state.
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "Game Data/Game Data")]
public class GameData : ScriptableObject
{
    [SerializeField] private int startingDay = 1;
    [SerializeField] private int startingMoney = 0;
    [SerializeField] private int startingEnergy = 5;

    [Tooltip("canAdvance acts as a LOCK: true = day advancement is BLOCKED (initial task not done yet), " +
             "false = UNBLOCKED (advancing is allowed). TaskManager flips this each day.")]
    [SerializeField] private bool startingAdvance = true;

    public int CurrentDay { get; private set; }
    public int Money { get; private set; }
    public int Energy { get; private set; }

    /// <summary>
    /// true = advancing is currently BLOCKED (required task not completed yet).
    /// false = advancing is allowed. See tooltip above for why this reads "backwards".
    /// </summary>
    public bool canAdvance { get; private set; }

    /// <summary>
    /// Fired whenever the day changes, passing the new day number.
    /// </summary>
    public event Action<int> OnDayChanged;

    /// <summary>
    /// IMPORTANT: ScriptableObject assets persist their values in the Editor between
    /// play sessions (they're real files on disk, not scene objects). OnEnable runs
    /// each time you enter Play Mode, so we reset here to avoid stale data from a
    /// previous session carrying over.
    /// </summary>
    private void OnEnable()
    {
        CurrentDay = startingDay;
        Money = startingMoney;
        Energy = startingEnergy;
        canAdvance = startingAdvance;
    }

    /// <summary>
    /// Advances to the next day — but only if advancing isn't currently locked
    /// (i.e. only once the day's required initial task has been completed).
    /// </summary>
    public void AdvanceDay()
    {
        if (canAdvance)
        {
            Debug.LogWarning("[GameData] Cannot advance day yet — required task not completed.");
            return;
        }

        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);
    }

    /// <summary>
    /// Jumps to a specific day directly (e.g. loading a save file). Bypasses the
    /// canAdvance lock, since this isn't a "player pressed advance" action.
    /// </summary>
    public void SetDay(int day)
    {
        CurrentDay = day;
        OnDayChanged?.Invoke(CurrentDay);
    }

    /// <summary>
    /// Sets the advance lock. Called by TaskManager: true when a new day's initial
    /// task is assigned (locking advancement), false once that task is completed.
    /// </summary>
    public void SetCanAdvance(bool value)
    {
        canAdvance = value;
    }

    public void addMoney(int moneyAdded)
    {
        Money += moneyAdded;
    }

    public void resetEnergy()
    {
        Energy = startingEnergy;
    }

    public int getDay()
    {
        return CurrentDay;
    }
    public int getMoney()
    {
        return Money;
    }
}