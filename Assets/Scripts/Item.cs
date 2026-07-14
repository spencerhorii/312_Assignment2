// using UnityEngine;

// /// <summary>
// /// Sits on a world item pickup GameObject. Detects when the player enters/exits an
// /// interaction range trigger, shows a "Pickup (E)" or "Inventory Full" tooltip above
// /// the item (via the shared WorldTooltip component), and hands the item off to
// /// InventoryManager on successful pickup.
// ///
// /// Pickup input is ignored while a dialogue conversation is active, since E is repurposed
// /// for advancing/selecting dialogue during that time.
// ///
// /// Requires the Player GameObject to be tagged "Player".
// /// </summary>
// [RequireComponent(typeof(SpriteRenderer))]
// public class Item : MonoBehaviour
// {
//     [Header("Item Data")]
//     [Tooltip("The ItemData asset this pickup represents. Its icon is applied automatically.")]
//     [SerializeField] private ItemData itemData;
//     public ItemData Data => itemData;

//     [Header("References")]
//     [Tooltip("Sprite renderer for this item in the world. Auto-found on this object if left empty.")]
//     [SerializeField] private SpriteRenderer spriteRenderer;
//     [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
//     [SerializeField] private Collider2D interactionRange;
//     [Tooltip("WorldTooltip component showing 'Pickup (E)' / 'Inventory Full' when the player is in range.")]
//     [SerializeField] private WorldTooltip worldTooltip;

//     private bool playerInRange;

//     private void Awake()
//     {
//         if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
//         if (interactionRange == null) interactionRange = GetComponent<Collider2D>();

//         if (itemData != null && spriteRenderer != null)
//         {
//             spriteRenderer.sprite = itemData.icon;
//         }

//         if (interactionRange != null && !interactionRange.isTrigger)
//         {
//             Debug.LogWarning($"Item '{name}': interactionRange collider should have 'Is Trigger' enabled.");
//         }
//     }

//     private void Update()
//     {
//         if (!playerInRange) return;
//         if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

//         RefreshTooltipText();

//         if (Input.GetKeyDown(KeyCode.E))
//         {
//             TryPickup();
//         }
//     }

//     private void RefreshTooltipText()
//     {
//         if (worldTooltip == null) return;

//         bool inventoryFull = InventoryManager.Instance != null && InventoryManager.Instance.IsFull;
//         worldTooltip.SetText(inventoryFull ? "Inventory Full" : "Pickup (E)");
//     }

//     private void TryPickup()
//     {
//         if (InventoryManager.Instance == null || itemData == null) return;

//         bool added = InventoryManager.Instance.TryAddItem(itemData);
//         if (added)
//         {
//             Destroy(gameObject);
//         }
//         // If not added (inventory full), the tooltip already communicates why - nothing else to do.
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (!other.CompareTag("Player")) return;

//         playerInRange = true;
//         RefreshTooltipText();
//         if (worldTooltip != null) worldTooltip.Show();
//     }

//     private void OnTriggerExit2D(Collider2D other)
//     {
//         if (!other.CompareTag("Player")) return;

//         playerInRange = false;
//         if (worldTooltip != null) worldTooltip.Hide();
//     }
// }

using UnityEngine;

/// <summary>
/// Sits on a world item pickup GameObject. Detects when the player enters/exits an
/// interaction range trigger, shows a "Pickup (E)" or "Inventory Full" tooltip above
/// the item (via the shared WorldTooltip component), and hands the item off to
/// InventoryManager on successful pickup.
///
/// Pickup input is ignored while a dialogue conversation is active, since E is repurposed
/// for advancing/selecting dialogue during that time.
///
/// Requires the Player GameObject to be tagged "Player".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Item : MonoBehaviour
{
    [Header("Item Data")]
    [Tooltip("The ItemData asset this pickup represents. Its icon is applied automatically.")]
    [SerializeField] private ItemData itemData;
    public ItemData Data => itemData;

    [Header("References")]
    [Tooltip("Sprite renderer for this item in the world. Auto-found on this object if left empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
    [SerializeField] private Collider2D interactionRange;
    [Tooltip("WorldTooltip component showing 'Pickup (E)' / 'Inventory Full' when the player is in range.")]
    [SerializeField] private WorldTooltip worldTooltip;

    private bool playerInRange;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (interactionRange == null) interactionRange = GetComponent<Collider2D>();

        if (itemData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }

        if (interactionRange != null && !interactionRange.isTrigger)
        {
            Debug.LogWarning($"Item '{name}': interactionRange collider should have 'Is Trigger' enabled.");
        }
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen) return;

        RefreshTooltipText();

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    private void RefreshTooltipText()
    {
        if (worldTooltip == null) return;

        bool inventoryFull = InventoryManager.Instance != null && InventoryManager.Instance.IsFull;
        worldTooltip.SetText(inventoryFull ? "Inventory Full" : "Pickup (E)");
    }

    private void TryPickup()
    {
        if (InventoryManager.Instance == null || itemData == null) return;

        bool added = InventoryManager.Instance.TryAddItem(itemData);
        if (added)
        {
            Destroy(gameObject);
        }
        // If not added (inventory full), the tooltip already communicates why - nothing else to do.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        RefreshTooltipText();
        if (worldTooltip != null) worldTooltip.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (worldTooltip != null) worldTooltip.Hide();
    }
}