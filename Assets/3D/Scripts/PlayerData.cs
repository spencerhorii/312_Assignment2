using UnityEngine;

public class PlayerData : MonoBehaviour
{
    private static int energyPoints;
    private static int money;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        energyPoints = 5;
        money = 0;
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static int getEnergy()
    {
        return energyPoints;
    }
    public static int getMoney()
    {
        return money;
    }

    public static void addMoney(int amt)
    {
        money += amt;
    }
    public static void addEnergy(int amt)
    {
        energyPoints += amt;
    }
}
