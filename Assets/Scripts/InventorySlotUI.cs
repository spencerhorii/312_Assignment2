using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single inventory slot's visuals: the item icon and its opacity.
/// One of these lives on each instantiated slot prefab under the InventoryUI's slot container.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Tooltip("Image component displaying the item's icon. Should be a child of this slot.")]
    [SerializeField] private Image iconImage;

    public RectTransform RectTransform => (RectTransform)transform;

    /// <summary>Sets (or clears, if item is null) the icon shown in this slot.</summary>
    public void SetItem(ItemData item)
    {
        if (iconImage == null) return;

        if (item != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
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
}
