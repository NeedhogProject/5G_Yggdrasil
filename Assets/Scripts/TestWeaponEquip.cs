using UnityEngine;

public class TestWeaponEquip : MonoBehaviour
{
    [SerializeField]private WeaponData weaponData;
    private PlayerEquipment _equipment;

    private void Start()
    {
        _equipment = GetComponent<PlayerEquipment>();
        if( weaponData != null )
            _equipment.EquipItem(new WeaponInstance(weaponData));
    }
}
