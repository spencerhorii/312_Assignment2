using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class TaskEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmp; 
    [SerializeField] private Image npcIcon;
    [SerializeField] private Sprite pendImg;
    [SerializeField] private Sprite pipImg;
    [SerializeField] private Sprite borImg;
    [SerializeField] private Sprite arturImg;
    [SerializeField] private Sprite tortokImg;
    [SerializeField] private Sprite samImg;
    [SerializeField] private Sprite kevImg;
    [SerializeField] private Sprite heraldImg;
    [SerializeField] private Sprite isabelleImg;
    [SerializeField] private Sprite heriImg;
    [SerializeField] private Sprite cocoImg;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(TaskItem task)
    {
        tmp.text = task.description;
        handleImg(task);


    }

    private void handleImg(TaskItem task)
    {
        switch (task.npc)
        {
            case TaskItem.NPC.Pendi:
                npcIcon.sprite = pendImg;
                break;
            case TaskItem.NPC.Pip:
                npcIcon.sprite = pipImg;
                break;
            case TaskItem.NPC.Bor:
                npcIcon.sprite = borImg;
                break;
            case TaskItem.NPC.Artur:
                npcIcon.sprite = arturImg;
                break;
            case TaskItem.NPC.Tortok:
                npcIcon.sprite = tortokImg;
                break;
            case TaskItem.NPC.Sam:
                npcIcon.sprite = samImg;
                break;
            case TaskItem.NPC.Kev:
                npcIcon.sprite = kevImg;
                break;
            case TaskItem.NPC.Herald:
                npcIcon.sprite = heraldImg;
                break;
            case TaskItem.NPC.Isabelle:
                npcIcon.sprite = isabelleImg;
                break;
            case TaskItem.NPC.Heri:
                npcIcon.sprite = heriImg;
                break;
            case TaskItem.NPC.Coco:
                npcIcon.sprite = cocoImg;
                break;
            default:
                Debug.LogWarning($"[handleImg] No sprite mapped for NPC: {task.npc}");
                break;
        }
    }
}
