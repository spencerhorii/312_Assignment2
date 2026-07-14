using UnityEngine;

/// <summary>
/// Gently pulses a RectTransform's scale larger and smaller, with easing at both ends of the
/// swing. Fully generic and reusable - attach to any UI Image (or any RectTransform) that
/// should have a subtle "breathing" idle animation, such as the inventory/dialogue selection
/// icons.
///
/// Starts/stops automatically with the GameObject's enabled state, so toggling this object
/// on/off naturally starts and stops the animation and restores its original scale.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIScalePulse : MonoBehaviour
{
    [Tooltip("How far the scale swings from its base value, as a fraction (e.g. 0.08 = pulses " +
             "between 92% and 108% of its normal size). Keep this small for a subtle effect.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float pulseAmplitude = 0.08f;

    [Tooltip("How fast it pulses, in full grow-and-shrink cycles per second.")]
    [SerializeField] private float pulseSpeed = 1f;

    [Tooltip("Easing applied across each half-cycle (min -> max, then max -> min). " +
             "EaseInOut gives a smooth, organic swing rather than a linear one.")]
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rect;
    private Vector3 baseScale;
    private float elapsed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        baseScale = rect.localScale;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime * pulseSpeed;

        // PingPong sweeps 0 -> 1 -> 0 repeatedly; feeding that through the curve gives an
        // eased grow-then-shrink-then-grow motion rather than an abrupt snap at the peaks.
        float t = Mathf.PingPong(elapsed, 1f);
        float curved = pulseCurve.Evaluate(t);

        float scaleMultiplier = 1f + Mathf.Lerp(-pulseAmplitude, pulseAmplitude, curved);
        rect.localScale = baseScale * scaleMultiplier;
    }

    private void OnDisable()
    {
        if (rect != null) rect.localScale = baseScale;
    }
}