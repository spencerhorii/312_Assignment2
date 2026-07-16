// using System;
// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// Central authority for the player's quest items. Persists across scene loads, same pattern
// /// as InventoryManager/CurrencyManager - but unlike regular inventory, this has NO fixed slot
// /// count and NO "full" state. Quest items always fit; the UI's slot count just grows to match
// /// however many are currently held (with a minimum of 1 empty slot shown when there are none).
// /// </summary>
// public class QuestInventoryManager : MonoBehaviour
// {
//     public static QuestInventoryManager Instance { get; private set; }

//     private readonly List<QuestItemData> items = new List<QuestItemData>();

//     /// <summary>Fired whenever the quest item list changes (added/removed). Passes the full list.</summary>
//     public event Action<List<QuestItemData>> OnQuestInventoryChanged;

//     /// <summary>Fired specifically when a new quest item is added, passing the item and its index in the list.</summary>
//     public event Action<QuestItemData, int> OnQuestItemAdded;

//     public IReadOnlyList<QuestItemData> Items => items;

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

//     /// <summary>Adds a quest item. Always succeeds - quest inventory is never full.</summary>
//     public void AddItem(QuestItemData item)
//     {
//         if (item == null) return;

//         items.Add(item);
//         int index = items.Count - 1;

//         OnQuestInventoryChanged?.Invoke(items);
//         OnQuestItemAdded?.Invoke(item, index);
//     }

//     /// <summary>
//     /// Removes the first matching instance of item (e.g. after a delivery is completed).
//     /// Included now since the Delivery system will need it soon - safe to leave unused until then.
//     /// </summary>
//     public bool RemoveItem(QuestItemData item)
//     {
//         bool removed = items.Remove(item);
//         if (removed) OnQuestInventoryChanged?.Invoke(items);
//         return removed;
//     }

//     public bool HasItem(QuestItemData item) => items.Contains(item);
// }















// using System;
// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// Central authority for the player's quest items. Persists across scene loads, same pattern
// /// as InventoryManager/CurrencyManager - but unlike regular inventory, this has NO fixed slot
// /// count and NO "full" state. Quest items always fit; the UI's slot count just grows to match
// /// however many are currently held (with a minimum of 1 empty slot shown when there are none).
// ///
// /// Removal is two-phase, same reasoning as InventoryManager: TryRemoveItem() validates and
// /// fires OnQuestItemRemovalRequested; ConfirmRemoval() - called by InventoryUI once its
// /// animation finishes - performs the actual removal (List.RemoveAt already reorders naturally).
// /// </summary>
// public class QuestInventoryManager : MonoBehaviour
// {
//     public static QuestInventoryManager Instance { get; private set; }

//     private readonly List<QuestItemData> items = new List<QuestItemData>();

//     /// <summary>Fired whenever the quest item list changes (added/removed). Passes the full list.</summary>
//     public event Action<List<QuestItemData>> OnQuestInventoryChanged;

//     /// <summary>Fired specifically when a new quest item is added, passing the item and its index in the list.</summary>
//     public event Action<QuestItemData, int> OnQuestItemAdded;

//     /// <summary>
//     /// Fired when a removal has been validated (the item exists) but not yet applied to data -
//     /// InventoryUI should play its shrink animation on this slot, then call ConfirmRemoval(index).
//     /// </summary>
//     public event Action<QuestItemData, int> OnQuestItemRemovalRequested;

//     public IReadOnlyList<QuestItemData> Items => items;

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

//     /// <summary>Adds a quest item. Always succeeds - quest inventory is never full.</summary>
//     public void AddItem(QuestItemData item)
//     {
//         if (item == null) return;

//         items.Add(item);
//         int index = items.Count - 1;

//         OnQuestInventoryChanged?.Invoke(items);
//         OnQuestItemAdded?.Invoke(item, index);
//     }

//     /// <summary>
//     /// Validates that the player has this quest item and, if so, fires
//     /// OnQuestItemRemovalRequested so InventoryUI can play its removal animation. Does NOT
//     /// remove it from data yet - call ConfirmRemoval() once the animation finishes. Returns
//     /// false if the item isn't held.
//     /// </summary>
//     public bool TryRemoveItem(QuestItemData item)
//     {
//         if (item == null) return false;

//         int index = items.IndexOf(item);
//         if (index < 0) return false;

//         OnQuestItemRemovalRequested?.Invoke(item, index);
//         return true;
//     }

//     /// <summary>Actually removes the item at the given index. Called by InventoryUI once its removal animation completes.</summary>
//     public void ConfirmRemoval(int index)
//     {
//         if (index < 0 || index >= items.Count) return;

//         items.RemoveAt(index);
//         OnQuestInventoryChanged?.Invoke(items);
//     }

//     public bool HasItem(QuestItemData item) => items.Contains(item);
// }


using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central authority for the player's quest items. Persists across scene loads, same pattern
/// as InventoryManager/CurrencyManager - but unlike regular inventory, this has NO fixed slot
/// count and NO "full" state. Quest items always fit; the UI's slot count just grows to match
/// however many are currently held (with a minimum of 1 empty slot shown when there are none).
///
/// Removal is two-phase, same reasoning as InventoryManager: TryRemoveItem() validates and
/// fires OnQuestItemRemovalRequested; ConfirmRemoval() - called by InventoryUI once its
/// animation finishes - performs the actual removal (List.RemoveAt already reorders naturally).
/// </summary>
public class QuestInventoryManager : MonoBehaviour
{
    public static QuestInventoryManager Instance { get; private set; }

    [SerializeField] private GameData gameData;

    private readonly List<QuestItemData> items = new List<QuestItemData>();

    /// <summary>Fired whenever the quest item list changes (added/removed). Passes the full list.</summary>
    public event Action<List<QuestItemData>> OnQuestInventoryChanged;

    /// <summary>Fired specifically when a new quest item is added, passing the item and its index in the list.</summary>
    public event Action<QuestItemData, int> OnQuestItemAdded;

    /// <summary>
    /// Fired when a removal has been validated (the item exists) but not yet applied to data -
    /// InventoryUI should play its shrink animation on this slot, then call ConfirmRemoval(index).
    /// </summary>
    public event Action<QuestItemData, int> OnQuestItemRemovalRequested;

    public IReadOnlyList<QuestItemData> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameData == null)
        {
            Debug.LogError("[QuestInventoryManager] No GameData assigned.");
        }

        DontDestroyOnLoad(gameObject);

        if (gameData != null)
        {
            items.Clear();
            items.AddRange(gameData.GetQuestInventoryItems());
        }
    }

    /// <summary>Adds a quest item. Always succeeds - quest inventory is never full.</summary>
    public void AddItem(QuestItemData item)
    {
        if (item == null) return;

        items.Add(item);

        gameData.AddQuestItem(item);
        int index = items.Count - 1;

        OnQuestInventoryChanged?.Invoke(items);
        OnQuestItemAdded?.Invoke(item, index);
    }

    /// <summary>
    /// Validates that the player has this quest item and, if so, fires
    /// OnQuestItemRemovalRequested so InventoryUI can play its removal animation. Does NOT
    /// remove it from data yet - call ConfirmRemoval() once the animation finishes. Returns
    /// false if the item isn't held.
    /// </summary>
    public bool TryRemoveItem(QuestItemData item)
    {
        if (item == null) return false;

        int index = items.IndexOf(item);
        if (index < 0) return false;

        OnQuestItemRemovalRequested?.Invoke(item, index);
        return true;
    }

    /// <summary>Actually removes the item at the given index. Called by InventoryUI once its removal animation completes.</summary>
    public void ConfirmRemoval(int index)
    {
        if (index < 0 || index >= items.Count) return;

        QuestItemData removedItem = items[index];

        items.RemoveAt(index);

        if (gameData != null)
        {
            gameData.RemoveQuestItem(removedItem);
        }
        OnQuestInventoryChanged?.Invoke(items);
    }

    public bool HasItem(QuestItemData item) => items.Contains(item);
}