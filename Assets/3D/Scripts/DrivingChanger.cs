using UnityEngine;
using UnityEngine.Rendering.Universal; 

public class DrivingChanger : MonoBehaviour
{
    [SerializeField] GameData gd;
    [SerializeField] Light2D[] lights;
    [SerializeField] Camera cam;
    private int prevDay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        prevDay = 0;
        
    }

    // Update is called once per frame
    private void Update()
    {
        if(prevDay != gd.getDay())
        {
            if(gd.getDay() == 1)
            {
                SetLights(0f);
            }

            prevDay = gd.getDay();
        }
        
    }

   private void SetLights(float intens)
    {
        foreach (Light2D light in lights)
        {
            light.intensity = intens;
        }
    }
}
