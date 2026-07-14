using UnityEngine;

/// <summary>
/// Defines a reusable "quest item type" as a ScriptableObject asset, same pattern as ItemData.
/// Quest items are for delivery objectives, not selling - no sellValue/purchasePrice needed.
/// Create via Assets > Create > Inventory > Quest Item Data.
/// </summary>
[CreateAssetMenu(fileName = "NewQuestItem", menuName = "Inventory/Quest Item Data")]
public class QuestItemData : ScriptableObject
{
    [Tooltip("Unique identifier used to reference this quest item in code (delivery checks, save data, etc).")]
    public string itemID;

    [Tooltip("Display name shown in UI.")]
    public string itemName;

    [Tooltip("Icon sprite used both on the world pickup and in the quest inventory UI slot.")]
    public Sprite icon;
}
