using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelUpStation : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private LevelUpUI levelUpUI;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private PlayerResources _player;
    private bool _playerInside;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (other.TryGetComponent(out PlayerResources res))
        {
            _player = res;
            _playerInside = true;

            if (debugLog) Debug.Log("[LevelUpStation] Player entered.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (_player != null && other.gameObject == _player.gameObject)
        {
            _playerInside = false;
            _player = null;

            if (debugLog) Debug.Log("[LevelUpStation] Player exited.");
        }
    }

    private void Update()
    {
        if (!_playerInside || _player == null)
            return;

        if (Input.GetKeyDown(interactKey) && levelUpUI != null)
        {
            levelUpUI.Open(_player);
        }
    }
}
