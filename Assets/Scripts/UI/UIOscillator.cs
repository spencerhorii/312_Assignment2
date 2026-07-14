using UnityEngine;

/// <summary>
/// Gently oscillates a RectTransform up and down around its resting anchoredPosition, using a
/// sine wave (same approach as WorldTooltip's world-space oscillation, just for UI space).
/// Fully generic and reusable - attach to any UI icon that needs this "bob up and down" idle
/// animation (the inventory page arrow indicators, and anything similar added later).
///
/// Starts/stops automatically with the GameObject's enabled state, so toggling this object
/// on/off (e.g. show/hide an indicator) naturally starts and stops the animation.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIOscillator : MonoBehaviour
{
    [Tooltip("How far up/down (in UI units) the object moves from its resting position.")]
    [SerializeField] private float amplitude = 6f;
    [Tooltip("How fast it oscillates, in full up-down cycles per second.")]
    [SerializeField] private float speed = 1f;

    private RectTransform rect;
    private Vector2 basePosition;
    private float elapsed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        basePosition = rect.anchoredPosition;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float offsetY = Mathf.Sin(elapsed * speed * Mathf.PI * 2f) * amplitude;
        rect.anchoredPosition = basePosition + new Vector2(0f, offsetY);
    }

    private void OnDisable()
    {
        if (rect != null) rect.anchoredPosition = basePosition;
    }
}