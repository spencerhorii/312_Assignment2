using UnityEngine;
using TMPro;

/// <summary>
/// Attach to any GameObject with a TMP_Text component (TextMeshProUGUI for UI, or world-space
/// TextMeshPro) to give its text a "line boil" effect - rapidly alternating between two font
/// assets to fake the jittery, hand-drawn line quality of old cartoon animation.
///
/// Fully modular: works on inventory labels, dialogue text, tooltips, or any other TMP text in
/// the game, just by adding this component to that object. No other script needs to know it
/// exists - it reads/writes only its own TMP_Text's font property.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TextFontBoil : MonoBehaviour
{
    [Header("Fonts")]
    [Tooltip("First font to alternate to. Leave empty to use whatever font the TMP_Text already has assigned when this starts.")]
    [SerializeField] private TMP_FontAsset fontA;
    [Tooltip("Second font to alternate to (e.g. MANIC-Alternates2 SDF, if fontA is MANIC-Alternates3 SDF).")]
    [SerializeField] private TMP_FontAsset fontB;

    [Header("Timing")]
    [Tooltip("How many times per second the font swaps.")]
    [SerializeField] private float swapsPerSecond = 6f;
    [Tooltip("If checked, this instance starts at a random point in its cycle, so multiple boiling " +
             "texts on screen at once don't all flicker in perfect unison.")]
    [SerializeField] private bool randomizeStartOffset = true;

    private TMP_Text text;
    private float timer;
    private bool showingFontA = true;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();

        if (fontA == null && text != null)
        {
            fontA = text.font;
        }

        if (randomizeStartOffset && swapsPerSecond > 0f)
        {
            timer = Random.Range(0f, 1f / swapsPerSecond);
        }
    }

    private void OnEnable()
    {
        ApplyFont(showingFontA ? fontA : fontB);
    }

    private void Update()
    {
        if (text == null || fontA == null || fontB == null || swapsPerSecond <= 0f) return;

        timer += Time.deltaTime;
        float interval = 1f / swapsPerSecond;

        if (timer >= interval)
        {
            timer -= interval;
            showingFontA = !showingFontA;
            ApplyFont(showingFontA ? fontA : fontB);
        }
    }

    private void ApplyFont(TMP_FontAsset font)
    {
        if (text == null || font == null) return;
        text.font = font;
    }
}