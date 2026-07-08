// using UnityEngine;
// using TMPro;

// /// <summary>
// /// Sits on a world item pickup GameObject. Detects when the player enters/exits an
// /// interaction range trigger, shows a "Pickup (E)" or "Inventory Full" tooltip above
// /// the item, and hands the item off to InventoryManager on successful pickup.
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

//     [Tooltip("World-space TextMeshPro (3D text, not UI) positioned above the item, used for the tooltip.")]
//     [SerializeField] private TextMeshPro tooltipText;

//     [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
//     [SerializeField] private Collider2D interactionRange;

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

//         if (tooltipText != null)
//         {
//             tooltipText.gameObject.SetActive(false);
//         }
//     }

//     private void Update()
//     {
//         if (!playerInRange) return;

//         RefreshTooltip();

//         if (Input.GetKeyDown(KeyCode.E))
//         {
//             TryPickup();
//         }
//     }

//     private void RefreshTooltip()
//     {
//         if (tooltipText == null) return;

//         bool inventoryFull = InventoryManager.Instance != null && InventoryManager.Instance.IsFull;
//         tooltipText.text = inventoryFull ? "Inventory Full" : "Pickup (E)";
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
//         if (tooltipText != null)
//         {
//             tooltipText.gameObject.SetActive(true);
//         }
//         RefreshTooltip();
//     }

//     private void OnTriggerExit2D(Collider2D other)
//     {
//         if (!other.CompareTag("Player")) return;

//         playerInRange = false;
//         if (tooltipText != null)
//         {
//             tooltipText.gameObject.SetActive(false);
//         }
//     }
// }


using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Sits on a world item pickup GameObject. Detects when the player enters/exits an
/// interaction range trigger, shows a "Pickup (E)" or "Inventory Full" tooltip above
/// the item with an eased scale-in/out animation, and hands the item off to
/// InventoryManager on successful pickup.
///
/// While fully visible, the tooltip gently oscillates up and down (a subtle "rocking"
/// motion) for as long as the player stays in range. The oscillation only starts once
/// the scale-in animation has finished, and stops immediately when the tooltip begins
/// scaling back out.
///
/// Requires the Player GameObject to be tagged "Player".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Item : MonoBehaviour
{
    private enum TooltipState { Hidden, ScalingIn, Visible, ScalingOut }

    [Header("Item Data")]
    [Tooltip("The ItemData asset this pickup represents. Its icon is applied automatically.")]
    [SerializeField] private ItemData itemData;
    public ItemData Data => itemData;

    [Header("References")]
    [Tooltip("Sprite renderer for this item in the world. Auto-found on this object if left empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("World-space TextMeshPro (3D text, not UI) positioned above the item, used for the tooltip.")]
    [SerializeField] private TextMeshPro tooltipText;

    [Tooltip("Trigger collider defining the interaction range. Must have 'Is Trigger' enabled.")]
    [SerializeField] private Collider2D interactionRange;

    [Header("Tooltip Scale Animation")]
    [Tooltip("Duration in seconds for the tooltip to scale in or out.")]
    [SerializeField] private float tooltipScaleDuration = 0.15f;
    [Tooltip("Easing curve used for the scale-in/out animation (0->1 over normalized time).")]
    [SerializeField] private AnimationCurve tooltipScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("The scale the tooltip animates to when fully visible.")]
    [SerializeField] private Vector3 tooltipMaxScale = Vector3.one;

    [Header("Tooltip Oscillation")]
    [Tooltip("How far up/down (in local units) the tooltip rocks from its base position.")]
    [SerializeField] private float oscillationAmplitude = 0.05f;
    [Tooltip("How fast the tooltip oscillates, in full up-down cycles per second.")]
    [SerializeField] private float oscillationSpeed = 1f;

    private bool playerInRange;
    private TooltipState tooltipState = TooltipState.Hidden;
    private Vector3 tooltipBaseLocalPosition;
    private Coroutine tooltipScaleCoroutine;
    private Coroutine tooltipOscillateCoroutine;

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

        if (tooltipText != null)
        {
            tooltipBaseLocalPosition = tooltipText.transform.localPosition;
            tooltipText.transform.localScale = Vector3.zero;
            tooltipText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        RefreshTooltipText();

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    private void RefreshTooltipText()
    {
        if (tooltipText == null) return;

        bool inventoryFull = InventoryManager.Instance != null && InventoryManager.Instance.IsFull;
        tooltipText.text = inventoryFull ? "Inventory Full" : "Pickup (E)";
    }

    private void TryPickup()
    {
        if (InventoryManager.Instance == null || itemData == null) return;

        bool added = InventoryManager.Instance.TryAddItem(itemData);
        if (added)
        {
            Destroy(gameObject); // tooltip and its coroutines are destroyed along with this object
        }
        // If not added (inventory full), the tooltip already communicates why - nothing else to do.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        RefreshTooltipText();
        ShowTooltip();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        HideTooltip();
    }

    // ---------- Tooltip animation ----------

    private void ShowTooltip()
    {
        if (tooltipText == null) return;

        if (tooltipScaleCoroutine != null) StopCoroutine(tooltipScaleCoroutine);
        StopOscillation();

        tooltipText.gameObject.SetActive(true);
        tooltipState = TooltipState.ScalingIn;
        tooltipScaleCoroutine = StartCoroutine(ScaleTooltip(tooltipMaxScale, TooltipState.Visible));
    }

    private void HideTooltip()
    {
        if (tooltipText == null) return;

        if (tooltipScaleCoroutine != null) StopCoroutine(tooltipScaleCoroutine);
        StopOscillation();

        // Reset to base position in case we're interrupting mid-oscillation, so the
        // scale-out shrinks cleanly from the resting position rather than an offset one.
        tooltipText.transform.localPosition = tooltipBaseLocalPosition;

        tooltipState = TooltipState.ScalingOut;
        tooltipScaleCoroutine = StartCoroutine(ScaleTooltip(Vector3.zero, TooltipState.Hidden));
    }

    private IEnumerator ScaleTooltip(Vector3 targetScale, TooltipState endState)
    {
        Transform tooltipTransform = tooltipText.transform;
        Vector3 startScale = tooltipTransform.localScale;
        float elapsed = 0f;

        while (elapsed < tooltipScaleDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / tooltipScaleDuration);
            float curved = tooltipScaleCurve.Evaluate(normalized);
            tooltipTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curved);
            yield return null;
        }

        tooltipTransform.localScale = targetScale;
        tooltipState = endState;

        if (endState == TooltipState.Visible)
        {
            tooltipOscillateCoroutine = StartCoroutine(OscillateTooltip());
        }
        else if (endState == TooltipState.Hidden)
        {
            tooltipText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gently rocks the tooltip up and down around its base local position using a sine wave,
    /// which is inherently eased (smooth acceleration/deceleration at each peak). Runs
    /// indefinitely until stopped by HideTooltip() or another ShowTooltip() call.
    /// </summary>
    private IEnumerator OscillateTooltip()
    {
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            float offsetY = Mathf.Sin(elapsed * oscillationSpeed * Mathf.PI * 2f) * oscillationAmplitude;
            tooltipText.transform.localPosition = tooltipBaseLocalPosition + new Vector3(0f, offsetY, 0f);
            yield return null;
        }
    }

    private void StopOscillation()
    {
        if (tooltipOscillateCoroutine != null)
        {
            StopCoroutine(tooltipOscillateCoroutine);
            tooltipOscillateCoroutine = null;
        }
    }
}