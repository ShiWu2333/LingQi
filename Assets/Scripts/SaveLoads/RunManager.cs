using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRunResettable
{
    void ResetToDefault();
}

[DisallowMultipleComponent]
public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Camp")]
    [SerializeField] private Transform campSpawnPoint;

    [Header("Player (Auto)")]
    [SerializeField] private bool autoFindPlayer = true;

    [Tooltip("调试用：实际绑定到的 PlayerResources")]
    [SerializeField] private PlayerResources playerResources;

    [Tooltip("调试用：实际使用的 Player Root")]
    [SerializeField] private Transform playerRoot;

    [SerializeField] private bool resetOnPlayerDeath = true;

    private readonly List<IRunResettable> _resettables = new();
    private readonly List<GameObject> _runtimeSpawned = new();

    public Transform CampSpawnPoint => campSpawnPoint;

    // ================= 生命周期 =================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (autoFindPlayer)
            AutoBindPlayer();
    }

    private void OnEnable()
    {
        TrySubscribePlayerDeath();
    }

    private void OnDisable()
    {
        if (playerResources != null)
            playerResources.OnDeath -= HandlePlayerDeath;
    }

    // ================= Player 自动绑定 =================

    private void AutoBindPlayer()
    {
        // 1️⃣ 优先通过 Tag 找
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");

        if (playerGO != null)
        {
            playerResources = playerGO.GetComponentInChildren<PlayerResources>();
            playerRoot = playerGO.transform;
        }

        // 2️⃣ fallback：全局找
        if (playerResources == null)
            playerResources = FindObjectOfType<PlayerResources>();

        // 3️⃣ 推断 root
        if (playerRoot == null && playerResources != null)
            playerRoot = playerResources.transform.root;

        if (playerResources == null)
        {
            Debug.LogWarning("[RunManager] PlayerResources not found. Player death reset disabled.");
            return;
        }

        TrySubscribePlayerDeath();
    }

    private void TrySubscribePlayerDeath()
    {
        if (playerResources == null)
            return;

        // 防止重复订阅
        playerResources.OnDeath -= HandlePlayerDeath;
        playerResources.OnDeath += HandlePlayerDeath;
    }

    // ================= 玩家死亡 =================

    private void HandlePlayerDeath()
    {
        if (!resetOnPlayerDeath)
            return;

        Debug.Log("[RunManager] Player died → ResetRun");

        ResetRun();
        TeleportPlayerToCamp();
        // ★ 最后做资源初始化（顺序很关键）
        playerResources.ResetToFull();
    }

    private void TeleportPlayerToCamp()
    {
        if (playerRoot == null || campSpawnPoint == null)
            return;

        playerRoot.position = campSpawnPoint.position;
        playerRoot.rotation = campSpawnPoint.rotation;
    }

    // ================= Reset 管线 =================

    public void RegisterResettable(IRunResettable r)
    {
        if (r == null || _resettables.Contains(r))
            return;

        _resettables.Add(r);
    }

    public void UnregisterResettable(IRunResettable r)
    {
        if (r == null) return;
        _resettables.Remove(r);
    }

    public void RegisterRuntimeObject(GameObject go)
    {
        if (go == null || _runtimeSpawned.Contains(go))
            return;

        _runtimeSpawned.Add(go);
    }

    public void ResetRun()
    {
        // 1️⃣ 清运行时生成物（尸体箱子等）
        for (int i = _runtimeSpawned.Count - 1; i >= 0; i--)
        {
            var go = _runtimeSpawned[i];
            if (go != null)
                Destroy(go);
        }
        _runtimeSpawned.Clear();

        // 2️⃣ 重置刷点 / 场景对象
        for (int i = 0; i < _resettables.Count; i++)
        {
            if (_resettables[i] == null) continue;
            _resettables[i].ResetToDefault();
        }

        // ★ 最后做资源初始化（顺序很关键）
        playerResources.ResetToFull();
    }
}
