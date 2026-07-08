using System;
using UnityEngine;

/// <summary>
/// Central authority for the player's currency. Persists across scene loads so
/// money carries through teleport gates into new rooms.
///
/// Other systems never touch the UI directly and never store their own copy of the
/// currency value � they call CurrencyManager.Instance.AddCurrency()/SpendCurrency(),
/// and anything that needs to react to changes (like CurrencyUI, or a Merchant UI
/// that needs to grey out unaffordable items) subscribes to OnCurrencyChanged.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Values")]
    [Tooltip("Currency the player starts the game with. Editable in Inspector.")]
    [SerializeField] private int startingCurrency = 50;

    /// <summary>Current currency amount. Read-only from outside; modify via AddCurrency/SpendCurrency.</summary>
    public int CurrentCurrency { get; private set; }

    /// <summary>Fired whenever currency changes, passing the new total. UI and other listeners subscribe to this.</summary>
    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        // Simple persistent singleton so currency survives scene transitions.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentCurrency = startingCurrency;
    }

    private void Start()
    {
        // Fire once on startup so any UI already in the scene initializes correctly.
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }

    /// <summary>Adds currency (e.g. from selling an item to a merchant).</summary>
    public void AddCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"AddCurrency called with negative amount ({amount}). Use SpendCurrency instead.");
            return;
        }

        CurrentCurrency += amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }

    /// <summary>
    /// Attempts to spend currency (e.g. buying from a merchant).
    /// Returns true if the player could afford it and the spend succeeded, false otherwise.
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"SpendCurrency called with negative amount ({amount}).");
            return false;
        }

        if (CurrentCurrency < amount)
        {
            return false; // not enough funds - calling UI should show feedback
        }

        CurrentCurrency -= amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
        return true;
    }

    /// <summary>Convenience check for UI (e.g. graying out items the player can't afford).</summary>
    public bool CanAfford(int amount) => CurrentCurrency >= amount;
}