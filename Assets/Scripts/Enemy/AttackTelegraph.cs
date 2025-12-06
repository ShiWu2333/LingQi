using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AttackTelegraph : MonoBehaviour
{
    [Header("Telegraph Prefab")]
    [SerializeField] private GameObject telegraphPrefab;

    [Header("Spawn Point (Optional)")]
    [SerializeField] private Transform spawnPoint;

    [Header("Timing")]
    [SerializeField] private float telegraphDelay = 0.2f;
    [SerializeField] private float autoDestroyTime = 0f;

    private Coroutine _routine;
    private bool _hasTelegraphSpawnedThisAttack = false;

    public void BeginAttackTelegraph()
    {
        if (_hasTelegraphSpawnedThisAttack)
            return;

        _hasTelegraphSpawnedThisAttack = true;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _routine = StartCoroutine(CoSpawnTelegraph());
    }

    public void ResetForNextAttack()
    {
        _hasTelegraphSpawnedThisAttack = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator CoSpawnTelegraph()
    {
        if (telegraphDelay > 0f)
            yield return new WaitForSeconds(telegraphDelay);

        if (telegraphPrefab == null)
            yield break;

        Transform src = spawnPoint != null ? spawnPoint : transform;

        Vector3 pos = src.position;
        Quaternion rot = src.rotation;
        Vector3 scale = src.localScale;

        GameObject vfx = Instantiate(telegraphPrefab, pos, rot);
        vfx.transform.localScale = scale;

        float life = autoDestroyTime;
        if (life <= 0f)
        {
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
                life = ps.main.duration + ps.main.startLifetime.constantMax;
            else
                life = 1f;
        }

        Destroy(vfx, life);
    }
}
