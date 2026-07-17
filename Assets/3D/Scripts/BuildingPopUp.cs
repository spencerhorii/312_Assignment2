using UnityEngine;

public class BuildingPopUp : MonoBehaviour
{
    [SerializeField] Sprite image1, image2;
    [SerializeField] Sprite selected;
    [SerializeField] string loc;
    [SerializeField] SceneController sc;
    

    [Header("Show/Hide Transition")]
    [Tooltip("How far below its normal position the popup hides to.")]
    [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, -2f, 0f);

    [Tooltip("Higher = snappier transition, lower = slower/smoother.")]
    [SerializeField] private float transitionSpeed = 8f;

    [Header("Camera Zoom Scaling")]
    [Tooltip("The camera's CameraControl script (tracks zoom state).")]
    [SerializeField] private CameraControl cameraControl;

    [Tooltip("How much bigger the popup gets (multiplied by its default scale) when the camera is fully zoomed out (idle).")]
    [SerializeField] private float maxScaleMultiplier = 2f;

    private SpriteRenderer sr;
    private bool clicked;
    private int sprtNum;
    private bool listening;

    private Vector3 shownPosition;
    private Vector3 hiddenPosition;
    private Vector3 shownScale;
    [SerializeField] private AudioClip popUp;
    [SerializeField] private AudioClip enter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = image1;
        clicked = false;
        sprtNum = 0;
        listening = false;

        // Capture this popup's authored local position/scale (relative to its parent building) as its "shown" state.
        shownPosition = transform.localPosition;
        shownScale = transform.localScale;
        hiddenPosition = shownPosition + hiddenOffset;

        // Start hidden.
        transform.localPosition = hiddenPosition;
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
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

        if(sprtNum == 0 && !clicked)
        {
            sr.sprite = image1;
        }
        else if(sprtNum == 1 && !clicked)
        {
            sr.sprite = image2;
        }
        else if (clicked)
        {
            sr.sprite = selected;
        }

        if(Input.GetKey(KeyCode.E))
        {
            clicked = true;
        }
        else
        {
            clicked = false;
        }

        checkEnter();
        updatePopupTransform();
    }

    private void updatePopupTransform()
    {
        float scaleMultiplier = GetDistanceScaleMultiplier();

        Vector3 targetPosition = listening ? GetShownPositionWithRise(scaleMultiplier) : hiddenPosition;
        Vector3 targetScale = listening ? shownScale * scaleMultiplier : Vector3.zero;

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime); // frame-rate independent smoothing
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }

    private Vector3 GetShownPositionWithRise(float scaleMultiplier)
    {
        // Scale the Y offset by the same multiplier as the popup's scale, so it rises
        // proportionally as it grows (keeps it visually anchored above the building
        // instead of appearing to sink in as it scales up from its pivot).
        Vector3 result = shownPosition;
        result.y = shownPosition.y * scaleMultiplier;
        return result;
    }

    private float GetDistanceScaleMultiplier()
    {
        if (cameraControl == null) return 1f;

        // zoomBlend: 0 = zoomed out/idle (popup should be BIG), 1 = zoomed in (popup at default size)
        return Mathf.Lerp(maxScaleMultiplier, 1f, cameraControl.ZoomBlend);
    }

    public void setListening(bool booler)
    {
        listening = booler;
        SoundFXManager.instance.PlaySoundFXClip(popUp, this.transform, 0.8f);

        
    }

    private void checkEnter()
    {
        if (listening)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                SoundFXManager.instance.PlaySoundFXClip(enter, transform, 0.5f);
                sc.ChangeScene(loc);
            }
        }
    }


};