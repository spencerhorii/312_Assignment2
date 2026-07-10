using UnityEngine;

public class DrivingGameManager : MonoBehaviour
{
    private Transform t;
    private float currRot, pastRot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        t = GetComponent<Transform>();
        currRot = 0;
        pastRot = currRot;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currRot += 90f;
            if(currRot >= 360)
            {
                currRot = 0;
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            currRot -=90f;
            if(currRot <= -90)
            {
                currRot = 270;
            }
        }

        if(currRot != pastRot)
        {
            t.rotation = Quaternion.Euler(0f, currRot, 0f);
            pastRot = currRot;
        }
    }
}
