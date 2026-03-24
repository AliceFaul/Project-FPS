using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponsSO")]
public class WeaponSO : ScriptableObject
{
    [Header("ID")]
    public int ID = 0;

    [Header("Prefabs")]
    public GameObject weaponPrefab;
    public GameObject HitVFXPrefab;

    [Header("Animation reference")]
    public string shootString;

    [Header("Zoom settings")]
    public float ZoomRotationSpeed = .3f;
    public float ZoomAmount = 10f;

    [Header("Properties")]
    public float Damage = 1f;
    public float FireRate = .5f;
    public float accuracyMod = 1f;
    public int MagazineSize = 12;
    public int ammoOnPickup = 5;

    public bool IsAutomatic = false;
    public bool CanZoom = false;
    public int CrosshairUI_ID = -1;
    public bool HitScan = true;
    public bool PickedUp = false;
}