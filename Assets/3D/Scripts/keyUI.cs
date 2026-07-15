using UnityEngine;
using UnityEngine.UI;

public class keyUI : MonoBehaviour
{
    [SerializeField] Sprite image1, image2;
    [SerializeField] Sprite selected;
    [SerializeField] string inputKey;
    private Image imgComp;
    private bool clicked;
    private int sprtNum;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        imgComp = GetComponent<Image>();
        imgComp.sprite = image1;
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
            imgComp.sprite = image1;
        }
        else if(sprtNum == 1 && !clicked)
        {
            imgComp.sprite = image2;
        }
        else if (clicked)
        {
            imgComp.sprite = selected;
        }

        HandleSwitch();

 
    }

    private void HandleSwitch()
    {
        if(inputKey == "w" || inputKey == "W")
        {
            if (Input.GetKey(KeyCode.W))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }
        else if(inputKey == "d" || inputKey == "D")
        {
            if (Input.GetKey(KeyCode.D))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }
        else if(inputKey == "S" || inputKey == "s")
        {
            if (Input.GetKey(KeyCode.S))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }
        else if(inputKey == "A" || inputKey == "a")
        {
            if (Input.GetKey(KeyCode.A))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }
        else if(inputKey == "Q" || inputKey == "q")
        {
            if (Input.GetKey(KeyCode.Q))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }
        else if(inputKey == "R" || inputKey == "r")
        {
            if (Input.GetKey(KeyCode.R))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }
        else if(inputKey == " " || inputKey == " ")
        {
            if (Input.GetKey(KeyCode.Space))
            {
                clicked = true;
            }
            else
            {
                clicked = false;
            }
        }

    }
};
