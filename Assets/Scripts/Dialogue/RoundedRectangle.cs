using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A solid-color rounded rectangle drawn as a procedural mesh, instead of using a sprite image.
/// Regenerates automatically whenever its RectTransform is resized (standard Graphic behaviour),
/// which is what makes the dialogue bubble's dynamic sizing work cleanly - just change the
/// RectTransform's sizeDelta and this redraws itself to match.
///
/// Attach to a UI GameObject the same way you would an Image. Color comes from the inherited
/// Graphic.color field (visible in the Inspector as "Color").
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class RoundedRectangle : MaskableGraphic
{
    [Tooltip("Corner radius in UI units. Automatically clamped so it can never exceed half the " +
             "shape's width or height (preventing malformed geometry on very small/thin bubbles).")]
    [SerializeField] private float cornerRadius = 16f;

    [Tooltip("How many mesh segments make up each rounded corner. Higher = smoother curve, " +
             "at a small extra vertex cost. 8-12 looks smooth for typical UI sizes.")]
    [Range(2, 32)]
    [SerializeField] private int cornerSegments = 8;

    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public int CornerSegments
    {
        get => cornerSegments;
        set
        {
            cornerSegments = Mathf.Clamp(value, 2, 32);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        float width = r.width;
        float height = r.height;

        if (width <= 0f || height <= 0f) return;

        float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(width, height) * 0.5f);

        // Centre point of each rounded corner's arc, in the order: top-right, top-left, bottom-left, bottom-right.
        Vector2[] cornerCenters =
        {
            new Vector2(r.xMax - radius, r.yMax - radius),
            new Vector2(r.xMin + radius, r.yMax - radius),
            new Vector2(r.xMin + radius, r.yMin + radius),
            new Vector2(r.xMax - radius, r.yMin + radius),
        };

        // Each corner sweeps a 90-degree arc, starting at 0/90/180/270 degrees respectively,
        // so the arcs join up into one continuous perimeter loop.
        List<Vector2> perimeter = new List<Vector2>();
        for (int c = 0; c < 4; c++)
        {
            float startAngle = 90f * c;
            for (int i = 0; i <= cornerSegments; i++)
            {
                float angle = Mathf.Deg2Rad * (startAngle + (90f * i / cornerSegments));
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                perimeter.Add(cornerCenters[c] + offset);
            }
        }

        // Triangle fan from the rect's centre out to each perimeter point.
        Vector2 center = new Vector2(r.xMin + width * 0.5f, r.yMin + height * 0.5f);
        vh.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < perimeter.Count; i++)
        {
            vh.AddVert(perimeter[i], color, Vector2.zero);
        }

        for (int i = 1; i < perimeter.Count; i++)
        {
            vh.AddTriangle(0, i, i + 1);
        }
        vh.AddTriangle(0, perimeter.Count, 1); // close the fan back to the first perimeter point
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
