using UnityEngine;

/// <summary>
/// Reusable per-character audio configuration for dialogue typing sounds. Create one per
/// character (NPCs each get their own via NPC.voiceData, and DialogueManager has a
/// playerVoiceData for Player-speaker lines). Assign via Assets > Create > Dialogue > Voice Data.
/// </summary>
[CreateAssetMenu(fileName = "NewVoiceData", menuName = "Dialogue/Voice Data")]
public class DialogueVoiceData : ScriptableObject
{
    [Tooltip("Play a typing sound every N non-whitespace characters revealed (1 = every character, " +
             "2 = every other character, 3 = every third, etc). Whitespace is always skipped regardless.")]
    [Min(1)]
    public int frequency = 1;

    [Tooltip("Minimum pitch randomly picked each time a sound plays. Note: negative pitch values " +
             "play the clip in REVERSE in Unity - that's a valid stylistic choice, but worth knowing " +
             "if you didn't intend it. Use a small positive minimum (e.g. 0.9) for a more 'normal' " +
             "pitch-varied voice instead.")]
    public float minPitch = -1f;

    [Tooltip("Maximum pitch randomly picked each time a sound plays.")]
    public float maxPitch = 1f;

    [Tooltip("Pool of short typing blip clips (recommended 0.1-0.2s each) randomly picked from each time a sound plays.")]
    public AudioClip[] clips;
}