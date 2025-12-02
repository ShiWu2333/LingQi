using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private DamagePopup popupPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static void Show(float amount, Vector3 position)
    {
        if (Instance == null || Instance.popupPrefab == null)
            return;

        Instance.Spawn(amount, position);
    }

    private void Spawn(float amount, Vector3 position)
    {
        DamagePopup popup = Instantiate(popupPrefab, position, Quaternion.identity);
        popup.Init(amount);
    }
}
