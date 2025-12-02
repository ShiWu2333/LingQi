using LingQi.CameraSystem;
using LingQi.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingQi.Combat
{
    [DisallowMultipleComponent]
    public class CombatTestBootstrap : MonoBehaviour
    {
        [Header("Prefab Paths (Resources/Prefabs)")]
        [SerializeField] private string playerPrefabPath = "Prefabs/Player";
        [SerializeField] private string lightWeaponPath = "Prefabs/WeaponLight";
        [SerializeField] private string heavyWeaponPath = "Prefabs/WeaponHeavy";
        [SerializeField] private string dummyPath = "Prefabs/Dummy";

        private void Start()
        {
            var playerPrefab = LoadPrefab(playerPrefabPath);
            var lightWeaponPrefab = LoadPrefab(lightWeaponPath);
            var heavyWeaponPrefab = LoadPrefab(heavyWeaponPath);
            var dummyPrefab = LoadPrefab(dummyPath);

            GameObject player = Instantiate(playerPrefab, new Vector3(0f, 1f, -4f), Quaternion.identity);
            Weapon lightWeapon = Instantiate(lightWeaponPrefab, player.transform).GetComponent<Weapon>();
            Weapon heavyWeapon = Instantiate(heavyWeaponPrefab, player.transform).GetComponent<Weapon>();

            var combat = player.GetComponent<PlayerCombat>();
            combat.ConfigureWeapons(lightWeapon, heavyWeapon);

            Instantiate(dummyPrefab, new Vector3(0f, 0f, 5f), Quaternion.identity);
            SpawnGround();
            SetupCamera(player.transform);
        }

        private GameObject LoadPrefab(string path)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                throw new MissingReferenceException($"Missing prefab at Resources/{path}");
            }

            return prefab;
        }

        private void SpawnGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
        }

        private void SetupCamera(Transform target)
        {
            Camera existing = Camera.main;
            CameraFollow follow;

            if (existing == null)
            {
                GameObject cameraObject = new GameObject("MainCamera");
                existing = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }

            follow = existing.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = existing.gameObject.AddComponent<CameraFollow>();
            }

            follow.target = target;
            follow.offset = new Vector3(0f, 3f, -6f);
            follow.lookAtTarget = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "CombatTestScene")
            {
                return;
            }

            if (FindObjectOfType<CombatTestBootstrap>() != null)
            {
                return;
            }

            var bootstrapObject = new GameObject("CombatTestBootstrap");
            bootstrapObject.AddComponent<CombatTestBootstrap>();
        }
    }
}
