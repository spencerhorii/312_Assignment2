using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] GameData gd;
    [SerializeField] AudioClip[] clips;
    [SerializeField] private CameraControl cameraControl;

    [Header("Zoom Ducking")]
    [Tooltip("Music volume when the camera is fully zoomed out (idle).")]
    [SerializeField] private float defaultVolume = 0.7f;

    [Tooltip("Music volume when the camera is fully zoomed in on the target.")]
    [SerializeField] private float zoomedVolume = 0.25f;

    [Tooltip("How quickly the volume eases toward its target when the zoom state changes.")]
    [SerializeField] private float volumeLerpSpeed = 3f;

    private AudioSource audioSource;
    private float vol;
    private int clipAmt, currDay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        clipAmt = clips.Length + 1;
        currDay = gd.getDay();
        vol = defaultVolume;
        audioSource.clip = clips[gd.getDay() - 1];
        audioSource.Play();
        audioSource.loop = true;
    }

    // Update is called once per frame
    void Update()
    {

        float targetVolume = defaultVolume;
        if (cameraControl != null)
        {
            targetVolume = Mathf.Lerp(defaultVolume, zoomedVolume, cameraControl.ZoomBlend);
        }

        vol = Mathf.MoveTowards(vol, targetVolume, volumeLerpSpeed * Time.deltaTime);
        audioSource.volume = vol;
    }
}