using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawnPoint : MonoBehaviour, IRunResettable
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool spawnOnStart = true;

    [SerializeField] private GameObject currentEnemy;

    private Vector3 _initialPos;
    private Quaternion _initialRot;

    private void Awake()
    {
        _initialPos = transform.position;
        _initialRot = transform.rotation;
    }

    private void Start()
    {
        // 用 Start 确保 RunManager.Instance 已经 Awake 完毕
        RunManager.Instance?.RegisterResettable(this);

        if (spawnOnStart)
            SpawnFresh();
    }

    private void OnDestroy()
    {
        RunManager.Instance?.UnregisterResettable(this);
    }

    public void ResetToDefault()
    {
        Debug.Log($"[EnemySpawnPoint] ResetToDefault called: {name}", this);

        transform.SetPositionAndRotation(_initialPos, _initialRot);

        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
            currentEnemy = null;
        }

        SpawnFresh();
    }

    private void SpawnFresh()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawnPoint] enemyPrefab is NULL.", this);
            return;
        }

        currentEnemy = Instantiate(enemyPrefab, _initialPos, _initialRot);
    }
}
