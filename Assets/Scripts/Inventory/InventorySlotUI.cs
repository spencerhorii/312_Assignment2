// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// Controls a single inventory slot's visuals: the item icon and its opacity. Used by both
// /// the regular Items page and the Quest Items page in InventoryUI - accepts either ItemData
// /// or QuestItemData via overloads, both of which just resolve down to a Sprite.
// /// </summary>
// public class InventorySlotUI : MonoBehaviour
// {
//     [Tooltip("Image component displaying the item's icon. Should be a child of this slot.")]
//     [SerializeField] private Image iconImage;

//     public RectTransform RectTransform => (RectTransform)transform;

//     /// <summary>Sets (or clears, if item is null) the icon shown in this slot, for a regular item.</summary>
//     public void SetItem(ItemData item)
//     {
//         SetIcon(item != null ? item.icon : null);
//     }

//     /// <summary>Sets (or clears, if item is null) the icon shown in this slot, for a quest item.</summary>
//     public void SetItem(QuestItemData item)
//     {
//         SetIcon(item != null ? item.icon : null);
//     }

//     /// <summary>Clears this slot (shows no icon). Used for the Quest page's "empty" placeholder slot.</summary>
//     public void Clear()
//     {
//         SetIcon(null);
//     }

//     private void SetIcon(Sprite icon)
//     {
//         if (iconImage == null) return;

//         if (icon != null)
//         {
//             iconImage.sprite = icon;
//             iconImage.enabled = true;
//         }
//         else
//         {
//             iconImage.sprite = null;
//             iconImage.enabled = false;
//         }
//     }

//     /// <summary>Sets the icon's opacity (used for the hovered/dimmed visual states).</summary>
//     public void SetOpacity(float alpha)
//     {
//         if (iconImage == null) return;

//         Color c = iconImage.color;
//         c.a = alpha;
//         iconImage.color = c;
//     }
// }

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single inventory slot's visuals: the item icon, its opacity, and (new) a
/// grow-then-collapse removal animation used when an item is being taken away via a dialogue
/// delivery action. Used by both the regular Items page and the Quest Items page in InventoryUI.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Tooltip("Image component displaying the item's icon. Should be a child of this slot.")]
    [SerializeField] private Image iconImage;

    public RectTransform RectTransform => (RectTransform)transform;

    private Coroutine removalCoroutine;

    /// <summary>Sets (or clears, if item is null) the icon shown in this slot, for a regular item.</summary>
    public void SetItem(ItemData item)
    {
        SetIcon(item != null ? item.icon : null);
    }

    /// <summary>Sets (or clears, if item is null) the icon shown in this slot, for a quest item.</summary>
    public void SetItem(QuestItemData item)
    {
        SetIcon(item != null ? item.icon : null);
    }

    /// <summary>Clears this slot (shows no icon). Used for the Quest page's "empty" placeholder slot.</summary>
    public void Clear()
    {
        SetIcon(null);
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    /// <summary>Sets the icon's opacity (used for the hovered/dimmed visual states).</summary>
    public void SetOpacity(float alpha)
    {
        if (iconImage == null) return;

        Color c = iconImage.color;
        c.a = alpha;
        iconImage.color = c;
    }

    /// <summary>
    /// Plays the "being removed" animation: eases up to growScale, then eases down to zero.
    /// Calls onComplete once fully collapsed, so the caller can finalize the actual data removal
    /// (which will typically destroy/rebuild this slot object shortly after anyway).
    /// </summary>
    public void PlayRemovalAnimation(float growScale, float duration, AnimationCurve curve, System.Action onComplete)
    {
        if (removalCoroutine != null) StopCoroutine(removalCoroutine);
        removalCoroutine = StartCoroutine(RemovalRoutine(growScale, duration, curve, onComplete));
    }

    private IEnumerator RemovalRoutine(float growScale, float duration, AnimationCurve curve, System.Action onComplete)
    {
        RectTransform rect = RectTransform;
        Vector3 baseScale = rect.localScale;
        Vector3 grownScale = baseScale * growScale;

        // Split the duration: a smaller portion easing up to the grown size, the rest easing
        // down to zero - matches "expand slightly, then collapse until no longer visible."
        yield return LerpScale(rect, baseScale, grownScale, duration * 0.35f, curve);
        yield return LerpScale(rect, grownScale, Vector3.zero, duration * 0.65f, curve);

        removalCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator LerpScale(RectTransform rect, Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            rect.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }
        rect.localScale = to;
    }
}