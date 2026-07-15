using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class TaskEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmp; 
    [SerializeField] private Image npcIcon;
    [SerializeField] private Sprite pendImg;
    [SerializeField] private Sprite pipImg;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(TaskItem task)
    {
        tmp.text = task.description;
        handleImg(task);


    }

    private void handleImg(TaskItem task)
    {
        if(task.npc == TaskItem.NPC.Pendi)
        {
            npcIcon.sprite = pendImg;
        }
        else if(task.npc == TaskItem.NPC.Pip)
        {
            npcIcon.sprite = pipImg;
        }
        
    }
}
