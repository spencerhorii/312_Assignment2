using UnityEngine;

public class DrivingGameManager : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 720f; // degrees per second, tune for snappiness

    private Transform t;
    private float currRot;
    private Quaternion targetRotation;

    void Start()
    {
        t = GetComponent<Transform>();
        currRot = 0;
        targetRotation = t.rotation;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currRot += 90f;
            if (currRot >= 360)
            {
                currRot = 0;
            }
            targetRotation = Quaternion.Euler(0f, currRot, 0f);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            currRot -= 90f;
            if (currRot < 0)
            {
                currRot = 270;
            }
            targetRotation = Quaternion.Euler(0f, currRot, 0f);
        }

        // Smoothly rotate towards the target each frame
        t.rotation = Quaternion.RotateTowards(t.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}