using UnityEngine;
using UnityEngine.UI;
using TMPro; // 1. Required namespace for TextMesh Pro

public class DayCounter : MonoBehaviour
{
    [SerializeField] private GameData gameData;
    private TextMeshProUGUI tmp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        
    }

    // Update is called once per frame
    void Update()
    {
        tmp.text = "DAY " + gameData.getDay().ToString();
        
    }
}
