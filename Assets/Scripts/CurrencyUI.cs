using UnityEngine;
using TMPro;

/// <summary>
/// Displays the player's current currency in the UI (coin icon + number).
/// Purely a "view" - it never modifies currency itself, only listens for changes
/// from CurrencyManager. Attach to the CurrencyUI object under your Canvas.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Text element that displays the numeric currency value.")]
    [SerializeField] private TextMeshProUGUI currencyText;

    [Tooltip("Optional: the coin icon Image, exposed in case you want to animate/flash it later.")]
    [SerializeField] private UnityEngine.UI.Image coinIcon;
    [SerializeField] private GameData gameData;

    private void Start()
    {
        // currencyText.text = gameData.getMoney().ToString();
        currencyText.text = gameData.GetCurrency().ToString();
    }

    private void Update()
    {
        // currencyText.text = gameData.getMoney().ToString();
        currencyText.text = gameData.GetCurrency().ToString();
    }

    // private bool isSubscribed;

    // private void Start()
    // {
    //     // Subscribing in Start() (rather than OnEnable/Awake) guarantees CurrencyManager.Awake()
    //     // has already run and set Instance, since Unity runs ALL Awake() calls in the scene
    //     // before ANY Start() call. This avoids a script-execution-order race.
    //     TrySubscribe();
    // }

    // private void OnEnable()
    // {
    //     // Covers the case where this object is disabled and re-enabled later
    //     // (e.g. if the currency UI panel gets toggled off/on at runtime).
    //     TrySubscribe();
    // }

    // private void OnDisable()
    // {
    //     if (CurrencyManager.Instance != null && isSubscribed)
    //     {
    //         CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    //         isSubscribed = false;
    //     }
    // }

    // private void TrySubscribe()
    // {
    //     if (isSubscribed) return;

    //     if (CurrencyManager.Instance == null)
    //     {
    //         Debug.LogWarning("CurrencyUI: CurrencyManager.Instance is null. " +
    //                           "Make sure a GameManagers object with CurrencyManager exists and is active in the scene.");
    //         return;
    //     }

    //     CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
    //     isSubscribed = true;

    //     // Initialize immediately with the current value.
    //     HandleCurrencyChanged(CurrencyManager.Instance.CurrentCurrency);
    // }

    // private void HandleCurrencyChanged(int newAmount)
    // {
    //     if (currencyText != null)
    //     {
    //         currencyText.text = newAmount.ToString();
    //     }
    //     else
    //     {
    //         Debug.LogWarning("CurrencyUI: currencyText is not assigned in the Inspector.");
    //     }
    // }
}