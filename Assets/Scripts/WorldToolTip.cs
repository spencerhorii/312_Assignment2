// using System.Collections;
// using UnityEngine;
// using TMPro;

// /// <summary>
// /// Reusable world-space tooltip that scales in/out with easing and, once fully visible,
// /// gently oscillates up and down. Used by both Item.cs ("Pickup (E)" / "Inventory Full")
// /// and NPC.cs ("Speak (E)"), so the animation logic only lives in one place.
// /// </summary>
// public class WorldTooltip : MonoBehaviour
// {
//     private enum TooltipState { Hidden, ScalingIn, Visible, ScalingOut }

//     [Header("References")]
//     [Tooltip("World-space TextMeshPro (3D text, not UI) that displays the tooltip.")]
//     [SerializeField] private TextMeshPro tooltipText;

//     [Header("Scale Animation")]
//     [SerializeField] private float scaleDuration = 0.15f;
//     [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//     [Tooltip("The scale the tooltip animates to when fully visible.")]
//     [SerializeField] private Vector3 maxScale = Vector3.one;

//     [Header("Oscillation")]
//     [Tooltip("How far up/down (in local units) the tooltip rocks from its base position.")]
//     [SerializeField] private float oscillationAmplitude = 0.05f;
//     [Tooltip("How fast the tooltip oscillates, in full up-down cycles per second.")]
//     [SerializeField] private float oscillationSpeed = 1f;

//     private TooltipState state = TooltipState.Hidden;
//     private Vector3 baseLocalPosition;
//     private Coroutine scaleCoroutine;
//     private Coroutine oscillateCoroutine;

//     private void Awake()
//     {
//         if (tooltipText != null)
//         {
//             baseLocalPosition = tooltipText.transform.localPosition;
//             tooltipText.transform.localScale = Vector3.zero;
//             tooltipText.gameObject.SetActive(false);
//         }
//     }

//     /// <summary>Sets the tooltip's displayed text. Safe to call whether or not it's currently visible.</summary>
//     public void SetText(string text)
//     {
//         if (tooltipText != null) tooltipText.text = text;
//     }

//     /// <summary>Animates the tooltip in (scale 0 -> maxScale), then begins the idle oscillation.</summary>
//     public void Show()
//     {
//         if (tooltipText == null) return;

//         if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
//         StopOscillation();

//         tooltipText.gameObject.SetActive(true);
//         state = TooltipState.ScalingIn;
//         scaleCoroutine = StartCoroutine(ScaleTo(maxScale, TooltipState.Visible));
//     }

//     /// <summary>Animates the tooltip out (scale -> 0), stopping any oscillation immediately.</summary>
//     public void Hide()
//     {
//         if (tooltipText == null) return;

//         if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
//         StopOscillation();

//         // Reset to base position in case we're interrupting mid-oscillation.
//         tooltipText.transform.localPosition = baseLocalPosition;

//         state = TooltipState.ScalingOut;
//         scaleCoroutine = StartCoroutine(ScaleTo(Vector3.zero, TooltipState.Hidden));
//     }

//     private IEnumerator ScaleTo(Vector3 targetScale, TooltipState endState)
//     {
//         Transform t = tooltipText.transform;
//         Vector3 startScale = t.localScale;
//         float elapsed = 0f;

//         while (elapsed < scaleDuration)
//         {
//             elapsed += Time.deltaTime;
//             float normalized = Mathf.Clamp01(elapsed / scaleDuration);
//             float curved = scaleCurve.Evaluate(normalized);
//             t.localScale = Vector3.LerpUnclamped(startScale, targetScale, curved);
//             yield return null;
//         }

//         t.localScale = targetScale;
//         state = endState;

//         if (endState == TooltipState.Visible)
//         {
//             oscillateCoroutine = StartCoroutine(Oscillate());
//         }
//         else if (endState == TooltipState.Hidden)
//         {
//             tooltipText.gameObject.SetActive(false);
//         }
//     }

//     private IEnumerator Oscillate()
//     {
//         float elapsed = 0f;
//         while (true)
//         {
//             elapsed += Time.deltaTime;
//             float offsetY = Mathf.Sin(elapsed * oscillationSpeed * Mathf.PI * 2f) * oscillationAmplitude;
//             tooltipText.transform.localPosition = baseLocalPosition + new Vector3(0f, offsetY, 0f);
//             yield return null;
//         }
//     }

//     private void StopOscillation()
//     {
//         if (oscillateCoroutine != null)
//         {
//             StopCoroutine(oscillateCoroutine);
//             oscillateCoroutine = null;
//         }
//     }
// }

using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable world-space tooltip that scales in/out with easing and, once fully visible,
/// gently oscillates up and down. Used by both Item.cs ("Pickup (E)" / "Inventory Full")
/// and NPC.cs ("Speak (E)"), so the animation logic only lives in one place.
/// </summary>
public class WorldTooltip : MonoBehaviour
{
    private enum TooltipState { Hidden, ScalingIn, Visible, ScalingOut }

    [Header("References")]
    [Tooltip("World-space TextMeshPro (3D text, not UI) that displays the tooltip.")]
    [SerializeField] private TextMeshPro tooltipText;

    [Header("Scale Animation")]
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("The scale the tooltip animates to when fully visible.")]
    [SerializeField] private Vector3 maxScale = Vector3.one;

    [Header("Oscillation")]
    [Tooltip("How far up/down (in local units) the tooltip rocks from its base position.")]
    [SerializeField] private float oscillationAmplitude = 0.05f;
    [Tooltip("How fast the tooltip oscillates, in full up-down cycles per second.")]
    [SerializeField] private float oscillationSpeed = 1f;

    [Header("Sound")]
    [Tooltip("Played when the tooltip begins appearing.")]
    [SerializeField] private AudioClip showSound;
    [Tooltip("Played when the tooltip begins disappearing.")]
    [SerializeField] private AudioClip hideSound;

    private TooltipState state = TooltipState.Hidden;
    private Vector3 baseLocalPosition;
    private Coroutine scaleCoroutine;
    private Coroutine oscillateCoroutine;

    private void Awake()
    {
        if (tooltipText != null)
        {
            baseLocalPosition = tooltipText.transform.localPosition;
            tooltipText.transform.localScale = Vector3.zero;
            tooltipText.gameObject.SetActive(false);
        }
    }

    /// <summary>Sets the tooltip's displayed text. Safe to call whether or not it's currently visible.</summary>
    public void SetText(string text)
    {
        if (tooltipText != null) tooltipText.text = text;
    }

    /// <summary>Animates the tooltip in (scale 0 -> maxScale), then begins the idle oscillation.</summary>
    public void Show()
    {
        if (tooltipText == null) return;

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        StopOscillation();

        tooltipText.gameObject.SetActive(true);
        state = TooltipState.ScalingIn;
        scaleCoroutine = StartCoroutine(ScaleTo(maxScale, TooltipState.Visible));
    }

    /// <summary>Animates the tooltip out (scale -> 0), stopping any oscillation immediately.</summary>
    public void Hide()
    {
        if (tooltipText == null) return;

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        StopOscillation();

        // Reset to base position in case we're interrupting mid-oscillation.
        tooltipText.transform.localPosition = baseLocalPosition;

        state = TooltipState.ScalingOut;
        scaleCoroutine = StartCoroutine(ScaleTo(Vector3.zero, TooltipState.Hidden));
    }

    private IEnumerator ScaleTo(Vector3 targetScale, TooltipState endState)
    {
        Transform t = tooltipText.transform;
        Vector3 startScale = t.localScale;
        float elapsed = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / scaleDuration);
            float curved = scaleCurve.Evaluate(normalized);
            t.localScale = Vector3.LerpUnclamped(startScale, targetScale, curved);
            yield return null;
        }

        t.localScale = targetScale;
        state = endState;

        if (endState == TooltipState.Visible)
        {
            oscillateCoroutine = StartCoroutine(Oscillate());
        }
        else if (endState == TooltipState.Hidden)
        {
            tooltipText.gameObject.SetActive(false);
        }
    }

    private IEnumerator Oscillate()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float offsetY = Mathf.Sin(elapsed * oscillationSpeed * Mathf.PI * 2f) * oscillationAmplitude;
            tooltipText.transform.localPosition = baseLocalPosition + new Vector3(0f, offsetY, 0f);
            yield return null;
        }
    }

    private void StopOscillation()
    {
        if (oscillateCoroutine != null)
        {
            StopCoroutine(oscillateCoroutine);
            oscillateCoroutine = null;
        }
    }
}