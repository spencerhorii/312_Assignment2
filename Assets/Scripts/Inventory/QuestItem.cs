// using UnityEngine;

// /// <summary>
// /// Sits on a world quest item pickup GameObject. Same interaction pattern as Item.cs (range
// /// detection, WorldTooltip, E to pick up), but adds to QuestInventoryManager instead, which
// /// never rejects a pickup for being "full."
// ///
// /// Requires the Player GameObject to be tagged "Player".
// /// </summary>
// [RequireComponent(typeof(SpriteRenderer))]
// public class QuestItem : MonoBehaviour
// {
//     [Header("Quest Item Data")]
//     [Tooltip("The QuestItemData asset this pickup represents. Its icon is applied automatically.")]
//     [SerializeField] private QuestItemData questItemData;
//     public QuestItemData Data => questItemData;

//     [Header("References")]
//     [Tooltip("Sprite renderer for this item in the world. Auto-found on this object if left empty.")]
//     [SerializeField] private SpriteRenderer spriteRenderer;
//     [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
//     [SerializeField] private Collider2D interactionRange;
//     [Tooltip("WorldTooltip component showing 'Pickup (E)' when the player is in range.")]
//     [SerializeField] private WorldTooltip worldTooltip;

//     private bool playerInRange;

//     private void Awake()
//     {
//         if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
//         if (interactionRange == null) interactionRange = GetComponent<Collider2D>();

//         if (questItemData != null && spriteRenderer != null)
//         {
//             spriteRenderer.sprite = questItemData.icon;
//         }

//         if (interactionRange != null && !interactionRange.isTrigger)
//         {
//             Debug.LogWarning($"QuestItem '{name}': interactionRange collider should have 'Is Trigger' enabled.");
//         }

//         if (worldTooltip != null) worldTooltip.SetText("Pickup (E)");
//     }

//     private void Update()
//     {
//         if (!playerInRange) return;
//         if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
//         if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen) return;

//         if (Input.GetKeyDown(KeyCode.E))
//         {
//             TryPickup();
//         }
//     }

//     private void TryPickup()
//     {
//         if (QuestInventoryManager.Instance == null || questItemData == null) return;

//         QuestInventoryManager.Instance.AddItem(questItemData);
//         Destroy(gameObject);
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (!other.CompareTag("Player")) return;

//         playerInRange = true;
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
/// Sits on a world quest item pickup GameObject. Same interaction pattern as Item.cs (range
/// detection, WorldTooltip, E to pick up), but adds to QuestInventoryManager instead, which
/// never rejects a pickup for being "full."
///
/// Requires the Player GameObject to be tagged "Player".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class QuestItem : MonoBehaviour
{
    [Header("Quest Item Data")]
    [Tooltip("The QuestItemData asset this pickup represents. Its icon is applied automatically.")]
    [SerializeField] private QuestItemData questItemData;
    public QuestItemData Data => questItemData;

    [Header("References")]
    [Tooltip("Sprite renderer for this item in the world. Auto-found on this object if left empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
    [SerializeField] private Collider2D interactionRange;
    [Tooltip("WorldTooltip component showing 'Pickup (E)' when the player is in range.")]
    [SerializeField] private WorldTooltip worldTooltip;

    private bool playerInRange;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (interactionRange == null) interactionRange = GetComponent<Collider2D>();

        if (questItemData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = questItemData.icon;
        }

        if (interactionRange != null && !interactionRange.isTrigger)
        {
            Debug.LogWarning($"QuestItem '{name}': interactionRange collider should have 'Is Trigger' enabled.");
        }

        if (worldTooltip != null) worldTooltip.SetText("Pickup (E)");
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    private void TryPickup()
    {
        if (QuestInventoryManager.Instance == null || questItemData == null) return;

        QuestInventoryManager.Instance.AddItem(questItemData);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUISound(questItemData.pickupSound);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (worldTooltip != null) worldTooltip.Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (worldTooltip != null) worldTooltip.Hide();
    }
}