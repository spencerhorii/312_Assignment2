// using System;
// using System.Collections;
// using UnityEngine;

// /// <summary>
// /// Central authority for a shop session: fixed 8-slot Sell grid (mirrors the player's first 8
// /// inventory slots) and 8-slot Purchase grid (from a ShopData asset), 3-column navigation
// /// (Sell / Purchase / Exit) via WASD+arrows, E to select, a Yes/No confirm step, and the
// /// actual buy/sell transactions. Owns no UI directly - fires events for ShopUI to render,
// /// same Manager/UI split as every other system in this project.
// ///
// /// Persistent singleton. Opened by DialogueManager when a node with opensShop is reached.
// /// </summary>
// public class ShopManager : MonoBehaviour
// {
//     private enum ShopColumn { Sell = 0, Purchase = 1, Exit = 2 }
//     private enum ShopPhase { Browsing, Confirming, InsufficientFunds }
//     private enum ConfirmAction { None, Sell, Purchase, Leave }

//     private const int SlotsPerColumn = 8;
//     private const int GridColumns = 2;

//     public static ShopManager Instance { get; private set; }

//     [Header("Blur")]
//     [Tooltip("How strongly the game world blurs behind the shop UI (0 = none, 1 = maximum). Adjustable here.")]
//     [Range(0f, 1f)]
//     [SerializeField] private float blurAmount = 0.5f;

//     [Header("Insufficient Funds")]
//     [Tooltip("How long the 'Insufficient Funds' message stays up before reverting to the normal hover text.")]
//     [SerializeField] private float insufficientFundsMessageDuration = 1.5f;

//     public event Action OnShopOpened;
//     public event Action OnShopClosed;
//     /// <summary>Fired whenever either grid's contents change. Always exactly 8 entries each (may contain nulls for empty slots).</summary>
//     public event Action<ItemData[], ItemData[]> OnSlotsChanged;
//     /// <summary>Fired when the hovered grid slot changes. column: 0=Sell, 1=Purchase, 2=Exit, -1=hidden (during confirm).</summary>
//     public event Action<int, int> OnSelectionChanged;
//     public event Action<string> OnBottomTextChanged;
//     public event Action<bool> OnConfirmOptionsShown;
//     /// <summary>0 = Yes highlighted, 1 = No highlighted.</summary>
//     public event Action<int> OnConfirmSelectionChanged;
//     public event Action<float> OnBlurAmountChanged;

//     public bool IsShopOpen { get; private set; }

//     private ShopData currentShop;
//     private NPC currentNPC;
//     private ShopColumn currentColumn = ShopColumn.Purchase;
//     private int currentIndex;
//     private ShopPhase phase = ShopPhase.Browsing;

//     private ConfirmAction pendingAction;
//     private ItemData pendingItem;
//     private int pendingSlotIndex;
//     private int confirmSelectedIndex;

//     private ItemData[] sellSlots = new ItemData[SlotsPerColumn];
//     private ItemData[] purchaseSlots = new ItemData[SlotsPerColumn];

//     private bool isSubscribedToInventory;
//     private Coroutine insufficientFundsCoroutine;

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     private void Update()
//     {
//         if (!IsShopOpen) return;

//         if (phase == ShopPhase.Browsing)
//         {
//             HandleBrowsingInput();
//         }
//         else if (phase == ShopPhase.Confirming)
//         {
//             HandleConfirmingInput();
//         }
//         // InsufficientFunds phase: no input processed, auto-reverts via coroutine.
//     }

//     // ---------- Opening / closing ----------

//     public void OpenShop(ShopData shop, NPC npc)
//     {
//         if (IsShopOpen || shop == null) return;

//         currentShop = shop;
//         currentNPC = npc;
//         IsShopOpen = true;
//         phase = ShopPhase.Browsing;
//         currentColumn = ShopColumn.Purchase; // per spec: selection starts top-left of the Purchase menu
//         currentIndex = 0;

//         if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = false;

//         SubscribeToInventoryIfNeeded();
//         RefreshSlots();

//         OnBlurAmountChanged?.Invoke(blurAmount);
//         OnShopOpened?.Invoke();
//         UpdateSelectionAndText();
//     }

//     private void CloseShop()
//     {
//         IsShopOpen = false;
//         currentShop = null;

//         if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = true;
//         if (currentNPC != null) currentNPC.OnDialogueEnded(); // re-shows the "Speak (E)" tooltip if still in range
//         currentNPC = null;

//         OnShopClosed?.Invoke();
//     }

//     private void SubscribeToInventoryIfNeeded()
//     {
//         if (isSubscribedToInventory) return;
//         if (InventoryManager.Instance != null)
//         {
//             InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
//             isSubscribedToInventory = true;
//         }
//     }

//     private void HandleInventoryChanged(ItemData[] slots)
//     {
//         if (!IsShopOpen) return;
//         RefreshSlots();
//     }

//     private void RefreshSlots()
//     {
//         sellSlots = new ItemData[SlotsPerColumn];
//         if (InventoryManager.Instance != null)
//         {
//             ItemData[] playerSlots = InventoryManager.Instance.GetSlots();
//             if (playerSlots != null)
//             {
//                 int count = Mathf.Min(SlotsPerColumn, playerSlots.Length);
//                 for (int i = 0; i < count; i++) sellSlots[i] = playerSlots[i];
//             }
//         }

//         purchaseSlots = new ItemData[SlotsPerColumn];
//         if (currentShop != null && currentShop.itemsForSale != null)
//         {
//             int count = Mathf.Min(SlotsPerColumn, currentShop.itemsForSale.Count);
//             for (int i = 0; i < count; i++) purchaseSlots[i] = currentShop.itemsForSale[i];
//         }

//         OnSlotsChanged?.Invoke(sellSlots, purchaseSlots);
//     }

//     // ---------- Browsing input ----------

//     private void HandleBrowsingInput()
//     {
//         if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveHorizontal(-1);
//         else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) MoveHorizontal(1);
//         else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) MoveVertical(-1);
//         else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) MoveVertical(1);
//         else if (Input.GetKeyDown(KeyCode.E)) HandleSelect();
//     }

//     private void MoveHorizontal(int direction)
//     {
//         if (currentColumn == ShopColumn.Exit)
//         {
//             if (direction < 0)
//             {
//                 currentColumn = ShopColumn.Purchase;
//                 currentIndex = 1; // land on the rightmost column of the top row
//             }
//             UpdateSelectionAndText();
//             return;
//         }

//         int col = currentIndex % GridColumns;
//         int row = currentIndex / GridColumns;
//         int newCol = col + direction;

//         if (newCol < 0)
//         {
//             if (currentColumn == ShopColumn.Purchase)
//             {
//                 currentColumn = ShopColumn.Sell;
//                 currentIndex = row * GridColumns + 1;
//             }
//             // already Sell's leftmost column - nowhere further left to go.
//         }
//         else if (newCol > GridColumns - 1)
//         {
//             if (currentColumn == ShopColumn.Sell)
//             {
//                 currentColumn = ShopColumn.Purchase;
//                 currentIndex = row * GridColumns + 0;
//             }
//             else if (currentColumn == ShopColumn.Purchase)
//             {
//                 currentColumn = ShopColumn.Exit;
//                 currentIndex = 0;
//             }
//         }
//         else
//         {
//             currentIndex = row * GridColumns + newCol;
//         }

//         UpdateSelectionAndText();
//     }

//     private void MoveVertical(int direction)
//     {
//         if (currentColumn == ShopColumn.Exit) return; // single slot, no vertical movement

//         int col = currentIndex % GridColumns;
//         int rowCount = SlotsPerColumn / GridColumns;
//         int row = Mathf.Clamp(currentIndex / GridColumns + direction, 0, rowCount - 1);
//         currentIndex = row * GridColumns + col;

//         UpdateSelectionAndText();
//     }

//     private void UpdateSelectionAndText()
//     {
//         OnSelectionChanged?.Invoke((int)currentColumn, currentIndex);

//         string text = "";
//         if (currentColumn == ShopColumn.Sell)
//         {
//             ItemData item = currentIndex < sellSlots.Length ? sellSlots[currentIndex] : null;
//             text = item != null ? item.itemName : "";
//         }
//         else if (currentColumn == ShopColumn.Purchase)
//         {
//             ItemData item = currentIndex < purchaseSlots.Length ? purchaseSlots[currentIndex] : null;
//             text = item != null ? item.itemName : "";
//         }
//         // Exit: no hover text - "Leave?" only appears once selected.

//         OnBottomTextChanged?.Invoke(text);
//     }

//     private void HandleSelect()
//     {
//         if (currentColumn == ShopColumn.Exit)
//         {
//             BeginConfirm(ConfirmAction.Leave, null, currentIndex);
//             return;
//         }

//         ItemData item = currentColumn == ShopColumn.Sell ? sellSlots[currentIndex] : purchaseSlots[currentIndex];
//         if (item == null) return; // empty slot

//         if (currentColumn == ShopColumn.Purchase)
//         {
//             bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(item.purchasePrice);
//             if (!canAfford)
//             {
//                 ShowInsufficientFundsMessage();
//                 return;
//             }
//             BeginConfirm(ConfirmAction.Purchase, item, currentIndex);
//         }
//         else
//         {
//             BeginConfirm(ConfirmAction.Sell, item, currentIndex);
//         }
//     }

//     private void ShowInsufficientFundsMessage()
//     {
//         phase = ShopPhase.InsufficientFunds;
//         OnBottomTextChanged?.Invoke("Insufficient Funds");

//         if (insufficientFundsCoroutine != null) StopCoroutine(insufficientFundsCoroutine);
//         insufficientFundsCoroutine = StartCoroutine(RevertAfterInsufficientFunds());
//     }

//     private IEnumerator RevertAfterInsufficientFunds()
//     {
//         yield return new WaitForSeconds(insufficientFundsMessageDuration);

//         if (phase == ShopPhase.InsufficientFunds)
//         {
//             phase = ShopPhase.Browsing;
//             UpdateSelectionAndText();
//         }
//     }

//     // ---------- Confirm step ----------

//     private void BeginConfirm(ConfirmAction action, ItemData item, int slotIndex)
//     {
//         pendingAction = action;
//         pendingItem = item;
//         pendingSlotIndex = slotIndex;
//         phase = ShopPhase.Confirming;
//         confirmSelectedIndex = 0; // Yes highlighted by default

//         string prompt;
//         if (action == ConfirmAction.Leave) prompt = "Leave?";
//         else if (action == ConfirmAction.Sell) prompt = $"Sell {item.itemName} for ${item.sellValue}?";
//         else prompt = $"Purchase {item.itemName} for ${item.purchasePrice}?";

//         OnBottomTextChanged?.Invoke(prompt);
//         OnSelectionChanged?.Invoke(-1, -1); // hide the grid selection icon - it "moves down to the bottom bubble"
//         OnConfirmOptionsShown?.Invoke(true);
//         OnConfirmSelectionChanged?.Invoke(confirmSelectedIndex);
//     }

//     private void HandleConfirmingInput()
//     {
//         if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
//         {
//             confirmSelectedIndex = 0;
//             OnConfirmSelectionChanged?.Invoke(confirmSelectedIndex);
//         }
//         else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
//         {
//             confirmSelectedIndex = 1;
//             OnConfirmSelectionChanged?.Invoke(confirmSelectedIndex);
//         }
//         else if (Input.GetKeyDown(KeyCode.E))
//         {
//             ConfirmYesNo(confirmSelectedIndex == 0);
//         }
//     }

//     private void ConfirmYesNo(bool yes)
//     {
//         OnConfirmOptionsShown?.Invoke(false);

//         if (yes)
//         {
//             switch (pendingAction)
//             {
//                 case ConfirmAction.Sell:
//                     if (InventoryManager.Instance != null)
//                     {
//                         // Direct removal (not the two-phase TryRemoveItem/animation flow) - the
//                         // shop's own Sell column already shows the transaction happening, so
//                         // triggering InventoryUI's separate removal popup on top would just be
//                         // redundant/conflicting with this modal.
//                         InventoryManager.Instance.ConfirmRemoval(pendingSlotIndex);
//                         if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddCurrency(pendingItem.sellValue);
//                     }
//                     break;

//                 case ConfirmAction.Purchase:
//                     if (InventoryManager.Instance != null && CurrencyManager.Instance != null)
//                     {
//                         bool added = InventoryManager.Instance.TryAddItem(pendingItem);
//                         if (added)
//                         {
//                             CurrencyManager.Instance.SpendCurrency(pendingItem.purchasePrice);
//                         }
//                         // If inventory was full at the moment of confirming (rare - checked at
//                         // select time, not confirm time), the purchase silently doesn't happen
//                         // and currency isn't spent. Not shown to the player as a special message
//                         // currently - straightforward to add later if this comes up in testing.
//                     }
//                     break;

//                 case ConfirmAction.Leave:
//                     CloseShop();
//                     return; // don't fall through to ReturnToBrowsing - shop is now closed
//             }
//         }

//         ReturnToBrowsing();
//     }

//     private void ReturnToBrowsing()
//     {
//         phase = ShopPhase.Browsing;
//         pendingAction = ConfirmAction.None;
//         pendingItem = null;

//         OnSelectionChanged?.Invoke((int)currentColumn, currentIndex);
//         UpdateSelectionAndText();
//     }
// }

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central authority for a shop session: fixed 8-slot Sell grid (mirrors the player's first 8
/// inventory slots) and 8-slot Purchase grid (from a ShopData asset), 3-column navigation
/// (Sell / Purchase / Exit) via WASD+arrows, E to select, a Yes/No confirm step, and the
/// actual buy/sell transactions. Owns no UI directly - fires events for ShopUI to render,
/// same Manager/UI split as every other system in this project.
///
/// Persistent singleton. Opened by DialogueManager when a node with opensShop is reached.
/// </summary>
public class ShopManager : MonoBehaviour
{
    private enum ShopColumn { Sell = 0, Purchase = 1, Exit = 2 }
    private enum ShopPhase { Browsing, Confirming, InsufficientFunds }
    private enum ConfirmAction { None, Sell, Purchase, Leave }

    private const int SlotsPerColumn = 8;
    private const int GridColumns = 2;

    public static ShopManager Instance { get; private set; }

    [Header("Insufficient Funds")]
    [Tooltip("How long the 'Insufficient Funds' message stays up before reverting to the normal hover text.")]
    [SerializeField] private float insufficientFundsMessageDuration = 1.5f;

    public event Action OnShopOpened;
    public event Action OnShopClosed;
    /// <summary>Fired whenever either grid's contents change. Always exactly 8 entries each (may contain nulls for empty slots).</summary>
    public event Action<ItemData[], ItemData[]> OnSlotsChanged;
    /// <summary>Fired when the hovered grid slot changes. column: 0=Sell, 1=Purchase, 2=Exit, -1=hidden (during confirm).</summary>
    public event Action<int, int> OnSelectionChanged;
    public event Action<string> OnBottomTextChanged;
    public event Action<bool> OnConfirmOptionsShown;
    /// <summary>0 = Yes highlighted, 1 = No highlighted.</summary>
    public event Action<int> OnConfirmSelectionChanged;

    public bool IsShopOpen { get; private set; }

    private ShopData currentShop;
    private NPC currentNPC;
    private ShopColumn currentColumn = ShopColumn.Purchase;
    private int currentIndex;
    private ShopPhase phase = ShopPhase.Browsing;

    private ConfirmAction pendingAction;
    private ItemData pendingItem;
    private int pendingSlotIndex;
    private int confirmSelectedIndex;

    private ItemData[] sellSlots = new ItemData[SlotsPerColumn];
    private ItemData[] purchaseSlots = new ItemData[SlotsPerColumn];

    private bool isSubscribedToInventory;
    private Coroutine insufficientFundsCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!IsShopOpen) return;

        if (phase == ShopPhase.Browsing)
        {
            HandleBrowsingInput();
        }
        else if (phase == ShopPhase.Confirming)
        {
            HandleConfirmingInput();
        }
        // InsufficientFunds phase: no input processed, auto-reverts via coroutine.
    }

    // ---------- Opening / closing ----------

    public void OpenShop(ShopData shop, NPC npc)
    {
        if (IsShopOpen || shop == null) return;

        currentShop = shop;
        currentNPC = npc;
        IsShopOpen = true;
        phase = ShopPhase.Browsing;
        currentColumn = ShopColumn.Purchase; // per spec: selection starts top-left of the Purchase menu
        currentIndex = 0;

        if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = false;

        SubscribeToInventoryIfNeeded();
        RefreshSlots();

        OnShopOpened?.Invoke();
        UpdateSelectionAndText();
    }

    private void CloseShop()
    {
        IsShopOpen = false;
        currentShop = null;

        if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = true;
        if (currentNPC != null) currentNPC.OnDialogueEnded(); // re-shows the "Speak (E)" tooltip if still in range
        currentNPC = null;

        OnShopClosed?.Invoke();
    }

    private void SubscribeToInventoryIfNeeded()
    {
        if (isSubscribedToInventory) return;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
            isSubscribedToInventory = true;
        }
    }

    private void HandleInventoryChanged(ItemData[] slots)
    {
        if (!IsShopOpen) return;
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        sellSlots = new ItemData[SlotsPerColumn];
        if (InventoryManager.Instance != null)
        {
            ItemData[] playerSlots = InventoryManager.Instance.GetSlots();
            if (playerSlots != null)
            {
                int count = Mathf.Min(SlotsPerColumn, playerSlots.Length);
                for (int i = 0; i < count; i++) sellSlots[i] = playerSlots[i];
            }
        }

        purchaseSlots = new ItemData[SlotsPerColumn];
        if (currentShop != null && currentShop.itemsForSale != null)
        {
            int count = Mathf.Min(SlotsPerColumn, currentShop.itemsForSale.Count);
            for (int i = 0; i < count; i++) purchaseSlots[i] = currentShop.itemsForSale[i];
        }

        OnSlotsChanged?.Invoke(sellSlots, purchaseSlots);
    }

    // ---------- Browsing input ----------

    private void HandleBrowsingInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveHorizontal(-1);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) MoveHorizontal(1);
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) MoveVertical(-1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) MoveVertical(1);
        else if (Input.GetKeyDown(KeyCode.E)) HandleSelect();
    }

    private void MoveHorizontal(int direction)
    {
        if (currentColumn == ShopColumn.Exit)
        {
            if (direction < 0)
            {
                currentColumn = ShopColumn.Purchase;
                currentIndex = 1; // land on the rightmost column of the top row
            }
            UpdateSelectionAndText();
            return;
        }

        int col = currentIndex % GridColumns;
        int row = currentIndex / GridColumns;
        int newCol = col + direction;

        if (newCol < 0)
        {
            if (currentColumn == ShopColumn.Purchase)
            {
                currentColumn = ShopColumn.Sell;
                currentIndex = row * GridColumns + 1;
            }
            // already Sell's leftmost column - nowhere further left to go.
        }
        else if (newCol > GridColumns - 1)
        {
            if (currentColumn == ShopColumn.Sell)
            {
                currentColumn = ShopColumn.Purchase;
                currentIndex = row * GridColumns + 0;
            }
            else if (currentColumn == ShopColumn.Purchase)
            {
                currentColumn = ShopColumn.Exit;
                currentIndex = 0;
            }
        }
        else
        {
            currentIndex = row * GridColumns + newCol;
        }

        UpdateSelectionAndText();
    }

    private void MoveVertical(int direction)
    {
        if (currentColumn == ShopColumn.Exit) return; // single slot, no vertical movement

        int col = currentIndex % GridColumns;
        int rowCount = SlotsPerColumn / GridColumns;
        int row = Mathf.Clamp(currentIndex / GridColumns + direction, 0, rowCount - 1);
        currentIndex = row * GridColumns + col;

        UpdateSelectionAndText();
    }

    private void UpdateSelectionAndText()
    {
        OnSelectionChanged?.Invoke((int)currentColumn, currentIndex);

        string text = "";
        if (currentColumn == ShopColumn.Sell)
        {
            ItemData item = currentIndex < sellSlots.Length ? sellSlots[currentIndex] : null;
            text = item != null ? item.itemName : "";
        }
        else if (currentColumn == ShopColumn.Purchase)
        {
            ItemData item = currentIndex < purchaseSlots.Length ? purchaseSlots[currentIndex] : null;
            text = item != null ? item.itemName : "";
        }
        // Exit: no hover text - "Leave?" only appears once selected.

        OnBottomTextChanged?.Invoke(text);
    }

    private void HandleSelect()
    {
        if (currentColumn == ShopColumn.Exit)
        {
            BeginConfirm(ConfirmAction.Leave, null, currentIndex);
            return;
        }

        ItemData item = currentColumn == ShopColumn.Sell ? sellSlots[currentIndex] : purchaseSlots[currentIndex];
        if (item == null) return; // empty slot

        if (currentColumn == ShopColumn.Purchase)
        {
            bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(item.purchasePrice);
            if (!canAfford)
            {
                ShowInsufficientFundsMessage();
                return;
            }
            BeginConfirm(ConfirmAction.Purchase, item, currentIndex);
        }
        else
        {
            BeginConfirm(ConfirmAction.Sell, item, currentIndex);
        }
    }

    private void ShowInsufficientFundsMessage()
    {
        phase = ShopPhase.InsufficientFunds;
        OnBottomTextChanged?.Invoke("Insufficient Funds");

        if (insufficientFundsCoroutine != null) StopCoroutine(insufficientFundsCoroutine);
        insufficientFundsCoroutine = StartCoroutine(RevertAfterInsufficientFunds());
    }

    private IEnumerator RevertAfterInsufficientFunds()
    {
        yield return new WaitForSeconds(insufficientFundsMessageDuration);

        if (phase == ShopPhase.InsufficientFunds)
        {
            phase = ShopPhase.Browsing;
            UpdateSelectionAndText();
        }
    }

    // ---------- Confirm step ----------

    private void BeginConfirm(ConfirmAction action, ItemData item, int slotIndex)
    {
        pendingAction = action;
        pendingItem = item;
        pendingSlotIndex = slotIndex;
        phase = ShopPhase.Confirming;
        confirmSelectedIndex = 0; // Yes highlighted by default

        string prompt;
        if (action == ConfirmAction.Leave) prompt = "Leave?";
        else if (action == ConfirmAction.Sell) prompt = $"Sell {item.itemName} for ${item.sellValue}?";
        else prompt = $"Purchase {item.itemName} for ${item.purchasePrice}?";

        OnBottomTextChanged?.Invoke(prompt);
        OnSelectionChanged?.Invoke(-1, -1); // hide the grid selection icon - it "moves down to the bottom bubble"
        OnConfirmOptionsShown?.Invoke(true);
        OnConfirmSelectionChanged?.Invoke(confirmSelectedIndex);
    }

    private void HandleConfirmingInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            confirmSelectedIndex = 0;
            OnConfirmSelectionChanged?.Invoke(confirmSelectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            confirmSelectedIndex = 1;
            OnConfirmSelectionChanged?.Invoke(confirmSelectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            ConfirmYesNo(confirmSelectedIndex == 0);
        }
    }

    private void ConfirmYesNo(bool yes)
    {
        OnConfirmOptionsShown?.Invoke(false);

        if (yes)
        {
            switch (pendingAction)
            {
                case ConfirmAction.Sell:
                    if (InventoryManager.Instance != null)
                    {
                        // Direct removal (not the two-phase TryRemoveItem/animation flow) - the
                        // shop's own Sell column already shows the transaction happening, so
                        // triggering InventoryUI's separate removal popup on top would just be
                        // redundant/conflicting with this modal.
                        InventoryManager.Instance.ConfirmRemoval(pendingSlotIndex);
                        if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddCurrency(pendingItem.sellValue);
                    }
                    break;

                case ConfirmAction.Purchase:
                    if (InventoryManager.Instance != null && CurrencyManager.Instance != null)
                    {
                        bool added = InventoryManager.Instance.TryAddItem(pendingItem);
                        if (added)
                        {
                            CurrencyManager.Instance.SpendCurrency(pendingItem.purchasePrice);
                        }
                        // If inventory was full at the moment of confirming (rare - checked at
                        // select time, not confirm time), the purchase silently doesn't happen
                        // and currency isn't spent. Not shown to the player as a special message
                        // currently - straightforward to add later if this comes up in testing.
                    }
                    break;

                case ConfirmAction.Leave:
                    CloseShop();
                    return; // don't fall through to ReturnToBrowsing - shop is now closed
            }
        }

        ReturnToBrowsing();
    }

    private void ReturnToBrowsing()
    {
        phase = ShopPhase.Browsing;
        pendingAction = ConfirmAction.None;
        pendingItem = null;

        OnSelectionChanged?.Invoke((int)currentColumn, currentIndex);
        UpdateSelectionAndText();
    }
}
