// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;

// /// <summary>
// /// Controls the inventory popup: two pages (Items, QuestItems) with fade+slide transitions,
// /// pickup previews, and (new) removal handling - when a dialogue delivery action removes an
// /// item, this listens for InventoryManager/QuestInventoryManager's "removal requested" events,
// /// opens the appropriate page, plays a grow-then-collapse animation on that specific slot
// /// (InventorySlotUI.PlayRemovalAnimation), and only THEN calls back into the manager's
// /// ConfirmRemoval() to actually delete the data and reorder the remaining items.
// ///
// /// Selection icons and arrow indicators are only shown during manual Browse mode - never during
// /// pickup previews OR removal displays - both toggled together via SetIconsAndArrows().
// /// </summary>
// public class InventoryUI : MonoBehaviour
// {
//     private enum PopupState { Closed, Opening, Open, Closing }
//     private enum DisplayMode { Browse, ItemPreview, QuestPreview }
//     private enum InventoryPage { Items, QuestItems }

//     [Header("Panel / Animation")]
//     [SerializeField] private RectTransform panelRoot;
//     [SerializeField] private CanvasGroup panelCanvasGroup;
//     [SerializeField] private float openCloseDuration = 0.15f;
//     [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//     [SerializeField] private Vector3 maxScale = Vector3.one;

//     [Header("Items Page")]
//     [SerializeField] private RectTransform itemsPageRoot;
//     [SerializeField] private CanvasGroup itemsPageCanvasGroup;
//     [SerializeField] private RectTransform itemsSlotsContainer;
//     [SerializeField] private GameObject itemSlotPrefab;
//     [SerializeField] private RectTransform itemsSelectionIcon;
//     [SerializeField] private GameObject itemsArrowIndicator;
//     [SerializeField] private TextMeshProUGUI itemsNameText;

//     [Header("Quest Items Page")]
//     [SerializeField] private RectTransform questPageRoot;
//     [SerializeField] private CanvasGroup questPageCanvasGroup;
//     [SerializeField] private RectTransform questSlotsContainer;
//     [SerializeField] private GameObject questSlotPrefab;
//     [SerializeField] private RectTransform questSelectionIcon;
//     [SerializeField] private GameObject questArrowIndicator;
//     [SerializeField] private TextMeshProUGUI questNameText;

//     [Header("Page Transition Animation")]
//     [SerializeField] private float pageTransitionDuration = 0.2f;
//     [SerializeField] private float pageSlideDistance = 40f;
//     [SerializeField] private AnimationCurve pageTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

//     [Header("Pickup Preview")]
//     [SerializeField] private float itemPickupPreviewDuration = 2.5f;
//     [SerializeField] private float questPickupPreviewDuration = 2.5f;

//     [Header("Removal Animation")]
//     [Tooltip("How much larger (as a multiplier) the item briefly grows before collapsing to zero.")]
//     [SerializeField] private float removalGrowScale = 1.3f;
//     [SerializeField] private float removalAnimationDuration = 0.5f;
//     [SerializeField] private AnimationCurve removalAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//     [Tooltip("How long the popup stays open after a removal finishes collapsing, before auto-closing.")]
//     [SerializeField] private float removalCloseDelay = 0.4f;

//     [Header("Opacity")]
//     [Range(0f, 1f)]
//     [SerializeField] private float dimmedAlpha = 0.5f;

//     private PopupState state = PopupState.Closed;
//     private DisplayMode mode = DisplayMode.Browse;
//     private InventoryPage currentPage = InventoryPage.Items;
//     private bool isPageTransitioning;

//     private readonly List<InventorySlotUI> itemSlotUIs = new List<InventorySlotUI>();
//     private readonly List<InventorySlotUI> questSlotUIs = new List<InventorySlotUI>();
//     private ItemData[] currentItemSlots;
//     private List<QuestItemData> currentQuestItems;

//     private int itemSelectedIndex;
//     private int questSelectedIndex;
//     private int itemHighlightIndex = -1;
//     private int questHighlightIndex = -1;

//     private Vector2 itemsPageBasePos;
//     private Vector2 questPageBasePos;

//     private Coroutine animCoroutine;
//     private Coroutine pageTransitionCoroutine;
//     private Coroutine itemPreviewCoroutine;
//     private Coroutine questPreviewCoroutine;
//     private bool isSubscribed;

//     private void Start()
//     {
//         if (panelCanvasGroup == null && panelRoot != null) panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
//         if (panelCanvasGroup == null)
//         {
//             Debug.LogError("InventoryUI: no CanvasGroup found on panelRoot. Add a CanvasGroup component to the panel.");
//         }

//         if (panelRoot != null) panelRoot.localScale = Vector3.zero;

//         if (itemsPageRoot != null) itemsPageBasePos = itemsPageRoot.anchoredPosition;
//         if (questPageRoot != null) questPageBasePos = questPageRoot.anchoredPosition;

//         SetPanelVisible(false);
//         SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
//         SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
//         SetIconsAndArrows(itemsVisible: false, questVisible: false);

//         TrySubscribe();
//     }

//     private void OnEnable() => TrySubscribe();

//     private void OnDisable()
//     {
//         if (!isSubscribed) return;

//         if (InventoryManager.Instance != null)
//         {
//             InventoryManager.Instance.OnInventoryChanged -= HandleItemInventoryChanged;
//             InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
//             InventoryManager.Instance.OnItemRemovalRequested -= HandleItemRemovalRequested;
//         }
//         if (QuestInventoryManager.Instance != null)
//         {
//             QuestInventoryManager.Instance.OnQuestInventoryChanged -= HandleQuestInventoryChanged;
//             QuestInventoryManager.Instance.OnQuestItemAdded -= HandleQuestItemAdded;
//             QuestInventoryManager.Instance.OnQuestItemRemovalRequested -= HandleQuestItemRemovalRequested;
//         }
//         isSubscribed = false;
//     }

//     private void TrySubscribe()
//     {
//         if (isSubscribed) return;

//         if (InventoryManager.Instance == null || QuestInventoryManager.Instance == null)
//         {
//             Debug.LogWarning("InventoryUI: InventoryManager or QuestInventoryManager instance is null.");
//             return;
//         }

//         InventoryManager.Instance.OnInventoryChanged += HandleItemInventoryChanged;
//         InventoryManager.Instance.OnItemAdded += HandleItemAdded;
//         InventoryManager.Instance.OnItemRemovalRequested += HandleItemRemovalRequested;
//         QuestInventoryManager.Instance.OnQuestInventoryChanged += HandleQuestInventoryChanged;
//         QuestInventoryManager.Instance.OnQuestItemAdded += HandleQuestItemAdded;
//         QuestInventoryManager.Instance.OnQuestItemRemovalRequested += HandleQuestItemRemovalRequested;
//         isSubscribed = true;

//         if (InventoryManager.Instance.IsInitialized)
//         {
//             HandleItemInventoryChanged(InventoryManager.Instance.GetSlots());
//         }
//         HandleQuestInventoryChanged(new List<QuestItemData>(QuestInventoryManager.Instance.Items));
//     }

//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Tab))
//         {
//             HandleTabPressed();
//         }

//         if (state == PopupState.Open && mode == DisplayMode.Browse && !isPageTransitioning)
//         {
//             if (currentPage == InventoryPage.Items)
//             {
//                 if (Input.GetKeyDown(KeyCode.RightArrow)) MoveItemSelection(1);
//                 else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveItemSelection(-1);
//                 else if (Input.GetKeyDown(KeyCode.UpArrow)) BeginPageTransition(InventoryPage.QuestItems);
//             }
//             else
//             {
//                 if (Input.GetKeyDown(KeyCode.RightArrow)) MoveQuestSelection(1);
//                 else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveQuestSelection(-1);
//                 else if (Input.GetKeyDown(KeyCode.DownArrow)) BeginPageTransition(InventoryPage.Items);
//             }
//         }
//     }

//     // ---------- Selection icon + arrow indicator visibility ----------

//     private void SetIconsAndArrows(bool itemsVisible, bool questVisible)
//     {
//         if (itemsSelectionIcon != null) itemsSelectionIcon.gameObject.SetActive(itemsVisible);
//         if (itemsArrowIndicator != null) itemsArrowIndicator.gameObject.SetActive(itemsVisible);

//         if (questSelectionIcon != null) questSelectionIcon.gameObject.SetActive(questVisible);
//         if (questArrowIndicator != null) questArrowIndicator.gameObject.SetActive(questVisible);
//     }

//     // ---------- Tab toggle / panel state machine ----------

//     private void HandleTabPressed()
//     {
//         switch (state)
//         {
//             case PopupState.Closed:
//                 OpenBrowseMode();
//                 break;

//             case PopupState.Open:
//                 if (mode == DisplayMode.ItemPreview)
//                 {
//                     CancelItemPreviewTimer();
//                     EnterBrowseMode(InventoryPage.Items, itemHighlightIndex);
//                 }
//                 else if (mode == DisplayMode.QuestPreview)
//                 {
//                     CancelQuestPreviewTimer();
//                     EnterBrowseMode(InventoryPage.QuestItems, questHighlightIndex);
//                 }
//                 else
//                 {
//                     StartClosing();
//                 }
//                 break;
//         }
//     }

//     private void OpenBrowseMode()
//     {
//         mode = DisplayMode.Browse;
//         currentPage = InventoryPage.Items;
//         itemSelectedIndex = 0;

//         SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
//         SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
//         SetIconsAndArrows(itemsVisible: true, questVisible: false);

//         RefreshItemOpacity();
//         StartOpening();

//         Canvas.ForceUpdateCanvases();
//         UpdateItemSelectionIconPosition();
//         UpdateItemNameLabel();
//     }

//     private void EnterBrowseMode(InventoryPage page, int highlightIndex)
//     {
//         mode = DisplayMode.Browse;
//         currentPage = page;

//         if (page == InventoryPage.Items)
//         {
//             itemSelectedIndex = highlightIndex >= 0 ? highlightIndex : 0;
//             SetIconsAndArrows(itemsVisible: true, questVisible: false);
//             RefreshItemOpacity();
//             Canvas.ForceUpdateCanvases();
//             UpdateItemSelectionIconPosition();
//             UpdateItemNameLabel();
//         }
//         else
//         {
//             questSelectedIndex = highlightIndex >= 0 ? highlightIndex : 0;
//             SetIconsAndArrows(itemsVisible: false, questVisible: true);
//             RefreshQuestOpacity();
//             Canvas.ForceUpdateCanvases();
//             UpdateQuestSelectionIconPosition();
//             UpdateQuestNameLabel();
//         }
//     }

//     private void StartOpening()
//     {
//         if (animCoroutine != null) StopCoroutine(animCoroutine);

//         state = PopupState.Opening;
//         SetPanelVisible(true);

//         animCoroutine = StartCoroutine(AnimateScale(maxScale, PopupState.Open));
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
//         }
//     }

//     private void SetPanelVisible(bool visible)
//     {
//         if (panelCanvasGroup == null) return;

//         panelCanvasGroup.alpha = visible ? 1f : 0f;
//         panelCanvasGroup.interactable = visible;
//         panelCanvasGroup.blocksRaycasts = visible;
//     }

//     // ---------- Page transition (Up/Down between Items and QuestItems) ----------

//     private void BeginPageTransition(InventoryPage targetPage)
//     {
//         if (isPageTransitioning || targetPage == currentPage) return;

//         bool movingToQuest = targetPage == InventoryPage.QuestItems;

//         RectTransform outRoot = movingToQuest ? itemsPageRoot : questPageRoot;
//         CanvasGroup outGroup = movingToQuest ? itemsPageCanvasGroup : questPageCanvasGroup;
//         Vector2 outBase = movingToQuest ? itemsPageBasePos : questPageBasePos;

//         RectTransform inRoot = movingToQuest ? questPageRoot : itemsPageRoot;
//         CanvasGroup inGroup = movingToQuest ? questPageCanvasGroup : itemsPageCanvasGroup;
//         Vector2 inBase = movingToQuest ? questPageBasePos : itemsPageBasePos;

//         bool moveDown = movingToQuest;

//         SetIconsAndArrows(itemsVisible: false, questVisible: false);

//         if (pageTransitionCoroutine != null) StopCoroutine(pageTransitionCoroutine);
//         pageTransitionCoroutine = StartCoroutine(PageTransition(outRoot, outGroup, outBase, inRoot, inGroup, inBase, moveDown, targetPage));
//     }

//     private IEnumerator PageTransition(RectTransform outRoot, CanvasGroup outGroup, Vector2 outBase,
//                                         RectTransform inRoot, CanvasGroup inGroup, Vector2 inBase,
//                                         bool moveDown, InventoryPage targetPage)
//     {
//         isPageTransitioning = true;

//         Vector2 exitOffset = moveDown ? new Vector2(0f, -pageSlideDistance) : new Vector2(0f, pageSlideDistance);
//         Vector2 enterStartOffset = moveDown ? new Vector2(0f, pageSlideDistance) : new Vector2(0f, -pageSlideDistance);

//         Vector2 outStart = outRoot.anchoredPosition;
//         Vector2 outTarget = outBase + exitOffset;
//         Vector2 inStart = inBase + enterStartOffset;
//         Vector2 inTarget = inBase;

//         inRoot.anchoredPosition = inStart;
//         inGroup.alpha = 0f;
//         inGroup.interactable = false;
//         inGroup.blocksRaycasts = false;

//         float outStartAlpha = outGroup.alpha;
//         float elapsed = 0f;

//         while (elapsed < pageTransitionDuration)
//         {
//             elapsed += Time.deltaTime;
//             float n = Mathf.Clamp01(elapsed / pageTransitionDuration);
//             float curved = pageTransitionCurve.Evaluate(n);

//             outRoot.anchoredPosition = Vector2.LerpUnclamped(outStart, outTarget, curved);
//             outGroup.alpha = Mathf.LerpUnclamped(outStartAlpha, 0f, curved);

//             inRoot.anchoredPosition = Vector2.LerpUnclamped(inStart, inTarget, curved);
//             inGroup.alpha = Mathf.LerpUnclamped(0f, 1f, curved);

//             yield return null;
//         }

//         outRoot.anchoredPosition = outBase;
//         outGroup.alpha = 0f;
//         outGroup.interactable = false;
//         outGroup.blocksRaycasts = false;

//         inRoot.anchoredPosition = inTarget;
//         inGroup.alpha = 1f;
//         inGroup.interactable = true;
//         inGroup.blocksRaycasts = true;

//         currentPage = targetPage;
//         isPageTransitioning = false;

//         if (targetPage == InventoryPage.QuestItems)
//         {
//             questSelectedIndex = 0;
//             SetIconsAndArrows(itemsVisible: false, questVisible: true);
//             RefreshQuestOpacity();
//             Canvas.ForceUpdateCanvases();
//             UpdateQuestSelectionIconPosition();
//             UpdateQuestNameLabel();
//         }
//         else
//         {
//             SetIconsAndArrows(itemsVisible: true, questVisible: false);
//             RefreshItemOpacity();
//             Canvas.ForceUpdateCanvases();
//             UpdateItemSelectionIconPosition();
//             UpdateItemNameLabel();
//         }
//     }

//     private void SetPageInstant(RectTransform root, CanvasGroup group, Vector2 basePos, bool visible)
//     {
//         if (root == null || group == null) return;

//         root.anchoredPosition = basePos;
//         group.alpha = visible ? 1f : 0f;
//         group.interactable = visible;
//         group.blocksRaycasts = visible;
//     }

//     // ---------- Items page navigation ----------

//     private void MoveItemSelection(int direction)
//     {
//         if (itemSlotUIs.Count == 0) return;

//         itemSelectedIndex = Mathf.Clamp(itemSelectedIndex + direction, 0, itemSlotUIs.Count - 1);
//         RefreshItemOpacity();
//         UpdateItemSelectionIconPosition();
//         UpdateItemNameLabel();
//     }

//     private void UpdateItemSelectionIconPosition()
//     {
//         if (itemsSelectionIcon == null || itemSlotUIs.Count == 0) return;
//         if (itemSelectedIndex < 0 || itemSelectedIndex >= itemSlotUIs.Count) return;

//         itemsSelectionIcon.position = itemSlotUIs[itemSelectedIndex].RectTransform.position;
//     }

//     private void UpdateItemNameLabel()
//     {
//         if (itemsNameText == null || currentItemSlots == null) return;

//         int index = mode == DisplayMode.ItemPreview ? itemHighlightIndex : itemSelectedIndex;

//         if (index >= 0 && index < currentItemSlots.Length && currentItemSlots[index] != null)
//         {
//             itemsNameText.text = currentItemSlots[index].itemName;
//         }
//         else
//         {
//             itemsNameText.text = "";
//         }
//     }

//     private void RefreshItemOpacity()
//     {
//         int activeIndex = mode == DisplayMode.ItemPreview ? itemHighlightIndex : itemSelectedIndex;

//         for (int i = 0; i < itemSlotUIs.Count; i++)
//         {
//             itemSlotUIs[i].SetOpacity(i == activeIndex ? 1f : dimmedAlpha);
//         }
//     }

//     // ---------- Quest page navigation ----------

//     private void MoveQuestSelection(int direction)
//     {
//         if (questSlotUIs.Count == 0) return;

//         questSelectedIndex = Mathf.Clamp(questSelectedIndex + direction, 0, questSlotUIs.Count - 1);
//         RefreshQuestOpacity();
//         UpdateQuestSelectionIconPosition();
//         UpdateQuestNameLabel();
//     }

//     private void UpdateQuestSelectionIconPosition()
//     {
//         if (questSelectionIcon == null || questSlotUIs.Count == 0) return;
//         if (questSelectedIndex < 0 || questSelectedIndex >= questSlotUIs.Count) return;

//         questSelectionIcon.position = questSlotUIs[questSelectedIndex].RectTransform.position;
//     }

//     private void UpdateQuestNameLabel()
//     {
//         if (questNameText == null || currentQuestItems == null) return;

//         int index = mode == DisplayMode.QuestPreview ? questHighlightIndex : questSelectedIndex;

//         if (index >= 0 && index < currentQuestItems.Count && currentQuestItems[index] != null)
//         {
//             questNameText.text = currentQuestItems[index].itemName;
//         }
//         else
//         {
//             questNameText.text = "";
//         }
//     }

//     private void RefreshQuestOpacity()
//     {
//         int activeIndex = mode == DisplayMode.QuestPreview ? questHighlightIndex : questSelectedIndex;

//         for (int i = 0; i < questSlotUIs.Count; i++)
//         {
//             questSlotUIs[i].SetOpacity(i == activeIndex ? 1f : dimmedAlpha);
//         }
//     }

//     // ---------- Items inventory events ----------

//     private void HandleItemInventoryChanged(ItemData[] slots)
//     {
//         currentItemSlots = slots;
//         BuildSlotsIfNeeded(itemsSlotsContainer, itemSlotUIs, slots.Length, itemSlotPrefab);
//         for (int i = 0; i < itemSlotUIs.Count && i < slots.Length; i++)
//         {
//             itemSlotUIs[i].SetItem(slots[i]);
//         }
//         RefreshItemOpacity();
//     }

//     private void HandleItemAdded(ItemData item, int slotIndex)
//     {
//         itemHighlightIndex = slotIndex;
//         if (itemsNameText != null) itemsNameText.text = item.itemName;

//         if (state == PopupState.Closed)
//         {
//             mode = DisplayMode.ItemPreview;
//             currentPage = InventoryPage.Items;
//             SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
//             SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
//             SetIconsAndArrows(itemsVisible: false, questVisible: false);

//             RefreshItemOpacity();
//             StartOpening();
//             RestartItemPreviewTimer();
//         }
//         else if (state == PopupState.Open && mode == DisplayMode.ItemPreview)
//         {
//             RefreshItemOpacity();
//             RestartItemPreviewTimer();
//         }
//         else
//         {
//             RefreshItemOpacity();
//         }
//     }

//     private void RestartItemPreviewTimer()
//     {
//         if (itemPreviewCoroutine != null) StopCoroutine(itemPreviewCoroutine);
//         itemPreviewCoroutine = StartCoroutine(AutoCloseItemPreview());
//     }

//     private IEnumerator AutoCloseItemPreview()
//     {
//         yield return new WaitForSeconds(itemPickupPreviewDuration);

//         if (state == PopupState.Open && mode == DisplayMode.ItemPreview)
//         {
//             StartClosing();
//         }
//     }

//     private void CancelItemPreviewTimer()
//     {
//         if (itemPreviewCoroutine != null)
//         {
//             StopCoroutine(itemPreviewCoroutine);
//             itemPreviewCoroutine = null;
//         }
//     }

//     // ---------- Items removal ----------

//     private void HandleItemRemovalRequested(ItemData item, int index)
//     {
//         itemHighlightIndex = index;
//         mode = DisplayMode.ItemPreview; // reuses the same "highlighted, no cursor" display rules as a pickup preview

//         if (state == PopupState.Closed)
//         {
//             currentPage = InventoryPage.Items;
//             SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
//             SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
//             SetIconsAndArrows(itemsVisible: false, questVisible: false);
//             if (itemsNameText != null) itemsNameText.text = item.itemName;

//             RefreshItemOpacity();
//             StartOpening();
//         }
//         else
//         {
//             if (itemsNameText != null) itemsNameText.text = item.itemName;
//             RefreshItemOpacity();
//         }

//         CancelItemPreviewTimer(); // a removal in progress should not be interrupted by a preview auto-close

//         if (index >= 0 && index < itemSlotUIs.Count)
//         {
//             itemSlotUIs[index].PlayRemovalAnimation(removalGrowScale, removalAnimationDuration, removalAnimationCurve, () =>
//             {
//                 if (InventoryManager.Instance != null) InventoryManager.Instance.ConfirmRemoval(index);
//                 StartCoroutine(CloseAfterRemoval());
//             });
//         }
//     }

//     private IEnumerator CloseAfterRemoval()
//     {
//         yield return new WaitForSeconds(removalCloseDelay);

//         if (state == PopupState.Open && mode == DisplayMode.ItemPreview)
//         {
//             StartClosing();
//         }
//     }

//     // ---------- Quest inventory events ----------

//     private void HandleQuestInventoryChanged(List<QuestItemData> items)
//     {
//         currentQuestItems = items;
//         int slotCount = Mathf.Max(1, items.Count);

//         BuildSlotsIfNeeded(questSlotsContainer, questSlotUIs, slotCount, questSlotPrefab);

//         for (int i = 0; i < questSlotUIs.Count; i++)
//         {
//             if (i < items.Count) questSlotUIs[i].SetItem(items[i]);
//             else questSlotUIs[i].Clear();
//         }

//         RefreshQuestOpacity();
//     }

//     private void HandleQuestItemAdded(QuestItemData item, int index)
//     {
//         questHighlightIndex = index;
//         if (questNameText != null) questNameText.text = item.itemName;

//         if (state == PopupState.Closed)
//         {
//             mode = DisplayMode.QuestPreview;
//             currentPage = InventoryPage.QuestItems;
//             SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, false);
//             SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, true);
//             SetIconsAndArrows(itemsVisible: false, questVisible: false);

//             RefreshQuestOpacity();
//             StartOpening();
//             RestartQuestPreviewTimer();
//         }
//         else if (state == PopupState.Open && mode == DisplayMode.QuestPreview)
//         {
//             RefreshQuestOpacity();
//             RestartQuestPreviewTimer();
//         }
//         else
//         {
//             RefreshQuestOpacity();
//         }
//     }

//     private void RestartQuestPreviewTimer()
//     {
//         if (questPreviewCoroutine != null) StopCoroutine(questPreviewCoroutine);
//         questPreviewCoroutine = StartCoroutine(AutoCloseQuestPreview());
//     }

//     private IEnumerator AutoCloseQuestPreview()
//     {
//         yield return new WaitForSeconds(questPickupPreviewDuration);

//         if (state == PopupState.Open && mode == DisplayMode.QuestPreview)
//         {
//             StartClosing();
//         }
//     }

//     private void CancelQuestPreviewTimer()
//     {
//         if (questPreviewCoroutine != null)
//         {
//             StopCoroutine(questPreviewCoroutine);
//             questPreviewCoroutine = null;
//         }
//     }

//     // ---------- Quest removal ----------

//     private void HandleQuestItemRemovalRequested(QuestItemData item, int index)
//     {
//         questHighlightIndex = index;
//         mode = DisplayMode.QuestPreview;

//         if (state == PopupState.Closed)
//         {
//             currentPage = InventoryPage.QuestItems;
//             SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, false);
//             SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, true);
//             SetIconsAndArrows(itemsVisible: false, questVisible: false);
//             if (questNameText != null) questNameText.text = item.itemName;

//             RefreshQuestOpacity();
//             StartOpening();
//         }
//         else
//         {
//             if (questNameText != null) questNameText.text = item.itemName;
//             RefreshQuestOpacity();
//         }

//         CancelQuestPreviewTimer();

//         if (index >= 0 && index < questSlotUIs.Count)
//         {
//             questSlotUIs[index].PlayRemovalAnimation(removalGrowScale, removalAnimationDuration, removalAnimationCurve, () =>
//             {
//                 if (QuestInventoryManager.Instance != null) QuestInventoryManager.Instance.ConfirmRemoval(index);
//                 StartCoroutine(CloseAfterQuestRemoval());
//             });
//         }
//     }

//     private IEnumerator CloseAfterQuestRemoval()
//     {
//         yield return new WaitForSeconds(removalCloseDelay);

//         if (state == PopupState.Open && mode == DisplayMode.QuestPreview)
//         {
//             StartClosing();
//         }
//     }

//     // ---------- Shared slot building ----------

//     private void BuildSlotsIfNeeded(RectTransform container, List<InventorySlotUI> slotList, int count, GameObject prefab)
//     {
//         if (slotList.Count == count) return;

//         foreach (Transform child in container)
//         {
//             child.gameObject.SetActive(false);
//             Destroy(child.gameObject);
//         }
//         slotList.Clear();

//         for (int i = 0; i < count; i++)
//         {
//             GameObject instance = Instantiate(prefab, container);
//             InventorySlotUI slotUI = instance.GetComponent<InventorySlotUI>();

//             if (slotUI == null)
//             {
//                 Debug.LogError("InventoryUI: slot prefab is missing an InventorySlotUI component.");
//                 continue;
//             }

//             slotList.Add(slotUI);
//         }

//         Canvas.ForceUpdateCanvases();
//     }
// }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controls the inventory popup: two pages (Items, QuestItems) with fade+slide transitions,
/// pickup previews, and (new) removal handling - when a dialogue delivery action removes an
/// item, this listens for InventoryManager/QuestInventoryManager's "removal requested" events,
/// opens the appropriate page, plays a grow-then-collapse animation on that specific slot
/// (InventorySlotUI.PlayRemovalAnimation), and only THEN calls back into the manager's
/// ConfirmRemoval() to actually delete the data and reorder the remaining items.
///
/// Selection icons and arrow indicators are only shown during manual Browse mode - never during
/// pickup previews OR removal displays - both toggled together via SetIconsAndArrows().
/// </summary>
public class InventoryUI : MonoBehaviour
{
    private enum PopupState { Closed, Opening, Open, Closing }
    private enum DisplayMode { Browse, ItemPreview, QuestPreview }
    private enum InventoryPage { Items, QuestItems }

    [Header("Panel / Animation")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float openCloseDuration = 0.15f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Vector3 maxScale = Vector3.one;

    [Header("Items Page")]
    [SerializeField] private RectTransform itemsPageRoot;
    [SerializeField] private CanvasGroup itemsPageCanvasGroup;
    [SerializeField] private RectTransform itemsSlotsContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private RectTransform itemsSelectionIcon;
    [SerializeField] private GameObject itemsArrowIndicator;
    [SerializeField] private TextMeshProUGUI itemsNameText;

    [Header("Quest Items Page")]
    [SerializeField] private RectTransform questPageRoot;
    [SerializeField] private CanvasGroup questPageCanvasGroup;
    [SerializeField] private RectTransform questSlotsContainer;
    [SerializeField] private GameObject questSlotPrefab;
    [SerializeField] private RectTransform questSelectionIcon;
    [SerializeField] private GameObject questArrowIndicator;
    [SerializeField] private TextMeshProUGUI questNameText;

    [Header("Page Transition Animation")]
    [SerializeField] private float pageTransitionDuration = 0.2f;
    [SerializeField] private float pageSlideDistance = 40f;
    [SerializeField] private AnimationCurve pageTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Pickup Preview")]
    [SerializeField] private float itemPickupPreviewDuration = 2.5f;
    [SerializeField] private float questPickupPreviewDuration = 2.5f;

    [Header("Removal Animation")]
    [Tooltip("How much larger (as a multiplier) the item briefly grows before collapsing to zero.")]
    [SerializeField] private float removalGrowScale = 1.3f;
    [SerializeField] private float removalAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve removalAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("How long the popup stays open after a removal finishes collapsing, before auto-closing.")]
    [SerializeField] private float removalCloseDelay = 0.4f;

    [Header("Opacity")]
    [Range(0f, 1f)]
    [SerializeField] private float dimmedAlpha = 0.5f;

    private PopupState state = PopupState.Closed;
    private DisplayMode mode = DisplayMode.Browse;
    private InventoryPage currentPage = InventoryPage.Items;
    private bool isPageTransitioning;

    private readonly List<InventorySlotUI> itemSlotUIs = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> questSlotUIs = new List<InventorySlotUI>();
    private ItemData[] currentItemSlots;
    private List<QuestItemData> currentQuestItems;

    private int itemSelectedIndex;
    private int questSelectedIndex;
    private int itemHighlightIndex = -1;
    private int questHighlightIndex = -1;

    private Vector2 itemsPageBasePos;
    private Vector2 questPageBasePos;

    private Coroutine animCoroutine;
    private Coroutine pageTransitionCoroutine;
    private Coroutine itemPreviewCoroutine;
    private Coroutine questPreviewCoroutine;
    private bool isSubscribed;

    private void Start()
    {
        if (panelCanvasGroup == null && panelRoot != null) panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            Debug.LogError("InventoryUI: no CanvasGroup found on panelRoot. Add a CanvasGroup component to the panel.");
        }

        if (panelRoot != null) panelRoot.localScale = Vector3.zero;

        if (itemsPageRoot != null) itemsPageBasePos = itemsPageRoot.anchoredPosition;
        if (questPageRoot != null) questPageBasePos = questPageRoot.anchoredPosition;

        SetPanelVisible(false);
        SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
        SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
        SetIconsAndArrows(itemsVisible: false, questVisible: false);

        TrySubscribe();
    }

    private void OnEnable() => TrySubscribe();

    private void OnDisable()
    {
        if (!isSubscribed) return;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleItemInventoryChanged;
            InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
            InventoryManager.Instance.OnItemRemovalRequested -= HandleItemRemovalRequested;
        }
        if (QuestInventoryManager.Instance != null)
        {
            QuestInventoryManager.Instance.OnQuestInventoryChanged -= HandleQuestInventoryChanged;
            QuestInventoryManager.Instance.OnQuestItemAdded -= HandleQuestItemAdded;
            QuestInventoryManager.Instance.OnQuestItemRemovalRequested -= HandleQuestItemRemovalRequested;
        }
        isSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (InventoryManager.Instance == null || QuestInventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryUI: InventoryManager or QuestInventoryManager instance is null.");
            return;
        }

        InventoryManager.Instance.OnInventoryChanged += HandleItemInventoryChanged;
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
        InventoryManager.Instance.OnItemRemovalRequested += HandleItemRemovalRequested;
        QuestInventoryManager.Instance.OnQuestInventoryChanged += HandleQuestInventoryChanged;
        QuestInventoryManager.Instance.OnQuestItemAdded += HandleQuestItemAdded;
        QuestInventoryManager.Instance.OnQuestItemRemovalRequested += HandleQuestItemRemovalRequested;
        isSubscribed = true;

        if (InventoryManager.Instance.IsInitialized)
        {
            HandleItemInventoryChanged(InventoryManager.Instance.GetSlots());
        }
        HandleQuestInventoryChanged(new List<QuestItemData>(QuestInventoryManager.Instance.Items));
    }

    private void Update()
    {
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleTabPressed();
        }

        if (state == PopupState.Open && mode == DisplayMode.Browse && !isPageTransitioning)
        {
            if (currentPage == InventoryPage.Items)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) MoveItemSelection(1);
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveItemSelection(-1);
                else if (Input.GetKeyDown(KeyCode.UpArrow)) BeginPageTransition(InventoryPage.QuestItems);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) MoveQuestSelection(1);
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveQuestSelection(-1);
                else if (Input.GetKeyDown(KeyCode.DownArrow)) BeginPageTransition(InventoryPage.Items);
            }
        }
    }

    // ---------- Selection icon + arrow indicator visibility ----------

    private void SetIconsAndArrows(bool itemsVisible, bool questVisible)
    {
        if (itemsSelectionIcon != null) itemsSelectionIcon.gameObject.SetActive(itemsVisible);
        if (itemsArrowIndicator != null) itemsArrowIndicator.gameObject.SetActive(itemsVisible);

        if (questSelectionIcon != null) questSelectionIcon.gameObject.SetActive(questVisible);
        if (questArrowIndicator != null) questArrowIndicator.gameObject.SetActive(questVisible);
    }

    // ---------- Tab toggle / panel state machine ----------

    private void HandleTabPressed()
    {
        switch (state)
        {
            case PopupState.Closed:
                OpenBrowseMode();
                break;

            case PopupState.Open:
                if (mode == DisplayMode.ItemPreview)
                {
                    CancelItemPreviewTimer();
                    EnterBrowseMode(InventoryPage.Items, itemHighlightIndex);
                }
                else if (mode == DisplayMode.QuestPreview)
                {
                    CancelQuestPreviewTimer();
                    EnterBrowseMode(InventoryPage.QuestItems, questHighlightIndex);
                }
                else
                {
                    StartClosing();
                }
                break;
        }
    }

    private void OpenBrowseMode()
    {
        mode = DisplayMode.Browse;
        currentPage = InventoryPage.Items;
        itemSelectedIndex = 0;

        SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
        SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
        SetIconsAndArrows(itemsVisible: true, questVisible: false);

        RefreshItemOpacity();
        StartOpening();

        Canvas.ForceUpdateCanvases();
        UpdateItemSelectionIconPosition();
        UpdateItemNameLabel();
    }

    private void EnterBrowseMode(InventoryPage page, int highlightIndex)
    {
        mode = DisplayMode.Browse;
        currentPage = page;

        if (page == InventoryPage.Items)
        {
            itemSelectedIndex = highlightIndex >= 0 ? highlightIndex : 0;
            SetIconsAndArrows(itemsVisible: true, questVisible: false);
            RefreshItemOpacity();
            Canvas.ForceUpdateCanvases();
            UpdateItemSelectionIconPosition();
            UpdateItemNameLabel();
        }
        else
        {
            questSelectedIndex = highlightIndex >= 0 ? highlightIndex : 0;
            SetIconsAndArrows(itemsVisible: false, questVisible: true);
            RefreshQuestOpacity();
            Canvas.ForceUpdateCanvases();
            UpdateQuestSelectionIconPosition();
            UpdateQuestNameLabel();
        }
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

    private void SetPanelVisible(bool visible)
    {
        if (panelCanvasGroup == null) return;

        panelCanvasGroup.alpha = visible ? 1f : 0f;
        panelCanvasGroup.interactable = visible;
        panelCanvasGroup.blocksRaycasts = visible;
    }

    // ---------- Page transition (Up/Down between Items and QuestItems) ----------

    private void BeginPageTransition(InventoryPage targetPage)
    {
        if (isPageTransitioning || targetPage == currentPage) return;

        bool movingToQuest = targetPage == InventoryPage.QuestItems;

        RectTransform outRoot = movingToQuest ? itemsPageRoot : questPageRoot;
        CanvasGroup outGroup = movingToQuest ? itemsPageCanvasGroup : questPageCanvasGroup;
        Vector2 outBase = movingToQuest ? itemsPageBasePos : questPageBasePos;

        RectTransform inRoot = movingToQuest ? questPageRoot : itemsPageRoot;
        CanvasGroup inGroup = movingToQuest ? questPageCanvasGroup : itemsPageCanvasGroup;
        Vector2 inBase = movingToQuest ? questPageBasePos : itemsPageBasePos;

        bool moveDown = movingToQuest;

        SetIconsAndArrows(itemsVisible: false, questVisible: false);

        if (pageTransitionCoroutine != null) StopCoroutine(pageTransitionCoroutine);
        pageTransitionCoroutine = StartCoroutine(PageTransition(outRoot, outGroup, outBase, inRoot, inGroup, inBase, moveDown, targetPage));
    }

    private IEnumerator PageTransition(RectTransform outRoot, CanvasGroup outGroup, Vector2 outBase,
                                        RectTransform inRoot, CanvasGroup inGroup, Vector2 inBase,
                                        bool moveDown, InventoryPage targetPage)
    {
        isPageTransitioning = true;

        Vector2 exitOffset = moveDown ? new Vector2(0f, -pageSlideDistance) : new Vector2(0f, pageSlideDistance);
        Vector2 enterStartOffset = moveDown ? new Vector2(0f, pageSlideDistance) : new Vector2(0f, -pageSlideDistance);

        Vector2 outStart = outRoot.anchoredPosition;
        Vector2 outTarget = outBase + exitOffset;
        Vector2 inStart = inBase + enterStartOffset;
        Vector2 inTarget = inBase;

        inRoot.anchoredPosition = inStart;
        inGroup.alpha = 0f;
        inGroup.interactable = false;
        inGroup.blocksRaycasts = false;

        float outStartAlpha = outGroup.alpha;
        float elapsed = 0f;

        while (elapsed < pageTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float n = Mathf.Clamp01(elapsed / pageTransitionDuration);
            float curved = pageTransitionCurve.Evaluate(n);

            outRoot.anchoredPosition = Vector2.LerpUnclamped(outStart, outTarget, curved);
            outGroup.alpha = Mathf.LerpUnclamped(outStartAlpha, 0f, curved);

            inRoot.anchoredPosition = Vector2.LerpUnclamped(inStart, inTarget, curved);
            inGroup.alpha = Mathf.LerpUnclamped(0f, 1f, curved);

            yield return null;
        }

        outRoot.anchoredPosition = outBase;
        outGroup.alpha = 0f;
        outGroup.interactable = false;
        outGroup.blocksRaycasts = false;

        inRoot.anchoredPosition = inTarget;
        inGroup.alpha = 1f;
        inGroup.interactable = true;
        inGroup.blocksRaycasts = true;

        currentPage = targetPage;
        isPageTransitioning = false;

        if (targetPage == InventoryPage.QuestItems)
        {
            questSelectedIndex = 0;
            SetIconsAndArrows(itemsVisible: false, questVisible: true);
            RefreshQuestOpacity();
            Canvas.ForceUpdateCanvases();
            UpdateQuestSelectionIconPosition();
            UpdateQuestNameLabel();
        }
        else
        {
            SetIconsAndArrows(itemsVisible: true, questVisible: false);
            RefreshItemOpacity();
            Canvas.ForceUpdateCanvases();
            UpdateItemSelectionIconPosition();
            UpdateItemNameLabel();
        }
    }

    private void SetPageInstant(RectTransform root, CanvasGroup group, Vector2 basePos, bool visible)
    {
        if (root == null || group == null) return;

        root.anchoredPosition = basePos;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    // ---------- Items page navigation ----------

    private void MoveItemSelection(int direction)
    {
        if (itemSlotUIs.Count == 0) return;

        itemSelectedIndex = Mathf.Clamp(itemSelectedIndex + direction, 0, itemSlotUIs.Count - 1);
        RefreshItemOpacity();
        UpdateItemSelectionIconPosition();
        UpdateItemNameLabel();
    }

    private void UpdateItemSelectionIconPosition()
    {
        if (itemsSelectionIcon == null || itemSlotUIs.Count == 0) return;
        if (itemSelectedIndex < 0 || itemSelectedIndex >= itemSlotUIs.Count) return;

        itemsSelectionIcon.position = itemSlotUIs[itemSelectedIndex].RectTransform.position;
    }

    private void UpdateItemNameLabel()
    {
        if (itemsNameText == null || currentItemSlots == null) return;

        int index = mode == DisplayMode.ItemPreview ? itemHighlightIndex : itemSelectedIndex;

        if (index >= 0 && index < currentItemSlots.Length && currentItemSlots[index] != null)
        {
            itemsNameText.text = currentItemSlots[index].itemName;
        }
        else
        {
            itemsNameText.text = "";
        }
    }

    private void RefreshItemOpacity()
    {
        int activeIndex = mode == DisplayMode.ItemPreview ? itemHighlightIndex : itemSelectedIndex;

        for (int i = 0; i < itemSlotUIs.Count; i++)
        {
            itemSlotUIs[i].SetOpacity(i == activeIndex ? 1f : dimmedAlpha);
        }
    }

    // ---------- Quest page navigation ----------

    private void MoveQuestSelection(int direction)
    {
        if (questSlotUIs.Count == 0) return;

        questSelectedIndex = Mathf.Clamp(questSelectedIndex + direction, 0, questSlotUIs.Count - 1);
        RefreshQuestOpacity();
        UpdateQuestSelectionIconPosition();
        UpdateQuestNameLabel();
    }

    private void UpdateQuestSelectionIconPosition()
    {
        if (questSelectionIcon == null || questSlotUIs.Count == 0) return;
        if (questSelectedIndex < 0 || questSelectedIndex >= questSlotUIs.Count) return;

        questSelectionIcon.position = questSlotUIs[questSelectedIndex].RectTransform.position;
    }

    private void UpdateQuestNameLabel()
    {
        if (questNameText == null || currentQuestItems == null) return;

        int index = mode == DisplayMode.QuestPreview ? questHighlightIndex : questSelectedIndex;

        if (index >= 0 && index < currentQuestItems.Count && currentQuestItems[index] != null)
        {
            questNameText.text = currentQuestItems[index].itemName;
        }
        else
        {
            questNameText.text = "";
        }
    }

    private void RefreshQuestOpacity()
    {
        int activeIndex = mode == DisplayMode.QuestPreview ? questHighlightIndex : questSelectedIndex;

        for (int i = 0; i < questSlotUIs.Count; i++)
        {
            questSlotUIs[i].SetOpacity(i == activeIndex ? 1f : dimmedAlpha);
        }
    }

    // ---------- Items inventory events ----------

    private void HandleItemInventoryChanged(ItemData[] slots)
    {
        currentItemSlots = slots;
        BuildSlotsIfNeeded(itemsSlotsContainer, itemSlotUIs, slots.Length, itemSlotPrefab);
        for (int i = 0; i < itemSlotUIs.Count && i < slots.Length; i++)
        {
            itemSlotUIs[i].SetItem(slots[i]);
        }
        RefreshItemOpacity();
    }

    private void HandleItemAdded(ItemData item, int slotIndex)
    {
        itemHighlightIndex = slotIndex;
        if (itemsNameText != null) itemsNameText.text = item.itemName;

        if (state == PopupState.Closed)
        {
            mode = DisplayMode.ItemPreview;
            currentPage = InventoryPage.Items;
            SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
            SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
            SetIconsAndArrows(itemsVisible: false, questVisible: false);

            RefreshItemOpacity();
            StartOpening();
            RestartItemPreviewTimer();
        }
        else if (state == PopupState.Open && mode == DisplayMode.ItemPreview)
        {
            RefreshItemOpacity();
            RestartItemPreviewTimer();
        }
        else
        {
            RefreshItemOpacity();
        }
    }

    private void RestartItemPreviewTimer()
    {
        if (itemPreviewCoroutine != null) StopCoroutine(itemPreviewCoroutine);
        itemPreviewCoroutine = StartCoroutine(AutoCloseItemPreview());
    }

    private IEnumerator AutoCloseItemPreview()
    {
        yield return new WaitForSeconds(itemPickupPreviewDuration);

        if (state == PopupState.Open && mode == DisplayMode.ItemPreview)
        {
            StartClosing();
        }
    }

    private void CancelItemPreviewTimer()
    {
        if (itemPreviewCoroutine != null)
        {
            StopCoroutine(itemPreviewCoroutine);
            itemPreviewCoroutine = null;
        }
    }

    // ---------- Items removal ----------

    private void HandleItemRemovalRequested(ItemData item, int index)
    {
        itemHighlightIndex = index;
        mode = DisplayMode.ItemPreview; // reuses the same "highlighted, no cursor" display rules as a pickup preview

        if (state == PopupState.Closed)
        {
            currentPage = InventoryPage.Items;
            SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, true);
            SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, false);
            SetIconsAndArrows(itemsVisible: false, questVisible: false);
            if (itemsNameText != null) itemsNameText.text = item.itemName;

            RefreshItemOpacity();
            StartOpening();
        }
        else
        {
            if (itemsNameText != null) itemsNameText.text = item.itemName;
            RefreshItemOpacity();
        }

        CancelItemPreviewTimer(); // a removal in progress should not be interrupted by a preview auto-close

        if (index >= 0 && index < itemSlotUIs.Count)
        {
            itemSlotUIs[index].PlayRemovalAnimation(removalGrowScale, removalAnimationDuration, removalAnimationCurve, () =>
            {
                if (InventoryManager.Instance != null) InventoryManager.Instance.ConfirmRemoval(index);
                StartCoroutine(CloseAfterRemoval());
            });
        }
    }

    private IEnumerator CloseAfterRemoval()
    {
        yield return new WaitForSeconds(removalCloseDelay);

        if (state == PopupState.Open && mode == DisplayMode.ItemPreview)
        {
            StartClosing();
        }
    }

    // ---------- Quest inventory events ----------

    private void HandleQuestInventoryChanged(List<QuestItemData> items)
    {
        currentQuestItems = items;
        int slotCount = Mathf.Max(1, items.Count);

        BuildSlotsIfNeeded(questSlotsContainer, questSlotUIs, slotCount, questSlotPrefab);

        for (int i = 0; i < questSlotUIs.Count; i++)
        {
            if (i < items.Count) questSlotUIs[i].SetItem(items[i]);
            else questSlotUIs[i].Clear();
        }

        RefreshQuestOpacity();
    }

    private void HandleQuestItemAdded(QuestItemData item, int index)
    {
        questHighlightIndex = index;
        if (questNameText != null) questNameText.text = item.itemName;

        if (state == PopupState.Closed)
        {
            mode = DisplayMode.QuestPreview;
            currentPage = InventoryPage.QuestItems;
            SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, false);
            SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, true);
            SetIconsAndArrows(itemsVisible: false, questVisible: false);

            RefreshQuestOpacity();
            StartOpening();
            RestartQuestPreviewTimer();
        }
        else if (state == PopupState.Open && mode == DisplayMode.QuestPreview)
        {
            RefreshQuestOpacity();
            RestartQuestPreviewTimer();
        }
        else
        {
            RefreshQuestOpacity();
        }
    }

    private void RestartQuestPreviewTimer()
    {
        if (questPreviewCoroutine != null) StopCoroutine(questPreviewCoroutine);
        questPreviewCoroutine = StartCoroutine(AutoCloseQuestPreview());
    }

    private IEnumerator AutoCloseQuestPreview()
    {
        yield return new WaitForSeconds(questPickupPreviewDuration);

        if (state == PopupState.Open && mode == DisplayMode.QuestPreview)
        {
            StartClosing();
        }
    }

    private void CancelQuestPreviewTimer()
    {
        if (questPreviewCoroutine != null)
        {
            StopCoroutine(questPreviewCoroutine);
            questPreviewCoroutine = null;
        }
    }

    // ---------- Quest removal ----------

    private void HandleQuestItemRemovalRequested(QuestItemData item, int index)
    {
        questHighlightIndex = index;
        mode = DisplayMode.QuestPreview;

        if (state == PopupState.Closed)
        {
            currentPage = InventoryPage.QuestItems;
            SetPageInstant(itemsPageRoot, itemsPageCanvasGroup, itemsPageBasePos, false);
            SetPageInstant(questPageRoot, questPageCanvasGroup, questPageBasePos, true);
            SetIconsAndArrows(itemsVisible: false, questVisible: false);
            if (questNameText != null) questNameText.text = item.itemName;

            RefreshQuestOpacity();
            StartOpening();
        }
        else
        {
            if (questNameText != null) questNameText.text = item.itemName;
            RefreshQuestOpacity();
        }

        CancelQuestPreviewTimer();

        if (index >= 0 && index < questSlotUIs.Count)
        {
            questSlotUIs[index].PlayRemovalAnimation(removalGrowScale, removalAnimationDuration, removalAnimationCurve, () =>
            {
                if (QuestInventoryManager.Instance != null) QuestInventoryManager.Instance.ConfirmRemoval(index);
                StartCoroutine(CloseAfterQuestRemoval());
            });
        }
    }

    private IEnumerator CloseAfterQuestRemoval()
    {
        yield return new WaitForSeconds(removalCloseDelay);

        if (state == PopupState.Open && mode == DisplayMode.QuestPreview)
        {
            StartClosing();
        }
    }

    // ---------- Shared slot building ----------

    private void BuildSlotsIfNeeded(RectTransform container, List<InventorySlotUI> slotList, int count, GameObject prefab)
    {
        if (slotList.Count == count) return;

        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        slotList.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(prefab, container);
            InventorySlotUI slotUI = instance.GetComponent<InventorySlotUI>();

            if (slotUI == null)
            {
                Debug.LogError("InventoryUI: slot prefab is missing an InventorySlotUI component.");
                continue;
            }

            slotList.Add(slotUI);
        }

        Canvas.ForceUpdateCanvases();
    }
}