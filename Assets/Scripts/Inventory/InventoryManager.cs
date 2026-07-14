// using System;
// using UnityEngine;

// /// <summary>
// /// Central authority for the player's inventory. Persists across scene loads, same pattern
// /// as CurrencyManager. Owns the actual slot array - nothing else stores inventory state
// /// directly. UI and world items call TryAddItem() / read GetSlots() and react to events.
// /// </summary>
// public class InventoryManager : MonoBehaviour
// {
//     public static InventoryManager Instance { get; private set; }

//     private ItemData[] slots;

//     /// <summary>True once Initialize() has been called (by PlayerController2D) with a slot count.</summary>
//     public bool IsInitialized { get; private set; }

//     /// <summary>Current number of inventory slots.</summary>
//     public int SlotCount => slots != null ? slots.Length : 0;

//     /// <summary>Fired whenever the slot contents OR slot count change. Passes the full slot array.</summary>
//     public event Action<ItemData[]> OnInventoryChanged;

//     /// <summary>Fired specifically when a new item is added, passing the item and the slot index it landed in.
//     /// Used by InventoryUI to trigger the "pickup preview" popup.</summary>
//     public event Action<ItemData, int> OnItemAdded;

//     /// <summary>True when every slot is occupied.</summary>
//     public bool IsFull
//     {
//         get
//         {
//             if (!IsInitialized) return false;
//             foreach (var slot in slots)
//             {
//                 if (slot == null) return false;
//             }
//             return true;
//         }
//     }

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

//     /// <summary>
//     /// Sets up the slot array with the given starting size. Called once by PlayerController2D
//     /// using its exposed inventorySlots field. Guarded so re-entering a scene doesn't wipe an
//     /// existing inventory. To change the slot count later at runtime (e.g. a bag upgrade item),
//     /// call SetSlotCount() instead - that's allowed to run any number of times.
//     /// </summary>
//     public void Initialize(int slotCount)
//     {
//         if (IsInitialized) return;
//         SetSlotCount(slotCount);
//     }

//     /// <summary>
//     /// Resizes the inventory to the given slot count at any point during gameplay (e.g. a bag
//     /// upgrade pickup increasing capacity from 4 to 5). Existing items are preserved in their
//     /// original slot positions; if shrinking below the number of currently held items, items
//     /// in slots beyond the new size are dropped (design note: you likely want to prevent
//     /// shrinking below the current item count at the call site once selling/dropping exists).
//     /// </summary>
//     public void SetSlotCount(int newSlotCount)
//     {
//         if (newSlotCount < 0)
//         {
//             Debug.LogWarning($"InventoryManager: SetSlotCount called with negative value ({newSlotCount}).");
//             return;
//         }

//         ItemData[] newSlots = new ItemData[newSlotCount];

//         if (slots != null)
//         {
//             int copyCount = Mathf.Min(slots.Length, newSlotCount);
//             for (int i = 0; i < copyCount; i++)
//             {
//                 newSlots[i] = slots[i];
//             }
//         }

//         slots = newSlots;
//         IsInitialized = true;
//         OnInventoryChanged?.Invoke(slots);
//     }

//     /// <summary>
//     /// Attempts to add an item to the first available slot. Returns false if the inventory is full
//     /// (or not yet initialized), true if the item was successfully added.
//     /// </summary>
//     public bool TryAddItem(ItemData item)
//     {
//         if (!IsInitialized || item == null) return false;

//         for (int i = 0; i < slots.Length; i++)
//         {
//             if (slots[i] == null)
//             {
//                 slots[i] = item;
//                 OnInventoryChanged?.Invoke(slots);
//                 OnItemAdded?.Invoke(item, i);
//                 return true;
//             }
//         }

//         return false; // full
//     }

//     /// <summary>Returns the current slot array. Treat as read-only - modify via TryAddItem/RemoveItemAt/SetSlotCount only.</summary>
//     public ItemData[] GetSlots() => slots;

//     /// <summary>
//     /// Removes an item at a specific slot index (e.g. after selling to a merchant). Included now
//     /// since the Merchant system will need it soon - safe to leave unused until then.
//     /// </summary>
//     public bool RemoveItemAt(int slotIndex)
//     {
//         if (!IsInitialized || slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null)
//         {
//             return false;
//         }

//         slots[slotIndex] = null;
//         OnInventoryChanged?.Invoke(slots);
//         return true;
//     }
// }

using System;
using UnityEngine;

/// <summary>
/// Central authority for the player's inventory. Persists across scene loads, same pattern
/// as CurrencyManager. Owns the actual slot array - nothing else stores inventory state
/// directly. UI and world items call TryAddItem() / read GetSlots() and react to events.
///
/// Removal is two-phase, so InventoryUI can play a shrink animation before the item actually
/// disappears from data: TryRemoveItem() validates the item exists and fires
/// OnItemRemovalRequested (it does NOT touch the slot array yet); ConfirmRemoval() - called by
/// InventoryUI once its animation finishes - performs the actual removal and reorders the
/// remaining items to fill the gap.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private ItemData[] slots;

    /// <summary>True once Initialize() has been called (by PlayerController2D) with a slot count.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Current number of inventory slots.</summary>
    public int SlotCount => slots != null ? slots.Length : 0;

    /// <summary>Fired whenever the slot contents OR slot count change. Passes the full slot array.</summary>
    public event Action<ItemData[]> OnInventoryChanged;

    /// <summary>Fired specifically when a new item is added, passing the item and the slot index it landed in.</summary>
    public event Action<ItemData, int> OnItemAdded;

    /// <summary>
    /// Fired when a removal has been validated (the item exists) but not yet applied to data -
    /// InventoryUI should play its shrink animation on this slot, then call ConfirmRemoval(index).
    /// </summary>
    public event Action<ItemData, int> OnItemRemovalRequested;

    /// <summary>True when every slot is occupied.</summary>
    public bool IsFull
    {
        get
        {
            if (!IsInitialized) return false;
            foreach (var slot in slots)
            {
                if (slot == null) return false;
            }
            return true;
        }
    }

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

    /// <summary>
    /// Sets up the slot array with the given starting size. Called once by PlayerController2D
    /// using its exposed inventorySlots field. Guarded so re-entering a scene doesn't wipe an
    /// existing inventory. To change the slot count later at runtime, call SetSlotCount() instead.
    /// </summary>
    public void Initialize(int slotCount)
    {
        if (IsInitialized) return;
        SetSlotCount(slotCount);
    }

    /// <summary>Resizes the inventory to the given slot count at any point during gameplay, preserving existing items.</summary>
    public void SetSlotCount(int newSlotCount)
    {
        if (newSlotCount < 0)
        {
            Debug.LogWarning($"InventoryManager: SetSlotCount called with negative value ({newSlotCount}).");
            return;
        }

        ItemData[] newSlots = new ItemData[newSlotCount];

        if (slots != null)
        {
            int copyCount = Mathf.Min(slots.Length, newSlotCount);
            for (int i = 0; i < copyCount; i++)
            {
                newSlots[i] = slots[i];
            }
        }

        slots = newSlots;
        IsInitialized = true;
        OnInventoryChanged?.Invoke(slots);
    }

    /// <summary>
    /// Attempts to add an item to the first available slot. Returns false if the inventory is full
    /// (or not yet initialized), true if the item was successfully added.
    /// </summary>
    public bool TryAddItem(ItemData item)
    {
        if (!IsInitialized || item == null) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                OnInventoryChanged?.Invoke(slots);
                OnItemAdded?.Invoke(item, i);
                return true;
            }
        }

        return false; // full
    }

    /// <summary>
    /// Validates that the player has this item and, if so, fires OnItemRemovalRequested so
    /// InventoryUI can play its removal animation. Does NOT remove the item from data yet -
    /// call ConfirmRemoval() once the animation finishes. Returns false if the item isn't held
    /// (the caller should treat this as "player doesn't have the item").
    /// </summary>
    public bool TryRemoveItem(ItemData item)
    {
        if (!IsInitialized || item == null) return false;

        int index = System.Array.IndexOf(slots, item);
        if (index < 0) return false;

        OnItemRemovalRequested?.Invoke(item, index);
        return true;
    }

    /// <summary>
    /// Actually removes the item at the given slot index and shifts remaining items down to
    /// fill the gap (reorder). Called by InventoryUI once its removal animation completes.
    /// </summary>
    public void ConfirmRemoval(int index)
    {
        if (!IsInitialized || index < 0 || index >= slots.Length) return;

        for (int i = index; i < slots.Length - 1; i++)
        {
            slots[i] = slots[i + 1];
        }
        slots[slots.Length - 1] = null;

        OnInventoryChanged?.Invoke(slots);
    }

    /// <summary>Returns the current slot array. Treat as read-only - modify via TryAddItem/TryRemoveItem/SetSlotCount only.</summary>
    public ItemData[] GetSlots() => slots;
}