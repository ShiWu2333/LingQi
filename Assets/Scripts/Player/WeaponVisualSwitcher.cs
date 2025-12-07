using UnityEngine;

public class WeaponVisualSwitcher : MonoBehaviour
{
    [Header("Weapons (only one active at a time)")]
    public GameObject weapon1;
    public GameObject weapon2;
    public GameObject weapon3;

    private int currentIndex = 0;

    private void Start()
    {
        // 默认显示第一把
        SetWeapon(1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetWeapon(1);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetWeapon(2);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetWeapon(3);
    }

    public void SetWeapon(int index)
    {
        currentIndex = index;

        if (weapon1 != null) weapon1.SetActive(index == 1);
        if (weapon2 != null) weapon2.SetActive(index == 2);
        if (weapon3 != null) weapon3.SetActive(index == 3);

        //（可选：之后你可以在这同步切换 PlayerCombatController 的 weaponConfig）
    }
}
