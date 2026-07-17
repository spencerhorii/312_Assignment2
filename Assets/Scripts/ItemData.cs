// using UnityEngine;

// /// <summary>
// /// Defines a reusable "item type" as a ScriptableObject asset (e.g. Apple.asset, Diamond.asset).
// /// Create these via Assets > Create > Inventory > Item Data.
// ///
// /// World pickups (Item.cs) and merchant stock lists (NPC.purchasableItems) both reference
// /// these assets rather than duplicating item data, so editing sellValue/purchasePrice/icon/name
// /// in one place updates everywhere the item is used.
// /// </summary>
// [CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
// public class ItemData : ScriptableObject
// {
//     [Tooltip("Unique identifier used to reference this item in code (quest checks, save data, etc). " +
//              "Keep this unique across all ItemData assets - uniqueness is not enforced automatically.")]
//     public string itemID;

//     [Tooltip("Display name shown in UI (inventory label, merchant menu, etc).")]
//     public string itemName;

//     [Tooltip("Icon sprite used both on the world pickup and in the inventory UI slot.")]
//     public Sprite icon;

//     [Tooltip("Currency the player receives when selling this item to a merchant.")]
//     public int sellValue;

//     [Tooltip("Currency the player must pay to buy this item from a merchant that stocks it. " +
//              "Only relevant for items listed in an NPC's purchasableItems.")]
//     public int purchasePrice;
// }

using UnityEngine;

/// <summary>
/// Defines a reusable "item type" as a ScriptableObject asset (e.g. Apple.asset, Diamond.asset).
/// Create these via Assets > Create > Inventory > Item Data.
///
/// World pickups (Item.cs) and merchant stock lists (NPC.purchasableItems) both reference
/// these assets rather than duplicating item data, so editing sellValue/purchasePrice/icon/name
/// in one place updates everywhere the item is used.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public enum ItemUpgradeEffect
    {
        None,
        SnowTires,
        Suspension
    }

    [Tooltip("Unique identifier used to reference this item in code (quest checks, save data, etc). " +
             "Keep this unique across all ItemData assets - uniqueness is not enforced automatically.")]
    public string itemID;

    // ... existing fields unchanged ...

    [Header("Special Behavior")]
    [Tooltip("If set, adding this item to the inventory permanently unlocks the matching upgrade in GameData. Leave as None for regular items.")]
    public ItemUpgradeEffect upgradeEffect = ItemUpgradeEffect.None;

    [Tooltip("Whether this item can be sold back at a shop. Uncheck for key items like Snow Tires / Suspension.")]
    public bool canBeSold = true;


    // [Tooltip("Unique identifier used to reference this item in code (quest checks, save data, etc). " +
    //          "Keep this unique across all ItemData assets - uniqueness is not enforced automatically.")]
    // public string itemID;

    [Tooltip("Display name shown in UI (inventory label, merchant menu, etc).")]
    public string itemName;

    [Tooltip("Icon sprite used both on the world pickup and in the inventory UI slot.")]
    public Sprite icon;

    [Tooltip("Currency the player receives when selling this item to a merchant.")]
    public int sellValue;

    [Tooltip("Currency the player must pay to buy this item from a merchant that stocks it. " +
             "Only relevant for items listed in an NPC's purchasableItems.")]
    public int purchasePrice;

    [Tooltip("Sound played when this item is picked up from the world.")]
    public AudioClip pickupSound;
}