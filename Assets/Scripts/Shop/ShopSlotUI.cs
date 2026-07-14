using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single shop grid slot: shows an item's icon and its price (sell value or purchase price,
/// depending on which column it's used for), plus opacity control for the hover state.
/// </summary>
public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI priceText;

    public RectTransform RectTransform => (RectTransform)transform;

    /// <summary>Sets this slot's contents. showSellPrice = true displays item.sellValue, false displays item.purchasePrice.</summary>
    public void SetItem(ItemData item, bool showSellPrice)
    {
        if (item != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
            if (priceText != null)
            {
                priceText.text = (showSellPrice ? item.sellValue : item.purchasePrice).ToString();
                priceText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (priceText != null)
            {
                priceText.text = "";
                priceText.gameObject.SetActive(false);
            }
        }
    }

    public void SetOpacity(float alpha)
    {
        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = alpha;
            iconImage.color = c;
        }
        if (priceText != null)
        {
            Color c = priceText.color;
            c.a = alpha;
            priceText.color = c;
        }
    }
}