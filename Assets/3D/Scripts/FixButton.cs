using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FixButton : MonoBehaviour, IPointerUpHandler, IPointerEnterHandler
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;

    public void OnPointerUp(PointerEventData eventData)
    {
        // Clears focus from the button immediately after release
        EventSystem.current.SetSelectedGameObject(null);

        PlaySound(clickSound, 0.5f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSound, 0.3f);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null) return;

        if (SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundFXClip(clip, transform, volume);
        }
        else
        {
            Debug.LogWarning("[FixButton] SoundFXManager.instance is null.", this);
        }
    }
}