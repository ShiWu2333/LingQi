using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyLootDropper : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private LootContainer lootContainerPrefab;   // 拖 PF_CorpseLootContainer
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Loot Table (Simple)")]
    [Tooltip("固定掉落（测试阶段先用这个，后面再换成概率表）")]
    [SerializeField] private LootTableConfig lootTable;
    [SerializeField] private int overrideRolls = -1; // <0 表示用 table.rolls

    [Header("Spawn")]
    [SerializeField] private bool destroyContainerWhenEmpty = false;

    private EnemyResources _enemy;
    private bool _dropped;

    private void Awake()
    {
        _enemy = GetComponent<EnemyResources>();
    }

    private void OnEnable()
    {
        if (_enemy != null)
            _enemy.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_enemy != null)
            _enemy.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        DropLoot();
    }

    public void DropLoot()
    {
        if (_dropped) return;
        _dropped = true;

        if (lootContainerPrefab == null)
        {
            Debug.LogWarning("[EnemyLootDropper] lootContainerPrefab is null.");
            return;
        }

        var container = Instantiate(
            lootContainerPrefab,
            transform.position + spawnOffset,
            Quaternion.identity
        );

        // ★ 关键 1：登记为运行时生成物，ResetRun 会统一 Destroy
        RunManager.Instance?.RegisterRuntimeObject(container.gameObject);

        // ★ 关键 2：尸体箱子不参与“回到预设状态”，只需要在 Reset 时被销毁
        container.SetParticipateInRunReset(false);

        // 可选：空了就销毁
        container.SetDestroyWhenEmpty(destroyContainerWhenEmpty);

        // 生成 loot
        int? rolls = overrideRolls >= 0 ? overrideRolls : (int?)null;
        var loot = LootTableSampler.Roll(lootTable, rolls);

        container.SetLoot(loot);
    }

    [ContextMenu("DEBUG Drop Loot")]
    private void DebugDrop()
    {
        DropLoot();
    }
}
