using UnityEngine;

public class BodegaUpdater : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameData gd;
    [SerializeField] private GameObject test;
    [SerializeField] private GameObject testOg;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(gd.getDay() == 1)
        {
            test.SetActive(false);
            testOg.SetActive(true);
            
        }
        else if(gd.getDay() == 2)
        {
            test.SetActive(true);
            testOg.SetActive(false);
        }
        
    }
}
