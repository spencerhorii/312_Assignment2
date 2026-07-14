using UnityEngine;
using UnityEngine.UI;

public class boilOnly : MonoBehaviour
{
    [SerializeField] Sprite frame1, frame2;
    Image img;
    private int sprtNum;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        img = GetComponent<Image>();
        img.sprite = frame1;
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

        if(sprtNum == 0)
        {
            img.sprite = frame1;
        }
        else if(sprtNum == 1)
        {
            img.sprite = frame2;
        }
        
    }
}
