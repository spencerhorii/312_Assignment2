using System.Collections;
using UnityEngine;

/// <summary>
/// Central authority for all audio playback. Persistent singleton, same pattern as every other
/// manager in this project. Owns dedicated AudioSources per channel so different sound types
/// never interrupt each other (a UI click and a dialogue blip can play simultaneously, etc).
///
/// Music uses two alternating sources (A/B) so PlayMusic() can crossfade smoothly from whatever
/// is currently playing into a new track, rather than an abrupt cut.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [Tooltip("Plays one-shot UI sounds: tooltips, inventory open/close, shop open/close, navigation, confirm, purchase/sale, currency change.")]
    [SerializeField] private AudioSource uiSource;
    [Tooltip("Plays the per-character dialogue typing blips. Pitch is set before each PlayOneShot call.")]
    [SerializeField] private AudioSource dialogueSource;
    [Tooltip("First of two alternating music sources, used for crossfading.")]
    [SerializeField] private AudioSource musicSourceA;
    [Tooltip("Second of two alternating music sources, used for crossfading.")]
    [SerializeField] private AudioSource musicSourceB;
    [Tooltip("Plays one-shot ambient/environmental sounds (wind, city noise, etc).")]
    [SerializeField] private AudioSource ambientSource;

    [Header("Music")]
    [Tooltip("The volume music sources fade up to / down from.")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }

    // ---------- One-shot channels ----------

    /// <summary>Plays a one-shot UI sound (tooltips, inventory, shop, navigation, confirm, purchase/sale, currency).</summary>
    public void PlayUISound(AudioClip clip)
    {
        if (clip == null || uiSource == null) return;
        uiSource.PlayOneShot(clip);
    }

    /// <summary>Plays a single dialogue typing blip at the given pitch. Called by DialogueManager per its frequency setting.</summary>
    public void PlayDialogueTypingSound(AudioClip clip, float pitch)
    {
        if (clip == null || dialogueSource == null) return;
        dialogueSource.pitch = pitch;
        dialogueSource.PlayOneShot(clip);
    }

    /// <summary>Plays a one-shot ambient sound (wind, city noise, etc). Called by AmbientAudioPlayer.</summary>
    public void PlayAmbientSound(AudioClip clip)
    {
        if (clip == null || ambientSource == null) return;
        ambientSource.PlayOneShot(clip);
    }

    // ---------- Music ----------

    /// <summary>Crossfades from whatever is currently playing (if anything) into a new looping/one-shot track.</summary>
    public void PlayMusic(AudioClip clip, bool loop, float fadeDuration)
    {
        if (clip == null) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(CrossfadeMusic(clip, loop, fadeDuration));
    }

    /// <summary>Fades the currently playing track out (does not stop/clear it - FadeInMusic can resume it).</summary>
    public void FadeOutMusic(float duration)
    {
        if (activeMusicSource == null) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeSourceVolume(activeMusicSource, activeMusicSource.volume, 0f, duration));
    }

    /// <summary>Fades the current track back in (resumes it if it had been paused/faded out, per your "resume" dialogue action).</summary>
    public void FadeInMusic(float duration)
    {
        if (activeMusicSource == null) return;

        if (!activeMusicSource.isPlaying && activeMusicSource.clip != null)
        {
            activeMusicSource.Play();
        }

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeSourceVolume(activeMusicSource, activeMusicSource.volume, musicVolume, duration));
    }

    private IEnumerator CrossfadeMusic(AudioClip clip, bool loop, float duration)
    {
        inactiveMusicSource.clip = clip;
        inactiveMusicSource.loop = loop;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.Play();

        float startVolA = activeMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            activeMusicSource.volume = Mathf.Lerp(startVolA, 0f, t);
            inactiveMusicSource.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        activeMusicSource.volume = 0f;
        activeMusicSource.Stop();
        inactiveMusicSource.volume = musicVolume;

        // Swap roles - whichever source just finished fading in becomes "active" for next time.
        (activeMusicSource, inactiveMusicSource) = (inactiveMusicSource, activeMusicSource);
    }

    private IEnumerator FadeSourceVolume(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, duration > 0f ? elapsed / duration : 1f);
            yield return null;
        }
        source.volume = to;
    }
}
