using UnityEngine;

public class FlatAsset : MonoBehaviour
{
    [SerializeField] private Sprite frame1, frame2;
    private SpriteRenderer sr;
    private Transform t;
    private int sprtNum;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        t = GetComponent<Transform>();
        sr = GetComponent<SpriteRenderer>();
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
            sr.sprite = frame1;
        }
        else if(sprtNum == 1)
        {
            sr.sprite = frame2;
        }

        
    }

    public void rotateRight()
    {
        // t.rotation = Quaternion.RotateTowards(t.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        t.Rotate(0, 90f, 0);
        if(t.rotation.y >= 405)
        {
            t.rotation = Quaternion.Euler(0f, 45f, 0f);
        }
    }
    public void rotateLeft()
    {
        // t.rotation = Quaternion.RotateTowards(t.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        t.Rotate(0, -90f, 0);
        if(t.rotation.y <= -45)
        {
            t.rotation = Quaternion.Euler(0f, 315f, 0f);
        }
    }
}
