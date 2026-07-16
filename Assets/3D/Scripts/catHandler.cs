using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TMP
using System.Collections;

public class catHandler : MonoBehaviour
{
    [SerializeField] GameData gd;
    [SerializeField] Sprite[] mains;
    [SerializeField] Sprite[] boils;
    [SerializeField] TextMeshProUGUI tmp;
    private Image img;
    private int sprtNum;
    private int day;

    [Header("Intro Animation (on load)")]
    [Tooltip("How long the scale/rotate-in animation takes, in seconds.")]
    [SerializeField] private float introDuration = 0.6f;

    [Tooltip("Scale this object starts at before animating in.")]
    [SerializeField] private Vector3 introStartScale = Vector3.zero;

    [Tooltip("Rotation (Euler angles, degrees) this object starts at before animating in - e.g. (0,0,180) for a half spin.")]
    [SerializeField] private Vector3 introStartRotationOffset = new Vector3(0f, 0f, 180f);

    [Tooltip("Easing curve for the intro animation.")]
    [SerializeField] private AnimationCurve introEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 normalScale;
    private Quaternion normalRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        img = GetComponent<Image>();
        sprtNum = 0;
        day = 1;

        PlayIntroAnimation();
    }

    private void PlayIntroAnimation()
    {
        // Capture whatever scale/rotation was authored in the Inspector/scene as "normal".
        normalScale = transform.localScale;
        normalRotation = transform.localRotation;

        // Snap into the starting (scaled down/rotated) state, then ease into normal.
        transform.localScale = introStartScale;
        transform.localRotation = normalRotation * Quaternion.Euler(introStartRotationOffset);

        StartCoroutine(IntroAnimationRoutine());
    }

    private IEnumerator IntroAnimationRoutine()
    {
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.localRotation;

        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / introDuration);
            float easedT = Mathf.Clamp01(introEaseCurve.Evaluate(t));

            transform.localScale = Vector3.Lerp(startScale, normalScale, easedT);
            transform.localRotation = Quaternion.Slerp(startRotation, normalRotation, easedT);

            yield return null;
        }

        transform.localScale = normalScale;
        transform.localRotation = normalRotation;
    }

    // Update is called once per frame
    void Update()
    {
        day = gd.getDay() - 1;

        if (Time.frameCount % 240 == 0) {
            if(sprtNum == 0)
            {
                sprtNum = 1;
            }
            else if(sprtNum == 1)
            {
                sprtNum = 0;
            }
        }

        if(sprtNum == 0)
        {
            img.sprite = mains[(day-1)];
        }
        else if(sprtNum == 1)
        {
            img.sprite = boils[(day-1)];
        }

        handleText();
    }

    private void handleText()
    {
        if(day == 1)
        {
            tmp.text = "<style=H2>AWESOME JOB</style>" + System.Environment.NewLine + "Day 1 Complete";
        }
        else if(day == 2)
        {
            tmp.text = "<style=H2>GREAT WORK</style>" + System.Environment.NewLine + "Day 2 Complete";
        }
        else if(day == 3)
        {
            tmp.text = "<style=H2>I'M A BIT WORRIED</style>" + System.Environment.NewLine + "Day 3 Complete";
        }
        else if(day == 4)
        {
            tmp.text = "<style=H2>THEY'RE ON TO ME!</style>" + System.Environment.NewLine + "Day 4 Complete";
        }
        else if(day == 5)
        {
            tmp.text = "<style=H2>HELP! ME!</style>" + System.Environment.NewLine + "Day 5 Complete";
        }
        
    }
}