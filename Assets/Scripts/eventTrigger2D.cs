using UnityEngine;
using UnityEngine.Events;

public class eventTrigger2D : MonoBehaviour
{
    [SerializeField] string tagFilter;

    [SerializeField] UnityEvent onTriggerEnter;
    [SerializeField] UnityEvent onTriggerExit;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;

        onTriggerEnter.Invoke();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;

        onTriggerExit.Invoke();
    }
}