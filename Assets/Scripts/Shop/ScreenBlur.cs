using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Approximates a screen blur without any custom shader, by repeatedly downsampling the
/// camera's rendered frame through bilinear-filtered Blit passes (GPU bilinear filtering
/// naturally softens the image each pass) and displaying the result on a full-screen RawImage.
///
/// Captures a single snapshot on demand via Capture() - appropriate for a modal like the shop
/// UI where the world is frozen behind it (movement disabled), rather than continuously
/// re-blurring every frame. If the background needs to keep animating through the blur in
/// real time later, this would need to become a per-frame post-processing effect instead.
/// </summary>
public class ScreenBlur : MonoBehaviour
{
    [Tooltip("Full-screen RawImage this writes the blurred snapshot to. Should sit behind the shop panel in the Canvas.")]
    [SerializeField] private RawImage targetImage;
    [Tooltip("Camera to capture. Defaults to Camera.main if left empty.")]
    [SerializeField] private Camera sourceCamera;
    [Range(0f, 1f)]
    [Tooltip("0 = no blur, 1 = maximum blur. Exposed so it can be tuned live.")]
    [SerializeField] private float blurAmount = 0.5f;
    [Tooltip("Number of downsample iterations used at blurAmount = 1. Higher = smoother/heavier blur, slightly more expensive.")]
    [SerializeField] private int maxIterations = 6;

    private RenderTexture resultTexture;

    /// <summary>Adjusts blur strength. Takes effect on the next Capture() call.</summary>
    public void SetBlurAmount(float amount)
    {
        blurAmount = Mathf.Clamp01(amount);
    }

    /// <summary>Captures the current frame and displays a blurred snapshot of it.</summary>
    public void Capture()
    {
        if (sourceCamera == null) sourceCamera = Camera.main;
        if (sourceCamera == null || targetImage == null) return;

        int iterations = Mathf.CeilToInt(maxIterations * blurAmount);
        if (iterations <= 0)
        {
            targetImage.gameObject.SetActive(false);
            return;
        }

        int width = Mathf.Max(4, sourceCamera.pixelWidth);
        int height = Mathf.Max(4, sourceCamera.pixelHeight);

        RenderTexture fullRes = RenderTexture.GetTemporary(width, height, 0);
        RenderTexture previousTarget = sourceCamera.targetTexture;
        sourceCamera.targetTexture = fullRes;
        sourceCamera.Render();
        sourceCamera.targetTexture = previousTarget;

        RenderTexture current = fullRes;
        for (int i = 0; i < iterations; i++)
        {
            int downW = Mathf.Max(4, current.width / 2);
            int downH = Mathf.Max(4, current.height / 2);

            RenderTexture down = RenderTexture.GetTemporary(downW, downH, 0);
            down.filterMode = FilterMode.Bilinear;
            Graphics.Blit(current, down);

            if (current != fullRes) RenderTexture.ReleaseTemporary(current);
            current = down;
        }

        if (resultTexture != null) RenderTexture.ReleaseTemporary(resultTexture);
        resultTexture = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(current, resultTexture); // upsample back to full size, still soft from the downsampling above

        if (current != fullRes) RenderTexture.ReleaseTemporary(current);
        RenderTexture.ReleaseTemporary(fullRes);

        targetImage.texture = resultTexture;
        targetImage.gameObject.SetActive(true);
    }

    /// <summary>Hides the blurred snapshot and releases its RenderTexture.</summary>
    public void Clear()
    {
        if (targetImage != null) targetImage.gameObject.SetActive(false);

        if (resultTexture != null)
        {
            RenderTexture.ReleaseTemporary(resultTexture);
            resultTexture = null;
        }
    }
}