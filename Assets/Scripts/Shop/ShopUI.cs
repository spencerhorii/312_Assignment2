// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// /// <summary>
// /// Renders the shop UI by subscribing to ShopManager's events - purely a view layer, same
// /// split as every other Manager/UI pair in this project. Builds the 8 fixed slots per column
// /// once (never rebuilt, since the shop grid size never changes), moves a single selection icon
// /// between grid slots, and toggles a separate confirm-step Yes/No cursor for the bottom bubble.
// /// </summary>
// public class ShopUI : MonoBehaviour
// {
//     [Header("Blur")]
//     [Tooltip("Captures/clears the background blur snapshot when the shop opens/closes.")]
//     [SerializeField] private ScreenBlur screenBlur;

//     [Header("Panel")]
//     [SerializeField] private CanvasGroup panelCanvasGroup;
//     [SerializeField] private float fadeDuration = 0.2f;

//     [Header("Sell / Purchase Columns")]
//     [Tooltip("Container with a Grid Layout Group (2 columns x 4 rows) for the player's sellable items.")]
//     [SerializeField] private RectTransform sellSlotsContainer;
//     [Tooltip("Container with a Grid Layout Group (2 columns x 4 rows) for the merchant's stock.")]
//     [SerializeField] private RectTransform purchaseSlotsContainer;
//     [SerializeField] private GameObject shopSlotPrefab;
//     [Tooltip("Selection icon moved between grid slots during Browsing. Hidden during the confirm step.")]
//     [SerializeField] private RectTransform gridSelectionIcon;

//     [Header("Exit Slot")]
//     [Tooltip("The free-standing exit icon's RectTransform, positioned just right of the Purchase column.")]
//     [SerializeField] private RectTransform exitSlot;
//     [SerializeField] private Image exitIconImage;

//     [Header("Bottom Bubble")]
//     [SerializeField] private TextMeshProUGUI bottomText;
//     [SerializeField] private GameObject confirmOptionsRoot;
//     [SerializeField] private TextMeshProUGUI yesText;
//     [SerializeField] private TextMeshProUGUI noText;
//     [Tooltip("Selection icon for the Yes/No confirm step - separate from gridSelectionIcon since it lives in a different part of the layout.")]
//     [SerializeField] private RectTransform confirmSelectionIcon;

//     [Header("Opacity")]
//     [Range(0f, 1f)]
//     [SerializeField] private float dimmedAlpha = 0.5f;

//     private readonly List<ShopSlotUI> sellSlotUIs = new List<ShopSlotUI>();
//     private readonly List<ShopSlotUI> purchaseSlotUIs = new List<ShopSlotUI>();

//     private int lastColumn = -1;
//     private int lastIndex = -1;

//     private bool isSubscribed;
//     private Coroutine fadeCoroutine;

//     private void Start()
//     {
//         BuildSlots(sellSlotsContainer, sellSlotUIs);
//         BuildSlots(purchaseSlotsContainer, purchaseSlotUIs);

//         SetPanelVisible(false, instant: true);
//         if (confirmOptionsRoot != null) confirmOptionsRoot.SetActive(false);
//         if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(false);
//         if (confirmSelectionIcon != null) confirmSelectionIcon.gameObject.SetActive(false);

//         TrySubscribe();
//     }

//     private void BuildSlots(RectTransform container, List<ShopSlotUI> list)
//     {
//         if (container == null || shopSlotPrefab == null) return;

//         for (int i = 0; i < 8; i++)
//         {
//             GameObject instance = Instantiate(shopSlotPrefab, container);
//             ShopSlotUI slot = instance.GetComponent<ShopSlotUI>();

//             if (slot == null)
//             {
//                 Debug.LogError("ShopUI: shopSlotPrefab is missing a ShopSlotUI component.");
//                 continue;
//             }

//             list.Add(slot);
//         }
//     }

//     private void OnEnable() => TrySubscribe();

//     private void OnDisable()
//     {
//         if (!isSubscribed) return;

//         if (ShopManager.Instance != null)
//         {
//             ShopManager.Instance.OnShopOpened -= HandleShopOpened;
//             ShopManager.Instance.OnShopClosed -= HandleShopClosed;
//             ShopManager.Instance.OnSlotsChanged -= HandleSlotsChanged;
//             ShopManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
//             ShopManager.Instance.OnBottomTextChanged -= HandleBottomTextChanged;
//             ShopManager.Instance.OnConfirmOptionsShown -= HandleConfirmOptionsShown;
//             ShopManager.Instance.OnConfirmSelectionChanged -= HandleConfirmSelectionChanged;
//             ShopManager.Instance.OnBlurAmountChanged -= HandleBlurAmountChanged;
//         }
//         isSubscribed = false;
//     }

//     private void TrySubscribe()
//     {
//         if (isSubscribed) return;

//         if (ShopManager.Instance == null)
//         {
//             Debug.LogWarning("ShopUI: ShopManager.Instance is null. Make sure ShopManager exists in the scene.");
//             return;
//         }

//         ShopManager.Instance.OnShopOpened += HandleShopOpened;
//         ShopManager.Instance.OnShopClosed += HandleShopClosed;
//         ShopManager.Instance.OnSlotsChanged += HandleSlotsChanged;
//         ShopManager.Instance.OnSelectionChanged += HandleSelectionChanged;
//         ShopManager.Instance.OnBottomTextChanged += HandleBottomTextChanged;
//         ShopManager.Instance.OnConfirmOptionsShown += HandleConfirmOptionsShown;
//         ShopManager.Instance.OnConfirmSelectionChanged += HandleConfirmSelectionChanged;
//         ShopManager.Instance.OnBlurAmountChanged += HandleBlurAmountChanged;
//         isSubscribed = true;
//     }

//     // ---------- Open / close ----------

//     private void HandleShopOpened()
//     {
//         if (screenBlur != null) screenBlur.Capture();
//         SetPanelVisible(true);
//     }

//     private void HandleShopClosed()
//     {
//         SetPanelVisible(false);
//         if (screenBlur != null) screenBlur.Clear();

//         if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(false);
//         if (confirmOptionsRoot != null) confirmOptionsRoot.SetActive(false);
//         if (confirmSelectionIcon != null) confirmSelectionIcon.gameObject.SetActive(false);

//         lastColumn = -1;
//         lastIndex = -1;
//     }

//     private void HandleBlurAmountChanged(float amount)
//     {
//         if (screenBlur != null) screenBlur.SetBlurAmount(amount);
//     }

//     private void SetPanelVisible(bool visible, bool instant = false)
//     {
//         if (panelCanvasGroup == null) return;
//         if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

//         if (instant)
//         {
//             panelCanvasGroup.alpha = visible ? 1f : 0f;
//             panelCanvasGroup.interactable = visible;
//             panelCanvasGroup.blocksRaycasts = visible;
//             return;
//         }

//         fadeCoroutine = StartCoroutine(FadePanel(visible));
//     }

//     private IEnumerator FadePanel(bool visible)
//     {
//         float start = panelCanvasGroup.alpha;
//         float target = visible ? 1f : 0f;
//         float elapsed = 0f;

//         while (elapsed < fadeDuration)
//         {
//             elapsed += Time.deltaTime;
//             panelCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
//             yield return null;
//         }

//         panelCanvasGroup.alpha = target;
//         panelCanvasGroup.interactable = visible;
//         panelCanvasGroup.blocksRaycasts = visible;
//     }

//     // ---------- Grid contents / selection ----------

//     private void HandleSlotsChanged(ItemData[] sell, ItemData[] purchase)
//     {
//         for (int i = 0; i < sellSlotUIs.Count; i++)
//         {
//             sellSlotUIs[i].SetItem(i < sell.Length ? sell[i] : null, showSellPrice: true);
//         }
//         for (int i = 0; i < purchaseSlotUIs.Count; i++)
//         {
//             purchaseSlotUIs[i].SetItem(i < purchase.Length ? purchase[i] : null, showSellPrice: false);
//         }

//         RefreshAllOpacity();
//     }

//     private void HandleSelectionChanged(int column, int index)
//     {
//         lastColumn = column;
//         lastIndex = index;

//         if (column < 0)
//         {
//             if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(false);
//             RefreshAllOpacity();
//             return;
//         }

//         if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(true);

//         RectTransform targetSlot = null;
//         if (column == 0 && index < sellSlotUIs.Count) targetSlot = sellSlotUIs[index].RectTransform;
//         else if (column == 1 && index < purchaseSlotUIs.Count) targetSlot = purchaseSlotUIs[index].RectTransform;
//         else if (column == 2) targetSlot = exitSlot;

//         if (targetSlot != null && gridSelectionIcon != null)
//         {
//             Canvas.ForceUpdateCanvases();
//             gridSelectionIcon.position = targetSlot.position;
//         }

//         RefreshAllOpacity();
//     }

//     private void RefreshAllOpacity()
//     {
//         bool hidden = lastColumn < 0; // true during the confirm step

//         for (int i = 0; i < sellSlotUIs.Count; i++)
//         {
//             bool active = !hidden && lastColumn == 0 && lastIndex == i;
//             sellSlotUIs[i].SetOpacity(active ? 1f : dimmedAlpha);
//         }

//         for (int i = 0; i < purchaseSlotUIs.Count; i++)
//         {
//             bool active = !hidden && lastColumn == 1 && lastIndex == i;
//             purchaseSlotUIs[i].SetOpacity(active ? 1f : dimmedAlpha);
//         }

//         if (exitIconImage != null)
//         {
//             bool active = !hidden && lastColumn == 2;
//             Color c = exitIconImage.color;
//             c.a = active ? 1f : dimmedAlpha;
//             exitIconImage.color = c;
//         }
//     }

//     // ---------- Bottom bubble ----------

//     private void HandleBottomTextChanged(string text)
//     {
//         if (bottomText != null) bottomText.text = text;
//     }

//     private void HandleConfirmOptionsShown(bool show)
//     {
//         if (confirmOptionsRoot != null) confirmOptionsRoot.SetActive(show);
//         if (!show && confirmSelectionIcon != null) confirmSelectionIcon.gameObject.SetActive(false);
//     }

//     private void HandleConfirmSelectionChanged(int index)
//     {
//         if (yesText != null)
//         {
//             Color c = yesText.color;
//             c.a = index == 0 ? 1f : dimmedAlpha;
//             yesText.color = c;
//         }
//         if (noText != null)
//         {
//             Color c = noText.color;
//             c.a = index == 1 ? 1f : dimmedAlpha;
//             noText.color = c;
//         }

//         if (confirmSelectionIcon != null)
//         {
//             confirmSelectionIcon.gameObject.SetActive(true);
//             RectTransform target = index == 0 ? (yesText != null ? yesText.rectTransform : null)
//                                                 : (noText != null ? noText.rectTransform : null);
//             if (target != null)
//             {
//                 Canvas.ForceUpdateCanvases();
//                 confirmSelectionIcon.position = target.position;
//             }
//         }
//     }
// }


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Renders the shop UI by subscribing to ShopManager's events - purely a view layer, same
/// split as every other Manager/UI pair in this project. Builds the 8 fixed slots per column
/// once (never rebuilt, since the shop grid size never changes), moves a single selection icon
/// between grid slots, and toggles a separate confirm-step Yes/No cursor for the bottom bubble.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Post-Processing Blur")]
    [Tooltip("The Post-process Volume (Built-in RP) or Volume (URP) component to enable/disable " +
             "while the shop is open. Typed as the generic Behaviour so this works with either " +
             "pipeline's component without a pipeline-specific dependency.")]
    [SerializeField] private Behaviour postProcessVolume;

    [Header("Panel")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Sell / Purchase Columns")]
    [Tooltip("Container with a Grid Layout Group (2 columns x 4 rows) for the player's sellable items.")]
    [SerializeField] private RectTransform sellSlotsContainer;
    [Tooltip("Container with a Grid Layout Group (2 columns x 4 rows) for the merchant's stock.")]
    [SerializeField] private RectTransform purchaseSlotsContainer;
    [SerializeField] private GameObject shopSlotPrefab;
    [Tooltip("Selection icon moved between grid slots during Browsing. Hidden during the confirm step.")]
    [SerializeField] private RectTransform gridSelectionIcon;

    [Header("Exit Slot")]
    [Tooltip("The free-standing exit icon's RectTransform, positioned just right of the Purchase column.")]
    [SerializeField] private RectTransform exitSlot;
    [SerializeField] private Image exitIconImage;

    [Header("Bottom Bubble")]
    [SerializeField] private TextMeshProUGUI bottomText;
    [SerializeField] private GameObject confirmOptionsRoot;
    [SerializeField] private TextMeshProUGUI yesText;
    [SerializeField] private TextMeshProUGUI noText;
    [Tooltip("Selection icon for the Yes/No confirm step - separate from gridSelectionIcon since it lives in a different part of the layout.")]
    [SerializeField] private RectTransform confirmSelectionIcon;

    [Header("Opacity")]
    [Range(0f, 1f)]
    [SerializeField] private float dimmedAlpha = 0.5f;

    private readonly List<ShopSlotUI> sellSlotUIs = new List<ShopSlotUI>();
    private readonly List<ShopSlotUI> purchaseSlotUIs = new List<ShopSlotUI>();

    private int lastColumn = -1;
    private int lastIndex = -1;

    private bool isSubscribed;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        BuildSlots(sellSlotsContainer, sellSlotUIs);
        BuildSlots(purchaseSlotsContainer, purchaseSlotUIs);

        SetPanelVisible(false, instant: true);
        if (confirmOptionsRoot != null) confirmOptionsRoot.SetActive(false);
        if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(false);
        if (confirmSelectionIcon != null) confirmSelectionIcon.gameObject.SetActive(false);

        TrySubscribe();
    }

    private void BuildSlots(RectTransform container, List<ShopSlotUI> list)
    {
        if (container == null || shopSlotPrefab == null) return;

        for (int i = 0; i < 8; i++)
        {
            GameObject instance = Instantiate(shopSlotPrefab, container);
            ShopSlotUI slot = instance.GetComponent<ShopSlotUI>();

            if (slot == null)
            {
                Debug.LogError("ShopUI: shopSlotPrefab is missing a ShopSlotUI component.");
                continue;
            }

            list.Add(slot);
        }
    }

    private void OnEnable() => TrySubscribe();

    private void OnDisable()
    {
        if (!isSubscribed) return;

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopOpened -= HandleShopOpened;
            ShopManager.Instance.OnShopClosed -= HandleShopClosed;
            ShopManager.Instance.OnSlotsChanged -= HandleSlotsChanged;
            ShopManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
            ShopManager.Instance.OnBottomTextChanged -= HandleBottomTextChanged;
            ShopManager.Instance.OnConfirmOptionsShown -= HandleConfirmOptionsShown;
            ShopManager.Instance.OnConfirmSelectionChanged -= HandleConfirmSelectionChanged;
        }
        isSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (ShopManager.Instance == null)
        {
            Debug.LogWarning("ShopUI: ShopManager.Instance is null. Make sure ShopManager exists in the scene.");
            return;
        }

        ShopManager.Instance.OnShopOpened += HandleShopOpened;
        ShopManager.Instance.OnShopClosed += HandleShopClosed;
        ShopManager.Instance.OnSlotsChanged += HandleSlotsChanged;
        ShopManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        ShopManager.Instance.OnBottomTextChanged += HandleBottomTextChanged;
        ShopManager.Instance.OnConfirmOptionsShown += HandleConfirmOptionsShown;
        ShopManager.Instance.OnConfirmSelectionChanged += HandleConfirmSelectionChanged;
        isSubscribed = true;
    }

    // ---------- Open / close ----------

    private void HandleShopOpened()
    {
        if (postProcessVolume != null) postProcessVolume.enabled = true;
        SetPanelVisible(true);
    }

    private void HandleShopClosed()
    {
        SetPanelVisible(false);
        if (postProcessVolume != null) postProcessVolume.enabled = false;

        if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(false);
        if (confirmOptionsRoot != null) confirmOptionsRoot.SetActive(false);
        if (confirmSelectionIcon != null) confirmSelectionIcon.gameObject.SetActive(false);

        lastColumn = -1;
        lastIndex = -1;
    }

    private void SetPanelVisible(bool visible, bool instant = false)
    {
        if (panelCanvasGroup == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (instant)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panelCanvasGroup.interactable = visible;
            panelCanvasGroup.blocksRaycasts = visible;
            return;
        }

        fadeCoroutine = StartCoroutine(FadePanel(visible));
    }

    private IEnumerator FadePanel(bool visible)
    {
        float start = panelCanvasGroup.alpha;
        float target = visible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = target;
        panelCanvasGroup.interactable = visible;
        panelCanvasGroup.blocksRaycasts = visible;
    }

    // ---------- Grid contents / selection ----------

    private void HandleSlotsChanged(ItemData[] sell, ItemData[] purchase)
    {
        for (int i = 0; i < sellSlotUIs.Count; i++)
        {
            sellSlotUIs[i].SetItem(i < sell.Length ? sell[i] : null, showSellPrice: true);
        }
        for (int i = 0; i < purchaseSlotUIs.Count; i++)
        {
            purchaseSlotUIs[i].SetItem(i < purchase.Length ? purchase[i] : null, showSellPrice: false);
        }

        RefreshAllOpacity();
    }

    private void HandleSelectionChanged(int column, int index)
    {
        lastColumn = column;
        lastIndex = index;

        if (column < 0)
        {
            if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(false);
            RefreshAllOpacity();
            return;
        }

        if (gridSelectionIcon != null) gridSelectionIcon.gameObject.SetActive(true);

        RectTransform targetSlot = null;
        if (column == 0 && index < sellSlotUIs.Count) targetSlot = sellSlotUIs[index].RectTransform;
        else if (column == 1 && index < purchaseSlotUIs.Count) targetSlot = purchaseSlotUIs[index].RectTransform;
        else if (column == 2) targetSlot = exitSlot;

        if (targetSlot != null && gridSelectionIcon != null)
        {
            Canvas.ForceUpdateCanvases();
            gridSelectionIcon.position = targetSlot.position;
        }

        RefreshAllOpacity();
    }

    private void RefreshAllOpacity()
    {
        bool hidden = lastColumn < 0; // true during the confirm step

        for (int i = 0; i < sellSlotUIs.Count; i++)
        {
            bool active = !hidden && lastColumn == 0 && lastIndex == i;
            sellSlotUIs[i].SetOpacity(active ? 1f : dimmedAlpha);
        }

        for (int i = 0; i < purchaseSlotUIs.Count; i++)
        {
            bool active = !hidden && lastColumn == 1 && lastIndex == i;
            purchaseSlotUIs[i].SetOpacity(active ? 1f : dimmedAlpha);
        }

        if (exitIconImage != null)
        {
            bool active = !hidden && lastColumn == 2;
            Color c = exitIconImage.color;
            c.a = active ? 1f : dimmedAlpha;
            exitIconImage.color = c;
        }
    }

    // ---------- Bottom bubble ----------

    private void HandleBottomTextChanged(string text)
    {
        if (bottomText != null) bottomText.text = text;
    }

    private void HandleConfirmOptionsShown(bool show)
    {
        if (confirmOptionsRoot != null) confirmOptionsRoot.SetActive(show);
        if (!show && confirmSelectionIcon != null) confirmSelectionIcon.gameObject.SetActive(false);
    }

    private void HandleConfirmSelectionChanged(int index)
    {
        if (yesText != null)
        {
            Color c = yesText.color;
            c.a = index == 0 ? 1f : dimmedAlpha;
            yesText.color = c;
        }
        if (noText != null)
        {
            Color c = noText.color;
            c.a = index == 1 ? 1f : dimmedAlpha;
            noText.color = c;
        }

        if (confirmSelectionIcon != null)
        {
            confirmSelectionIcon.gameObject.SetActive(true);
            RectTransform target = index == 0 ? (yesText != null ? yesText.rectTransform : null)
                                                : (noText != null ? noText.rectTransform : null);
            if (target != null)
            {
                Canvas.ForceUpdateCanvases();
                confirmSelectionIcon.position = target.position;
            }
        }
    }
}