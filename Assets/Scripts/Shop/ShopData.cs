using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines what a merchant sells, as a reusable ScriptableObject asset. Referenced by dialogue
/// nodes via their "Shop To Open" field - one ShopData asset can be reused across multiple
/// dialogue nodes/NPCs if they share the same stock.
/// </summary>
[CreateAssetMenu(fileName = "NewShopData", menuName = "Shop/Shop Data")]
public class ShopData : ScriptableObject
{
    [Tooltip("Items available to purchase from this shop, using each item's own ItemData.purchasePrice.")]
    public List<ItemData> itemsForSale = new List<ItemData>();
}
