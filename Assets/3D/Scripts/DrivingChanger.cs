using UnityEngine;
using UnityEngine.Rendering.Universal; 

public class DrivingChanger : MonoBehaviour
{
    [SerializeField] GameData gd;
    [SerializeField] Light2D[] lights;
    [SerializeField] Camera cam;
    [SerializeField] Light2D globalLight;
    [SerializeField] GameObject rain;
    [SerializeField] Color day4col, day5col;
    [SerializeField] GameObject[] policeWave1;
    [SerializeField] GameObject[] missingWave;
    [SerializeField] GameObject[] policeWave2;
    [SerializeField] GameObject fence;
    [SerializeField] GameObject snowBlock1;
    [SerializeField] GameObject snowBlock2;
    [SerializeField] GameObject rockBlock1;
    [SerializeField] GameObject rockBlock2;


    private ParticleSystem ps;
    private int prevDay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        prevDay = 0;
        ps = rain.GetComponent<ParticleSystem>();
        rain.SetActive(false);
        fence.SetActive(true);
        rockBlock1.SetActive(true);
        rockBlock2.SetActive(true);
        snowBlock1.SetActive(true);
        snowBlock2.SetActive(true);
    }

    // Update is called once per frame
    private void Update()
    {
        if(prevDay != gd.getDay())
        {
            if(gd.getDay() == 1)
            {
                SetLights(0f);
                rain.SetActive(false);
                setObjectState(false, policeWave1);
                setObjectState(false, missingWave);
            }

            else if(gd.getDay() == 2)
            {
                rain.SetActive(true);
                setRainAmt(20f);
                setObjectState(true, missingWave);
                fence.SetActive(false);
            }
            else if(gd.getDay() == 3)
            {
                setRainAmt(20f);
                setObjectState(true, policeWave1);
                setGlobalLight(0.75f);
                SetLights(1.5f);
                setBgColour(day4col);
            }
            else if(gd.getDay() == 4)
            {
                setRainAmt(100f);
                SetLights(3);
                setBgColour(day4col);
                setGlobalLight(0.5f);
            }
            else if(gd.getDay() == 5)
            {
                setRainAmt(200f);
                SetLights(4);
                setBgColour(day5col);
                setGlobalLight(0.2f);
                setRainSize(0.2f);
                
            }


            prevDay = gd.getDay();
        }

        handleBlocks();
        
    }

   private void SetLights(float intens)
    {
        foreach (Light2D light in lights)
        {
            light.intensity = intens;
        }
    }

    private void setRainAmt(float amt)
    {
        var emissionModule = ps.emission;
        emissionModule.rateOverTime = amt;
        
    }
    private void setRainSize(float size)
    {
        ps.startSize = size;
    }
    private void setBgColour(Color colour)
    {
        cam.backgroundColor = colour;
    }
    private void setGlobalLight(float intens)
    {
        globalLight.intensity = intens;
    }
    private void setObjectState(bool state, GameObject[] list)
    {
        foreach (GameObject go in list)
        {
            go.SetActive(state);
        }
    }

    private void handleBlocks()
    {
        if(gd.suspension == true)
        {
            rockBlock1.SetActive(false);
            rockBlock2.SetActive(false);
        }
        if(gd.snowTires == true)
        {
            snowBlock1.SetActive(false);
            snowBlock2.SetActive(false);
        }
    }
}
