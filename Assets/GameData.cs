using System;
using UnityEngine;

/// <summary>
/// Shared day-tracking data as a ScriptableObject asset. Any script in any scene can
/// reference this same asset directly in the Inspector — no singleton, no DontDestroyOnLoad,
/// no scene-load-order requirements.
///
/// SETUP: Right-click in the Project window -> Create -> Game Data -> Day Data.
/// This creates a .asset file. Drag that SAME asset into the DayData field on every
/// script (in every scene) that needs to read or write the current day.
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "Game Data/Game Data")]
public class GameData : ScriptableObject
{
    [SerializeField] private int startingDay = 1;
    [SerializeField] private int startingMoney = 0;
    [SerializeField] private int startingEnergy = 5;
    [SerializeField] private bool startingAdvance = false;

    public int CurrentDay { get; private set; }
    public int Money {get; private set; }
    public int Energy {get; private set;}
    public bool canAdvance {get; private set;}

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

    public void AdvanceDay()
    {
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);
    }

    public void SetDay(int day)
    {
        CurrentDay = day;
        OnDayChanged?.Invoke(CurrentDay);
    }
    public void addMoney(int moneyAdded)
    {
        Money+= moneyAdded;
    }
    public void resetEnergy()
    {
        Energy = startingEnergy;
    }
    public int getDay()
    {
        return CurrentDay;
    }
}