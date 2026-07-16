using UnityEngine;
using TMPro;

/// <summary>
/// Controls a single response option row's text and opacity. One of these lives on each
/// instantiated option prefab under DialogueUI's response options container. Same pattern
/// as InventorySlotUI.
/// </summary>
public class DialogueResponseOptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI optionText;

    public RectTransform RectTransform => (RectTransform)transform;

    public void SetText(string value)
    {
        if (optionText != null) optionText.text = value;
    }

    public void SetOpacity(float alpha)
    {
        if (optionText == null) return;

        Color c = optionText.color;
        optionText.color = new Color(0.54f, 0.06f, 1.56f, 1.0f);
        // c.a = alpha;
        optionText.color = c;
    }
}