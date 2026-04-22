using StarterAssets;
using TMPro;
using UnityEngine;
using Unity.Cinemachine;

public class ActiveWeapon : MonoBehaviour
{
    [Header("Weapon config")]
    [SerializeField] AudioClip magazineEmptyClip;
    [SerializeField] WeaponSO startingWeaponSO;
    [SerializeField] WeaponSO currentWeaponSO;
    [SerializeField] WeaponSO[] weaponList;
    [SerializeField] int[] currentAmmoList = { 12, 32, 5, 3 };

    [Header("Camera")]
    [SerializeField] Camera weaponCam;
    [SerializeField] CinemachineVirtualCamera playerFollowCam;

    [Header("UI")]
    [SerializeField] GameObject crosshair;
    [SerializeField] GameObject[] zoomUI;
    [SerializeField] GameObject[] weaponIcons;
    [SerializeField] TMP_Text ammoText;

    Animator animator;
    FirstPersonController firstPersonController;
    StarterAssetsInputs starterAssetsInputs;
    PlayerNetworkSetup playerNetworkSetup;
    Vector3 weaponIconSizeMod = new(1.3f, 1.3f, 1.3f);
    Weapon currentWeapon;

    public WeaponSO[] WeaponList => weaponList; 

    int[] checkpointAmmoList = null;
    int[] maxAmmoList = { 20, 56, 12, 8 };
    bool[] weaponsPickedUp = {false, false, false, false};
    bool[] checkpointWeaponsPickedUp = null;

    float defaultFOV;
    float defaultZoomRotationSpeed;
    float coolDown = 0f;
    float spreadControl = 1f;
    int currentAmmo = 0;
    int currentWeaponID = 0;

    public int CurrentAmmo => currentAmmo;
    public int[] CurrentAmmoList => currentAmmoList;
    public int[] MaxAmmoList => maxAmmoList;

    private bool isInitialized = false;

    void Awake()
    {
        starterAssetsInputs = GetComponentInParent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        firstPersonController = GetComponentInParent<FirstPersonController>();
        playerNetworkSetup = GetComponentInParent<PlayerNetworkSetup>();
    }

    public void Initialize(
        Camera weaponCam, 
        CinemachineVirtualCamera playerFollowCam, 
        GameObject crosshair, 
        GameObject[] zoomUI, 
        GameObject[] weaponIcons, 
        TMP_Text ammoText)
    {
        this.weaponCam = weaponCam;
        this.playerFollowCam = playerFollowCam;
        this.crosshair = crosshair;
        this.zoomUI = zoomUI;
        this.weaponIcons = weaponIcons;
        this.ammoText = ammoText;

        defaultFOV = playerFollowCam.m_Lens.FieldOfView;
        defaultZoomRotationSpeed = firstPersonController.RotationSpeed;

        for (int i = 0; i < weaponList.Length; i++)
        {
            weaponList[i].PickedUp = weaponsPickedUp[i];
            weaponList[i].MagazineSize = maxAmmoList[i];
        }

        foreach(GameObject icon in weaponIcons)
        {
            icon.SetActive(false);
        }

        for (int i = 0; i < weaponList.Length; i++) {
            if (weaponList[i].PickedUp) weaponIcons[i].SetActive(true);
        }

        if(checkpointAmmoList != null) currentAmmoList = checkpointAmmoList;
        if(checkpointWeaponsPickedUp != null) weaponsPickedUp = checkpointWeaponsPickedUp;

        SwitchWeapon(startingWeaponSO);
        isInitialized = true;
    }

    void Update()
    {
        if(!isInitialized) return;
        HandleShoot();
        HandleZoom();
    }

    public void AdjustAmmo(int amount)
    {
        currentAmmo += amount;
        currentAmmo = (int)Mathf.Clamp(currentAmmo, 0, maxAmmoList[currentWeaponID]);

        currentAmmoList[currentWeaponID] += amount;
        currentAmmoList[currentWeaponID] = (int)Mathf.Clamp(currentAmmo, 0, maxAmmoList[currentWeaponID]);

        ammoText.text = currentAmmo.ToString("D2");
    }

    public void AdjustAmmo(WeaponSO weaponSO, float ammoPortion)
    {
        currentAmmoList[weaponSO.ID] += (int)Mathf.Ceil(maxAmmoList[weaponSO.ID] * ammoPortion);
        currentAmmoList[weaponSO.ID] = (int)Mathf.Clamp(currentAmmoList[weaponSO.ID], 0, maxAmmoList[weaponSO.ID]);

        if (weaponSO.ID == currentWeaponID)
        {
            currentAmmo += (int)Mathf.Ceil(maxAmmoList[currentWeaponID] * ammoPortion);
            currentAmmo = (int)Mathf.Clamp(currentAmmo, 0, maxAmmoList[currentWeaponID]);
            ammoText.text = currentAmmo.ToString("D2");
        }
    }

    public void HandleShoot()
    {
        coolDown += Time.deltaTime;
        if (!starterAssetsInputs.shoot) return;

        if (coolDown >= currentWeaponSO.FireRate && currentAmmo > 0)
        {
            coolDown = 0f;
            currentWeapon.Shoot(currentWeaponSO, playerNetworkSetup, spreadControl);
            animator.Play(currentWeaponSO.shootString, 0, 0f);
            AdjustAmmo(-1);
        }

        else if (currentAmmo == 0)
        {
            SoundFXManager.instance.PlaySoundFX(magazineEmptyClip,transform);
            starterAssetsInputs.ShootInput(false);
        }

        if (!currentWeaponSO.IsAutomatic)
        {
            starterAssetsInputs.ShootInput(false);
        }
    }

    public void Switch2Weapon(int newWeaponID)
    {
        if (weaponList[newWeaponID].PickedUp)
        {
            SwitchWeapon(weaponList[newWeaponID]);
        }
    }

    public void SwitchWeapon(WeaponSO weaponSO)
    {
        if (currentWeapon)
        {
            currentAmmoList[currentWeaponID] = currentAmmo;
            Destroy(currentWeapon.gameObject);
        }

        if (!weaponSO.PickedUp)
        {
            weaponSO.PickedUp = true;
            weaponIcons[weaponSO.ID].SetActive(true);
            weaponList[weaponSO.ID].PickedUp = true;
        }

        currentAmmo = currentAmmoList[weaponSO.ID];
        ammoText.text = currentAmmo.ToString("D2");
        Weapon newWeapon = Instantiate(weaponSO.weaponPrefab, transform).GetComponent<Weapon>();
        currentWeapon = newWeapon;
        currentWeaponID = weaponSO.ID;
        this.currentWeaponSO = weaponSO;
        foreach (GameObject go in weaponIcons)
        {
            go.transform.localScale = new(1, 1, 1);
        }
        weaponIcons[currentWeaponID].transform.localScale = weaponIconSizeMod;

        playerFollowCam.m_Lens.FieldOfView = defaultFOV;
        weaponCam.fieldOfView = defaultFOV;
        firstPersonController.ChangeRotationSpeed(defaultZoomRotationSpeed);
        crosshair.SetActive(true);
        foreach (GameObject UI in zoomUI)
        {
            UI.SetActive(false);
        }
    }

    void HandleZoom()
    {
        if (!currentWeaponSO.CanZoom) return;

        if (starterAssetsInputs.zoom)
        {
            playerFollowCam.m_Lens.FieldOfView = currentWeaponSO.ZoomAmount;
            weaponCam.fieldOfView = currentWeaponSO.ZoomAmount;
            firstPersonController.ChangeRotationSpeed(currentWeaponSO.ZoomRotationSpeed);
            crosshair.SetActive(false);
            zoomUI[currentWeaponSO.CrosshairUI_ID].SetActive(true);
        }
        else
        {
            playerFollowCam.m_Lens.FieldOfView = defaultFOV;
            weaponCam.fieldOfView = defaultFOV;
            firstPersonController.ChangeRotationSpeed(defaultZoomRotationSpeed);
            crosshair.SetActive(true);
            zoomUI[currentWeaponSO.CrosshairUI_ID].SetActive(false);
        }
    }

    public void Checkpoint()
    {
        checkpointAmmoList = currentAmmoList;
        for(int i = 0; i < weaponList.Length; i++)
        {
            weaponsPickedUp[i] = weaponList[i].PickedUp;
        }
        checkpointWeaponsPickedUp = (bool[])weaponsPickedUp.Clone();
    }

    public void IncreaseMaxAmmo()
    {
        for(int i = 0; i< weaponList.Length; i++)
        {
            maxAmmoList[i] = (int)Mathf.Ceil(weaponList[i].MagazineSize * 1.25f);
        }
    }

    public void DecreaseSpread(float decreaseAmount) {
        spreadControl = Mathf.Clamp(spreadControl - decreaseAmount, 0.1f, 1f);
    }

    public void GrantAmmoForWeapon(int weaponId, float ammoPortion) {
        if(!TryGetWeaponById(weaponId, out WeaponSO weaponSO)) {
            return;
        }

        AdjustAmmo(weaponSO, ammoPortion);
    }

    public void GrantWeaponPickup(int weaponId, int ammoAmount) {
        if(!TryGetWeaponById(weaponId, out WeaponSO weaponSO)) {
            return;
        }

        SwitchWeapon(weaponSO);
        AdjustAmmo(ammoAmount);
    }

    private bool TryGetWeaponById(int weaponId, out WeaponSO weaponSO) {
        foreach(WeaponSO candidate in weaponList) {
            if(candidate != null && candidate.ID == weaponId) {
                weaponSO = candidate;
                return true;
            }
        }

        weaponSO = null;
        return false;
    }
}
