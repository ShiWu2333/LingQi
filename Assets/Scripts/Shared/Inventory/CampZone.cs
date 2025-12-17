using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class CampZone : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var interact = other.GetComponent<PlayerInteractController>();
        if (interact != null)
            interact.SetInCamp(true);
    }

    private void OnTriggerExit(Collider other)
    {
        var interact = other.GetComponent<PlayerInteractController>();
        if (interact != null)
            interact.SetInCamp(false);
    }
}
