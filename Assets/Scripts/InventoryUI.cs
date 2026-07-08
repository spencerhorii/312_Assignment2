// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;

// /// <summary>
// /// Controls the inventory popup: TAB toggle with grow/shrink animation, left/right arrow-key
// /// navigation with a red selection cursor, and the automatic "pickup preview" popup that
// /// appears briefly when a new item is collected.
// ///
// /// State machine: Closed -> Opening -> Open -> Closing -> Closed. The Opening/Closing states
// /// exist specifically so the player can't re-trigger the toggle mid-animation.
// ///
// /// Display modes while Open:
// ///   Browse         - manual TAB-opened, shows the red selection cursor, arrow keys navigate,
// ///                    freezes player movement (CanMove = false) since arrow keys are shared
// ///                    with navigation and this is a deliberate "menu" state.
// ///   PickupPreview  - auto-opened on item pickup, no cursor, highlights the newest item,
// ///                    auto-closes after pickupPreviewDuration seconds. Does NOT freeze
// ///                    movement - it's a passive notification, not a menu.
// ///
// /// IMPORTANT: visibility is controlled via a CanvasGroup (alpha/interactable/blocksRaycasts)
// /// rather than SetActive() on panelRoot's GameObject. This is deliberate - if this script were
// /// attached to the same GameObject as panelRoot and we disabled that GameObject, the script's
// /// own Update() loop would stop running and it could never react to Tab again. Using a
// /// CanvasGroup means panelRoot's GameObject stays active at all times, regardless of where
// /// this script is attached in the hierarchy.
// /// </summary>
// public class InventoryUI : MonoBehaviour
// {
//     private enum PopupState { Closed, Opening, Open, Closing }
//     private enum DisplayMode { Browse, PickupPreview }

//     [Header("Panel / Animation")]
//     [Tooltip("Root RectTransform of the whole popup panel. This is what scales up/down on open/close. " +
//              "Its GameObject must remain active at all times - visibility is handled via panelCanvasGroup.")]
//     [SerializeField] private RectTransform panelRoot;
//     [Tooltip("CanvasGroup on panelRoot (or a parent of it), used to hide/show and enable/disable " +
//              "interaction without ever deactivating the GameObject. Auto-found on panelRoot if left empty.")]
//     [SerializeField] private CanvasGroup panelCanvasGroup;
//     [Tooltip("Duration in seconds of the open/close scale animation.")]
//     [SerializeField] private float openCloseDuration = 0.15f;
//     [Tooltip("Easing curve for the scale animation (0->1 over normalized time).")]
//     [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

//     [Header("Slots")]
//     [Tooltip("Container with a Horizontal Layout Group that holds the instantiated slot prefabs.")]
//     [SerializeField] private RectTransform slotsContainer;
//     [Tooltip("Prefab for a single inventory slot, must have an InventorySlotUI component.")]
//     [SerializeField] private GameObject slotPrefab;

//     [Header("Selection Cursor")]
//     [Tooltip("Red selection icon RectTransform, moved between slots during Browse mode.")]
//     [SerializeField] private RectTransform selectionIcon;

//     [Header("Item Name Label")]
//     [SerializeField] private TextMeshProUGUI itemNameText;

//     [Header("Pickup Preview")]
//     [Tooltip("How long the auto-opened pickup preview stays visible before closing itself.")]
//     [SerializeField] private float pickupPreviewDuration = 2.5f;

//     [Header("Opacity")]
//     [Tooltip("Opacity applied to icons that are not currently selected/highlighted.")]
//     [Range(0f, 1f)]
//     [SerializeField] private float dimmedAlpha = 0.5f;

//     private PopupState state = PopupState.Closed;
//     private DisplayMode mode = DisplayMode.Browse;

//     private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
//     private ItemData[] currentSlots;

//     private int selectedIndex;   // used in Browse mode
//     private int highlightIndex = -1; // used in PickupPreview mode

//     private Coroutine animCoroutine;
//     private Coroutine previewCoroutine;
//     private bool isSubscribed;

//     private void Start()
//     {
//         if (panelCanvasGroup == null && panelRoot != null)
//         {
//             panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
//         }

//         if (panelCanvasGroup == null)
//         {
//             Debug.LogError("InventoryUI: no CanvasGroup found on panelRoot. Add a CanvasGroup " +
//                             "component to the panel so it can be shown/hidden correctly.");
//         }

//         if (panelRoot != null)
//         {
//             panelRoot.localScale = Vector3.zero;
//         }

//         SetPanelVisible(false);

//         TrySubscribe();
//     }

//     private void OnEnable()
//     {
//         TrySubscribe();
//     }

//     private void OnDisable()
//     {
//         if (InventoryManager.Instance != null && isSubscribed)
//         {
//             InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
//             InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
//             isSubscribed = false;
//         }
//     }

//     private void TrySubscribe()
//     {
//         if (isSubscribed) return;

//         if (InventoryManager.Instance == null)
//         {
//             Debug.LogWarning("InventoryUI: InventoryManager.Instance is null. " +
//                               "Make sure InventoryManager exists in the scene.");
//             return;
//         }

//         InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
//         InventoryManager.Instance.OnItemAdded += HandleItemAdded;
//         isSubscribed = true;

//         // Catch up in case Initialize() already ran before we subscribed.
//         if (InventoryManager.Instance.IsInitialized)
//         {
//             HandleInventoryChanged(InventoryManager.Instance.GetSlots());
//         }
//     }

//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Tab))
//         {
//             HandleTabPressed();
//         }

//         if (state == PopupState.Open && mode == DisplayMode.Browse)
//         {
//             if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
//             else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
//         }
//     }

//     // ---------- Tab toggle / state machine ----------

//     private void HandleTabPressed()
//     {
//         switch (state)
//         {
//             case PopupState.Closed:
//                 OpenBrowseMode();
//                 break;

//             case PopupState.Open:
//                 if (mode == DisplayMode.PickupPreview)
//                 {
//                     // Player wants to actually browse - take over instead of closing.
//                     CancelPreviewTimer();
//                     EnterBrowseMode();
//                 }
//                 else
//                 {
//                     StartClosing();
//                 }
//                 break;

//             // Opening / Closing: ignore input, animation must finish first.
//         }
//     }

//     private void OpenBrowseMode()
//     {
//         mode = DisplayMode.Browse;
//         selectedIndex = 0;

//         if (selectionIcon != null) selectionIcon.gameObject.SetActive(true);

//         RefreshOpacity();
//         StartOpening();

//         // Browse mode is a deliberate menu state - freeze movement so A/D doesn't move the
//         // player while they're navigating the inventory.
//         if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = false;

//         Canvas.ForceUpdateCanvases();
//         UpdateSelectionIconPosition();
//         UpdateItemNameLabel();
//     }

//     private void EnterBrowseMode()
//     {
//         mode = DisplayMode.Browse;
//         selectedIndex = highlightIndex >= 0 ? highlightIndex : 0;

//         if (selectionIcon != null) selectionIcon.gameObject.SetActive(true);

//         RefreshOpacity();

//         if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = false;

//         Canvas.ForceUpdateCanvases();
//         UpdateSelectionIconPosition();
//         UpdateItemNameLabel();
//     }

//     private void StartOpening()
//     {
//         if (animCoroutine != null) StopCoroutine(animCoroutine);

//         state = PopupState.Opening;
//         SetPanelVisible(true);

//         animCoroutine = StartCoroutine(AnimateScale(Vector3.one, PopupState.Open));
//     }

//     private void StartClosing()
//     {
//         if (animCoroutine != null) StopCoroutine(animCoroutine);

//         state = PopupState.Closing;
//         animCoroutine = StartCoroutine(AnimateScale(Vector3.zero, PopupState.Closed));
//     }

//     private IEnumerator AnimateScale(Vector3 targetScale, PopupState endState)
//     {
//         if (panelRoot == null) yield break;

//         Vector3 startScale = panelRoot.localScale;
//         float t = 0f;

//         while (t < openCloseDuration)
//         {
//             t += Time.deltaTime;
//             float normalized = Mathf.Clamp01(t / openCloseDuration);
//             float curved = scaleCurve.Evaluate(normalized);
//             panelRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, curved);
//             yield return null;
//         }

//         panelRoot.localScale = targetScale;
//         state = endState;

//         if (endState == PopupState.Closed)
//         {
//             SetPanelVisible(false);

//             // Always safe to restore movement here: if it was never frozen (PickupPreview),
//             // this is a harmless no-op since CanMove is already true.
//             if (PlayerController2D.Instance != null) PlayerController2D.Instance.CanMove = true;
//         }
//     }

//     /// <summary>
//     /// Shows/hides the panel via CanvasGroup instead of SetActive(), so the GameObject
//     /// (and this script, if attached to it) never gets deactivated.
//     /// </summary>
//     private void SetPanelVisible(bool visible)
//     {
//         if (panelCanvasGroup == null) return;

//         panelCanvasGroup.alpha = visible ? 1f : 0f;
//         panelCanvasGroup.interactable = visible;
//         panelCanvasGroup.blocksRaycasts = visible;
//     }

//     // ---------- Browse navigation ----------

//     private void MoveSelection(int direction)
//     {
//         if (slotUIs.Count == 0) return;

//         selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, slotUIs.Count - 1);
//         RefreshOpacity();
//         UpdateSelectionIconPosition();
//         UpdateItemNameLabel();
//     }

//     private void UpdateSelectionIconPosition()
//     {
//         if (selectionIcon == null || slotUIs.Count == 0) return;
//         if (selectedIndex < 0 || selectedIndex >= slotUIs.Count) return;

//         selectionIcon.position = slotUIs[selectedIndex].RectTransform.position;
//     }

//     private void UpdateItemNameLabel()
//     {
//         if (itemNameText == null || currentSlots == null) return;

//         int index = mode == DisplayMode.Browse ? selectedIndex : highlightIndex;

//         if (index >= 0 && index < currentSlots.Length && currentSlots[index] != null)
//         {
//             itemNameText.text = currentSlots[index].itemName;
//         }
//         else
//         {
//             itemNameText.text = "";
//         }
//     }

//     private void RefreshOpacity()
//     {
//         int activeIndex = mode == DisplayMode.Browse ? selectedIndex : highlightIndex;

//         for (int i = 0; i < slotUIs.Count; i++)
//         {
//             slotUIs[i].SetOpacity(i == activeIndex ? 1f : dimmedAlpha);
//         }
//     }

//     // ---------- Inventory events ----------

//     private void HandleInventoryChanged(ItemData[] slots)
//     {
//         currentSlots = slots;
//         BuildSlotsIfNeeded(slots.Length);
//         RefreshSlotIcons(slots);
//         RefreshOpacity();
//     }

//     private void HandleItemAdded(ItemData item, int slotIndex)
//     {
//         highlightIndex = slotIndex;

//         if (itemNameText != null) itemNameText.text = item.itemName;

//         if (state == PopupState.Closed)
//         {
//             // Auto-open in pickup preview mode: no cursor, just the newest item highlighted.
//             // Deliberately does NOT freeze movement - this is a passive notification.
//             mode = DisplayMode.PickupPreview;
//             if (selectionIcon != null) selectionIcon.gameObject.SetActive(false);
//             RefreshOpacity();
//             StartOpening();

//             RestartPreviewTimer();
//         }
//         else if (state == PopupState.Open && mode == DisplayMode.PickupPreview)
//         {
//             // A preview is already showing (e.g. player grabbed a second item quickly).
//             // Refresh to highlight the new item and restart the timer so they get the
//             // full duration to see it, rather than it vanishing early.
//             RefreshOpacity();
//             RestartPreviewTimer();
//         }
//         else
//         {
//             // Player is currently browsing manually, or mid-animation - just refresh
//             // the slot contents in the background, don't interrupt what they're doing.
//             RefreshOpacity();
//         }
//     }

//     private void RestartPreviewTimer()
//     {
//         if (previewCoroutine != null) StopCoroutine(previewCoroutine);
//         previewCoroutine = StartCoroutine(AutoClosePreview());
//     }

//     private IEnumerator AutoClosePreview()
//     {
//         yield return new WaitForSeconds(pickupPreviewDuration);

//         if (state == PopupState.Open && mode == DisplayMode.PickupPreview)
//         {
//             StartClosing();
//         }
//     }

//     private void CancelPreviewTimer()
//     {
//         if (previewCoroutine != null)
//         {
//             StopCoroutine(previewCoroutine);
//             previewCoroutine = null;
//         }
//     }

//     // ---------- Slot building ----------

//     private void BuildSlotsIfNeeded(int count)
//     {
//         if (slotUIs.Count == count) return;

//         foreach (Transform child in slotsContainer)
//         {
//             Destroy(child.gameObject);
//         }
//         slotUIs.Clear();

//         for (int i = 0; i < count; i++)
//         {
//             GameObject instance = Instantiate(slotPrefab, slotsContainer);
//             InventorySlotUI slotUI = instance.GetComponent<InventorySlotUI>();

//             if (slotUI == null)
//             {
//                 Debug.LogError("InventoryUI: slotPrefab is missing an InventorySlotUI component.");
//                 continue;
//             }

//             slotUIs.Add(slotUI);
//         }

//         Canvas.ForceUpdateCanvases();
//     }

//     private void RefreshSlotIcons(ItemData[] slots)
//     {
//         for (int i = 0; i < slotUIs.Count && i < slots.Length; i++)
//         {
//             slotUIs[i].SetItem(slots[i]);
//         }
//     }
// }


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controls the inventory popup: TAB toggle with grow/shrink animation, left/right arrow-key
/// navigation with a red selection cursor, and the automatic "pickup preview" popup that
/// appears briefly when a new item is collected.
///
/// State machine: Closed -> Opening -> Open -> Closing -> Closed. The Opening/Closing states
/// exist specifically so the player can't re-trigger the toggle mid-animation.
///
/// Display modes while Open:
///   Browse         - manual TAB-opened, shows the red selection cursor, arrow keys navigate.
///   PickupPreview  - auto-opened on item pickup, no cursor, highlights the newest item,
///                    auto-closes after pickupPreviewDuration seconds.
///
/// Player movement is never frozen by this UI - A/D and arrow keys are fully separated
/// (movement vs. UI navigation), so there's no input conflict to guard against.
///
/// Visibility is controlled via a CanvasGroup (alpha/interactable/blocksRaycasts) rather than
/// SetActive() on panelRoot's GameObject, so panelRoot's GameObject - and this script, if
/// attached to it - never gets deactivated and Update() keeps running at all times.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    private enum PopupState { Closed, Opening, Open, Closing }
    private enum DisplayMode { Browse, PickupPreview }

    [Header("Panel / Animation")]
    [Tooltip("Root RectTransform of the whole popup panel. This is what scales up/down on open/close. " +
             "Its GameObject must remain active at all times - visibility is handled via panelCanvasGroup.")]
    [SerializeField] private RectTransform panelRoot;
    [Tooltip("CanvasGroup on panelRoot (or a parent of it), used to hide/show and enable/disable " +
             "interaction without ever deactivating the GameObject. Auto-found on panelRoot if left empty.")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [Tooltip("Duration in seconds of the open/close scale animation.")]
    [SerializeField] private float openCloseDuration = 0.15f;
    [Tooltip("Easing curve for the scale animation (0->1 over normalized time).")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("The scale the popup animates to when fully open. Adjust this instead of editing " +
             "panelRoot's Scale directly in the Inspector - Start() resets panelRoot's scale to " +
             "zero at runtime, and the open animation always targets this value, so manual edits " +
             "to panelRoot's Transform Scale will be overwritten.")]
    [SerializeField] private Vector3 maxScale = Vector3.one;

    [Header("Slots")]
    [Tooltip("Container with a Horizontal Layout Group that holds the instantiated slot prefabs.")]
    [SerializeField] private RectTransform slotsContainer;
    [Tooltip("Prefab for a single inventory slot, must have an InventorySlotUI component.")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Selection Cursor")]
    [Tooltip("Red selection icon RectTransform, moved between slots during Browse mode.")]
    [SerializeField] private RectTransform selectionIcon;

    [Header("Item Name Label")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Header("Pickup Preview")]
    [Tooltip("How long the auto-opened pickup preview stays visible before closing itself.")]
    [SerializeField] private float pickupPreviewDuration = 2.5f;

    [Header("Opacity")]
    [Tooltip("Opacity applied to icons that are not currently selected/highlighted.")]
    [Range(0f, 1f)]
    [SerializeField] private float dimmedAlpha = 0.5f;

    private PopupState state = PopupState.Closed;
    private DisplayMode mode = DisplayMode.Browse;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private ItemData[] currentSlots;

    private int selectedIndex;   // used in Browse mode
    private int highlightIndex = -1; // used in PickupPreview mode

    private Coroutine animCoroutine;
    private Coroutine previewCoroutine;
    private bool isSubscribed;

    private void Start()
    {
        if (panelCanvasGroup == null && panelRoot != null)
        {
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        }

        if (panelCanvasGroup == null)
        {
            Debug.LogError("InventoryUI: no CanvasGroup found on panelRoot. Add a CanvasGroup " +
                            "component to the panel so it can be shown/hidden correctly.");
        }

        if (panelRoot != null)
        {
            panelRoot.localScale = Vector3.zero;
        }

        SetPanelVisible(false);

        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null && isSubscribed)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
            InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
            isSubscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryUI: InventoryManager.Instance is null. " +
                              "Make sure InventoryManager exists in the scene.");
            return;
        }

        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
        isSubscribed = true;

        // Catch up in case Initialize() already ran before we subscribed.
        if (InventoryManager.Instance.IsInitialized)
        {
            HandleInventoryChanged(InventoryManager.Instance.GetSlots());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleTabPressed();
        }

        if (state == PopupState.Open && mode == DisplayMode.Browse)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
        }
    }

    // ---------- Tab toggle / state machine ----------

    private void HandleTabPressed()
    {
        switch (state)
        {
            case PopupState.Closed:
                OpenBrowseMode();
                break;

            case PopupState.Open:
                if (mode == DisplayMode.PickupPreview)
                {
                    // Player wants to actually browse - take over instead of closing.
                    CancelPreviewTimer();
                    EnterBrowseMode();
                }
                else
                {
                    StartClosing();
                }
                break;

            // Opening / Closing: ignore input, animation must finish first.
        }
    }

    private void OpenBrowseMode()
    {
        mode = DisplayMode.Browse;
        selectedIndex = 0;

        if (selectionIcon != null) selectionIcon.gameObject.SetActive(true);

        RefreshOpacity();
        StartOpening();

        Canvas.ForceUpdateCanvases();
        UpdateSelectionIconPosition();
        UpdateItemNameLabel();
    }

    private void EnterBrowseMode()
    {
        mode = DisplayMode.Browse;
        selectedIndex = highlightIndex >= 0 ? highlightIndex : 0;

        if (selectionIcon != null) selectionIcon.gameObject.SetActive(true);

        RefreshOpacity();

        Canvas.ForceUpdateCanvases();
        UpdateSelectionIconPosition();
        UpdateItemNameLabel();
    }

    private void StartOpening()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);

        state = PopupState.Opening;
        SetPanelVisible(true);

        animCoroutine = StartCoroutine(AnimateScale(maxScale, PopupState.Open));
    }

    private void StartClosing()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);

        state = PopupState.Closing;
        animCoroutine = StartCoroutine(AnimateScale(Vector3.zero, PopupState.Closed));
    }

    private IEnumerator AnimateScale(Vector3 targetScale, PopupState endState)
    {
        if (panelRoot == null) yield break;

        Vector3 startScale = panelRoot.localScale;
        float t = 0f;

        while (t < openCloseDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / openCloseDuration);
            float curved = scaleCurve.Evaluate(normalized);
            panelRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, curved);
            yield return null;
        }

        panelRoot.localScale = targetScale;
        state = endState;

        if (endState == PopupState.Closed)
        {
            SetPanelVisible(false);
        }
    }

    /// <summary>
    /// Shows/hides the panel via CanvasGroup instead of SetActive(), so the GameObject
    /// (and this script, if attached to it) never gets deactivated.
    /// </summary>
    private void SetPanelVisible(bool visible)
    {
        if (panelCanvasGroup == null) return;

        panelCanvasGroup.alpha = visible ? 1f : 0f;
        panelCanvasGroup.interactable = visible;
        panelCanvasGroup.blocksRaycasts = visible;
    }

    // ---------- Browse navigation ----------

    private void MoveSelection(int direction)
    {
        if (slotUIs.Count == 0) return;

        selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, slotUIs.Count - 1);
        RefreshOpacity();
        UpdateSelectionIconPosition();
        UpdateItemNameLabel();
    }

    private void UpdateSelectionIconPosition()
    {
        if (selectionIcon == null || slotUIs.Count == 0) return;
        if (selectedIndex < 0 || selectedIndex >= slotUIs.Count) return;

        selectionIcon.position = slotUIs[selectedIndex].RectTransform.position;
    }

    private void UpdateItemNameLabel()
    {
        if (itemNameText == null || currentSlots == null) return;

        int index = mode == DisplayMode.Browse ? selectedIndex : highlightIndex;

        if (index >= 0 && index < currentSlots.Length && currentSlots[index] != null)
        {
            itemNameText.text = currentSlots[index].itemName;
        }
        else
        {
            itemNameText.text = "";
        }
    }

    private void RefreshOpacity()
    {
        int activeIndex = mode == DisplayMode.Browse ? selectedIndex : highlightIndex;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].SetOpacity(i == activeIndex ? 1f : dimmedAlpha);
        }
    }

    // ---------- Inventory events ----------

    private void HandleInventoryChanged(ItemData[] slots)
    {
        currentSlots = slots;
        BuildSlotsIfNeeded(slots.Length);
        RefreshSlotIcons(slots);
        RefreshOpacity();
    }

    private void HandleItemAdded(ItemData item, int slotIndex)
    {
        highlightIndex = slotIndex;

        if (itemNameText != null) itemNameText.text = item.itemName;

        if (state == PopupState.Closed)
        {
            // Auto-open in pickup preview mode: no cursor, just the newest item highlighted.
            mode = DisplayMode.PickupPreview;
            if (selectionIcon != null) selectionIcon.gameObject.SetActive(false);
            RefreshOpacity();
            StartOpening();

            RestartPreviewTimer();
        }
        else if (state == PopupState.Open && mode == DisplayMode.PickupPreview)
        {
            // A preview is already showing (e.g. player grabbed a second item quickly).
            // Refresh to highlight the new item and restart the timer so they get the
            // full duration to see it, rather than it vanishing early.
            RefreshOpacity();
            RestartPreviewTimer();
        }
        else
        {
            // Player is currently browsing manually, or mid-animation - just refresh
            // the slot contents in the background, don't interrupt what they're doing.
            RefreshOpacity();
        }
    }

    private void RestartPreviewTimer()
    {
        if (previewCoroutine != null) StopCoroutine(previewCoroutine);
        previewCoroutine = StartCoroutine(AutoClosePreview());
    }

    private IEnumerator AutoClosePreview()
    {
        yield return new WaitForSeconds(pickupPreviewDuration);

        if (state == PopupState.Open && mode == DisplayMode.PickupPreview)
        {
            StartClosing();
        }
    }

    private void CancelPreviewTimer()
    {
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }
    }

    // ---------- Slot building ----------

    private void BuildSlotsIfNeeded(int count)
    {
        if (slotUIs.Count == count) return;

        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = instance.GetComponent<InventorySlotUI>();

            if (slotUI == null)
            {
                Debug.LogError("InventoryUI: slotPrefab is missing an InventorySlotUI component.");
                continue;
            }

            slotUIs.Add(slotUI);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void RefreshSlotIcons(ItemData[] slots)
    {
        for (int i = 0; i < slotUIs.Count && i < slots.Length; i++)
        {
            slotUIs[i].SetItem(slots[i]);
        }
    }
}