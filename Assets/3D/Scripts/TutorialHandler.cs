using UnityEngine;

public class TutorialHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] GameObject[] objs;
    [SerializeField] SceneController sc;
    private int advancement;
    void Start()
    {
        advancement = 0;
        objs[0].SetActive(true);
        objs[1].SetActive(false);
        objs[2].SetActive(false);
        
    }

    // Update is called once per frame
    void Update()
    {

        
    }

    public void addAdvance()
    {
        advancement++;
        if(advancement == 1)
        {
            objs[0].SetActive(false);
            objs[1].SetActive(true);
        }
        else if(advancement == 2)
        {
            objs[1].SetActive(false);
            objs[2].SetActive(true);
        }
        else if(advancement >= 3)
        {
            advancement = 0;
            sc.ChangeScene("House");
            
        }
    }
}
