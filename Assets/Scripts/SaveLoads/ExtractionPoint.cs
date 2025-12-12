using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class ExtractionPoint : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

    [Header("Refs")]
    [SerializeField] private Transform campSpawnPointOverride; // 可选：不填就用 RunManager 的

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private bool playerInZone;
    [SerializeField] private bool awaitingConfirm;

    private GameObject _player;

    private void Reset()
    {
        // 确保是 Trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInZone || _player == null)
            return;

        // Esc 取消确认
        if (awaitingConfirm && Input.GetKeyDown(cancelKey))
        {
            awaitingConfirm = false;
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            if (!awaitingConfirm)
            {
                // 第一次按：进入确认态
                awaitingConfirm = true;
                return;
            }

            // 第二次按：确认撤离
            DoExtract();
        }
    }

    private void DoExtract()
    {
        awaitingConfirm = false;

        var rm = RunManager.Instance;
        if (rm == null)
        {
            Debug.LogError("[ExtractionPoint] RunManager.Instance is null!");
            return;
        }

        // 1) 重置本次 Run（敌人回初始、尸体箱子清掉、等等）
        rm.ResetRun();

        // 2) 传送玩家到营地
        Transform camp = campSpawnPointOverride != null ? campSpawnPointOverride : rm.CampSpawnPoint;
        if (camp == null)
        {
            Debug.LogError("[ExtractionPoint] CampSpawnPoint is not assigned!");
            return;
        }

        TeleportPlayerTo(_player, camp.position, camp.rotation);
        playerInZone = false;
    }

    private void TeleportPlayerTo(GameObject player, Vector3 pos, Quaternion rot)
    {
        // 兼容 CharacterController（直接改 transform 会穿模/抖）
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.SetPositionAndRotation(pos, rot);

        if (cc != null) cc.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _player = other.gameObject;
        playerInZone = true;
        awaitingConfirm = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (_player == null) return;
        if (other.gameObject != _player) return;

        playerInZone = false;
        awaitingConfirm = false;
        _player = null;
    }

    // 给你之后接 UI 用（比如弹出确认面板）
    public bool IsPlayerInZone => playerInZone;
    public bool AwaitingConfirm => awaitingConfirm;
}
