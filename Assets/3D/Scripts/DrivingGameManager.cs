using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrivingGameManager : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 540f; // degrees per second, tune for snappiness
    [SerializeField] private List<GameObject> flatAssets = new List<GameObject>();
    [SerializeField] private AudioClip left, right;

    private Transform t;
    private float currRot;
    private Quaternion targetRotation;

    void Start()
    {
        t = GetComponent<Transform>();
        currRot = 45;
        targetRotation = t.rotation;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currRot += 90f;
            if (currRot >= 405)
            {
                currRot = 45;
            }
            targetRotation = Quaternion.Euler(0f, currRot, 0f);
            foreach (GameObject flat in flatAssets)
            {
                flat.GetComponent<FlatAsset>().rotateRight();
            }
            SoundFXManager.instance.PlaySoundFXClip(right, this.transform, 1f);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currRot -= 90f;
            if (currRot < 45)
            {
                currRot = 315;
            }
            targetRotation = Quaternion.Euler(0f, currRot, 0f);
            foreach (GameObject flat in flatAssets)
            {
                flat.GetComponent<FlatAsset>().rotateLeft();
            }
            SoundFXManager.instance.PlaySoundFXClip(left, this.transform, 1f);
        }

        // Smoothly rotate towards the target each frame
        t.rotation = Quaternion.RotateTowards(t.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        {
            
        }

        
    }
}