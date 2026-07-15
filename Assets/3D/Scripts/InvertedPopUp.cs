using UnityEngine;

public class InvertedPopUp : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] Sprite image1, image2, selected;
    private SpriteRenderer sr;
    private bool clicked;
    private int sprtNum;
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = image1;
        clicked = false;
        sprtNum = 0;
        
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
        
    }
}
