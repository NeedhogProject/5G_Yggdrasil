using UnityEngine;
using System.IO;
using System.Collections.Generic;

// 저장 데이터 구조체

/// <summary>플레이어 스탯 저장 데이터</summary>
[System.Serializable]
public class SavedPlayerStats
{
    public float health;
    public float baseDefense;
    public float equipmentDefense;
    public float mental;
}

/// <summary>아이템 인스턴스 저장 데이터</summary>
[System.Serializable]
public class SavedItemInstance
{
    public string instanceId;      // 런타임 고유 ID
    public string itemDataName;    // ScriptableObject 에셋 이름
    public int stackCount;
    public int slotX;
    public int slotY;

    // 무기 전용
    public int enhancementLevel;

    // 방어구 전용
    public int runeSlot1;          // RuneElement 정수값
    public int runeSlot2;
}

/// <summary>전체 세이브 데이터</summary>
[System.Serializable]
public class SaveData
{
    [Header("메타 정보")]
    public int slotIndex;
    public string saveName;        // 플레이어가 직접 지은 저장 파일 이름
    public string saveDateTime;    // 저장 일시 (표시용)
    public float playTime;        // 총 플레이 시간 (초)

    [Header("위치/층")]
    public int currentFloor;
    public float playerX;
    public float playerY;
    public float playerZ;

    [Header("플레이어 스탯")]
    public SavedPlayerStats playerStats;

    [Header("인벤토리")]
    public List<SavedItemInstance> inventoryItems = new List<SavedItemInstance>();

    [Header("장착 장비")]
    public SavedItemInstance equippedWeapon;
    public SavedItemInstance equippedHelmet;
    public SavedItemInstance equippedChest;
    public SavedItemInstance equippedLegs;
    public SavedItemInstance equippedBoots;

    [Header("창고")]
    public List<SavedItemInstance> storageItems = new List<SavedItemInstance>();
}

// SaveSystem

/// <summary>
/// JSON 파일 기반 세이브 시스템
///
/// [기획 반영]
/// - 슬롯 3개 (slot_0.json ~ slot_2.json)
/// - 저장 항목: 플레이어 스탯, 인벤토리, 장착 장비, 창고, 층/위치, 강화/각인 상태
/// - 슬롯마다 플레이어가 직접 이름 지정 가능 (saveName)
/// - Application.persistentDataPath 에 저장 (플랫폼별 자동 경로)
///
/// [사용법]
/// SaveSystem.Instance.Save(slotIndex)
/// SaveSystem.Instance.Save(slotIndex, saveName)
/// SaveSystem.Instance.RenameSave(slotIndex, newName)
/// SaveSystem.Instance.Load(slotIndex)
/// SaveSystem.Instance.DeleteSave(slotIndex)
/// SaveSystem.Instance.HasSave(slotIndex)
/// </summary>
public class SaveSystem : MonoBehaviour
{
    // 싱글턴

    public static SaveSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 설정

    public const int SLOT_COUNT = 3;
    private const string SAVE_FILE_PREFIX = "slot_";
    private const string SAVE_FILE_EXT = ".json";

    /// <summary>저장 파일 경로 반환</summary>
    private string GetSavePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath,
                            SAVE_FILE_PREFIX + slotIndex + SAVE_FILE_EXT);
    }

    // 참조

    [Header("참조 (씬 로드 후 자동 탐색)")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private HouseSystem houseSystem;
    [SerializeField] private InventorySystem inventorySystem;

    private float _playTime = 0f;

    private void Update()
    {
        _playTime += Time.unscaledDeltaTime;
    }

    // 저장

    /// <summary>현재 게임 상태를 슬롯에 저장 (이름 없이)</summary>
    public bool Save(int slotIndex)
    {
        // 기존 이름 유지 (있으면), 없으면 기본 이름
        string existingName = "";

        if (HasSave(slotIndex) == true)
        {
            SaveData existing = GetSaveMeta(slotIndex);

            if (existing != null && string.IsNullOrEmpty(existing.saveName) == false)
            {
                existingName = existing.saveName;
            }
        }

        return SaveInternal(slotIndex, existingName);
    }

    /// <summary>현재 게임 상태를 슬롯에 저장 (이름 지정)</summary>
    public bool Save(int slotIndex, string saveName)
    {
        return SaveInternal(slotIndex, saveName);
    }

    /// <summary>저장 파일 이름만 변경 (게임 데이터는 유지)</summary>
    public bool RenameSave(int slotIndex, string newName)
    {
        if (HasSave(slotIndex) == false)
        {
            Debug.LogWarning("[SaveSystem] 이름 변경 실패 - 슬롯 " + slotIndex + " 에 저장 데이터 없음");
            return false;
        }

        try
        {
            string json = File.ReadAllText(GetSavePath(slotIndex));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            data.saveName = newName;

            string newJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSavePath(slotIndex), newJson);

            Debug.Log("[SaveSystem] 슬롯 " + slotIndex + " 이름 변경: " + newName);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] 이름 변경 실패: " + e.Message);
            return false;
        }
    }

    /// <summary>실제 저장 처리 내부 메서드</summary>
    private bool SaveInternal(int slotIndex, string saveName)
    {
        if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
        {
            Debug.LogError("[SaveSystem] 잘못된 슬롯 인덱스: " + slotIndex);
            return false;
        }

        RefreshReferences();

        SaveData data = new SaveData();
        data.slotIndex = slotIndex;
        data.saveName = saveName;
        data.saveDateTime = System.DateTime.Now.ToString("yyyy.MM.dd  HH:mm");
        data.playTime = _playTime;

        if (GameManager.Instance != null)
        {
            data.currentFloor = GameManager.Instance.CurrentFloor;
        }

        // 플레이어 위치
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
            data.playerZ = player.transform.position.z;
        }

        // 플레이어 스탯
        if (playerStats != null)
        {
            data.playerStats = new SavedPlayerStats();
            data.playerStats.health = playerStats.Health;
            data.playerStats.baseDefense = playerStats.BaseDefense;
            data.playerStats.equipmentDefense = playerStats.EquipmentDefense;
            data.playerStats.mental = playerStats.Mental;
        }

        // 장착 장비
        if (playerEquipment != null)
        {
            data.equippedWeapon = SerializeItem(playerEquipment.EquippedWeapon);
            data.equippedHelmet = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Helmet));
            data.equippedChest = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Chest));
            data.equippedLegs = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Legs));
            data.equippedBoots = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Boots));
        }

        // 창고
        if (houseSystem != null)
        {
            foreach (ItemInstance item in houseSystem.GetStorageItems())
            {
                data.storageItems.Add(SerializeItem(item));
            }
        }

        // 인벤토리
        if (inventorySystem == null)
        {
            inventorySystem = InventorySystem.Instance;
        }

        if (inventorySystem != null)
        {
            List<ItemInstance> allInstances = inventorySystem.GetAllInstances();

            for (int i = 0; i < allInstances.Count; i++)
            {
                data.inventoryItems.Add(SerializeItem(allInstances[i]));
            }
        }

        // JSON 직렬화 후 파일 저장
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSavePath(slotIndex), json);
            Debug.Log("[SaveSystem] 슬롯 " + slotIndex + " 저장 완료 (" + saveName + ")");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] 저장 실패: " + e.Message);
            return false;
        }
    }

    // 불러오기

    /// <summary>슬롯에서 데이터 불러오기</summary>
    public bool Load(int slotIndex)
    {
        if (HasSave(slotIndex) == false)
        {
            Debug.LogWarning("[SaveSystem] 슬롯 " + slotIndex + " 에 저장 데이터 없음");
            return false;
        }

        try
        {
            string json = File.ReadAllText(GetSavePath(slotIndex));
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            RefreshReferences();
            ApplySaveData(data);

            _playTime = data.playTime;
            Debug.Log("[SaveSystem] 슬롯 " + slotIndex + " 불러오기 완료");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] 불러오기 실패: " + e.Message);
            return false;
        }
    }

    /// <summary>불러온 SaveData 를 실제 게임 상태에 적용</summary>
    private void ApplySaveData(SaveData data)
    {
        // 플레이어 스탯 복원
        if (playerStats != null && data.playerStats != null)
        {
            float healthDiff = data.playerStats.health - playerStats.Health;
            float mentalDiff = data.playerStats.mental - playerStats.Mental;
            float defenseDiff = data.playerStats.baseDefense - playerStats.BaseDefense;

            if (healthDiff != 0f) { playerStats.ModifyHealth(healthDiff); }
            if (mentalDiff != 0f) { playerStats.ModifyMental(mentalDiff); }
            if (defenseDiff != 0f) { playerStats.ModifyBaseDefense(defenseDiff); }

            playerStats.SetEquipmentDefense(data.playerStats.equipmentDefense);
        }

        Debug.Log("[SaveSystem] 복원 층: " + data.currentFloor);

        // 인벤토리 복원
        RestoreInventory(data.inventoryItems);

        // 장착 장비 복원
        RestoreEquipment(data);

        // 창고 복원
        RestoreStorage(data.storageItems);
    }

    // 역직렬화 - 인벤토리

    /// <summary>저장된 인벤토리 아이템 목록을 InventorySystem 에 복원</summary>
    private void RestoreInventory(List<SavedItemInstance> savedList)
    {
        if (inventorySystem == null)
        {
            Debug.LogWarning("[SaveSystem] InventorySystem 없음 - 인벤토리 복원 건너뜀");
            return;
        }

        if (savedList == null || savedList.Count == 0)
        {
            return;
        }

        // 기존 인벤토리 비우기 (이어하기 시 중복 방지)
        List<ItemInstance> existing = inventorySystem.GetAllInstances();
        List<ItemInstance> toRemove = new List<ItemInstance>(existing);

        for (int i = 0; i < toRemove.Count; i++)
        {
            inventorySystem.RemoveItem(toRemove[i]);
        }

        for (int i = 0; i < savedList.Count; i++)
        {
            SavedItemInstance saved = savedList[i];
            ItemInstance instance = DeserializeItem(saved);

            if (instance == null) { continue; }

            bool placed = inventorySystem.AddItem(instance);

            if (placed == false)
            {
                Debug.LogWarning("[SaveSystem] 인벤토리 배치 실패 (가득 참): " + saved.itemDataName);
            }
        }

        Debug.Log("[SaveSystem] 인벤토리 복원 완료: " + savedList.Count + "개");
    }

    // 역직렬화 - 장착 장비

    /// <summary>저장된 장착 장비를 PlayerEquipment 에 복원</summary>
    private void RestoreEquipment(SaveData data)
    {
        if (playerEquipment == null)
        {
            Debug.LogWarning("[SaveSystem] PlayerEquipment 없음 - 장비 복원 건너뜀");
            return;
        }

        RestoreWeapon(data.equippedWeapon);
        RestoreArmor(data.equippedHelmet);
        RestoreArmor(data.equippedChest);
        RestoreArmor(data.equippedLegs);
        RestoreArmor(data.equippedBoots);
    }

    /// <summary>무기 복원 후 장착</summary>
    private void RestoreWeapon(SavedItemInstance saved)
    {
        if (saved == null || string.IsNullOrEmpty(saved.itemDataName) == true) { return; }

        ItemInstance instance = DeserializeItem(saved);
        WeaponInstance weapon = instance as WeaponInstance;

        if (weapon == null) { return; }

        playerEquipment.EquipItem(weapon);
        Debug.Log("[SaveSystem] 무기 복원: " + saved.itemDataName + " +" + saved.enhancementLevel);
    }

    /// <summary>방어구 복원 후 장착</summary>
    private void RestoreArmor(SavedItemInstance saved)
    {
        if (saved == null || string.IsNullOrEmpty(saved.itemDataName) == true) { return; }

        ItemInstance instance = DeserializeItem(saved);
        ArmorInstance armor = instance as ArmorInstance;

        if (armor == null) { return; }

        playerEquipment.EquipItem(armor);
        Debug.Log("[SaveSystem] 방어구 복원: " + saved.itemDataName);
    }

    // 역직렬화 - 창고

    /// <summary>저장된 창고 아이템을 HouseSystem 에 복원</summary>
    private void RestoreStorage(List<SavedItemInstance> savedList)
    {
        if (houseSystem == null)
        {
            Debug.LogWarning("[SaveSystem] HouseSystem 없음 - 창고 복원 건너뜀");
            return;
        }

        if (savedList == null || savedList.Count == 0) { return; }

        List<ItemInstance> restoredItems = new List<ItemInstance>();

        for (int i = 0; i < savedList.Count; i++)
        {
            ItemInstance instance = DeserializeItem(savedList[i]);
            if (instance != null) { restoredItems.Add(instance); }
        }

        houseSystem.LoadStorage(restoredItems);
        Debug.Log("[SaveSystem] 창고 복원 완료: " + restoredItems.Count + "개");
    }

    // 핵심 역직렬화

    /// <summary>
    /// SavedItemInstance 를 실제 ItemInstance 로 변환
    /// ItemDataRegistry 에서 에셋 이름으로 ScriptableObject 를 찾아 생성
    /// </summary>
    private ItemInstance DeserializeItem(SavedItemInstance saved)
    {
        if (saved == null) { return null; }
        if (string.IsNullOrEmpty(saved.itemDataName) == true) { return null; }

        if (ItemDataRegistry.Instance == null)
        {
            Debug.LogError("[SaveSystem] ItemDataRegistry 없음 - 씬에 배치됐는지 확인");
            return null;
        }

        ItemData itemData = ItemDataRegistry.Instance.Find(saved.itemDataName);
        if (itemData == null) { return null; }

        ItemInstance instance = null;

        if (itemData is WeaponData weaponData)
        {
            WeaponInstance weapon = new WeaponInstance(weaponData);

            for (int i = 0; i < saved.enhancementLevel; i++)
            {
                weapon.TryEnhance(true);
            }

            instance = weapon;
        }
        else if (itemData is ArmorData armorData)
        {
            ArmorInstance armor = new ArmorInstance(armorData);
            RuneElement rune1 = (RuneElement)saved.runeSlot1;
            RuneElement rune2 = (RuneElement)saved.runeSlot2;

            if (rune1 != RuneElement.None) { armor.SetRune(1, rune1); }
            if (rune2 != RuneElement.None) { armor.SetRune(2, rune2); }

            instance = armor;
        }
        else
        {
            instance = new ItemInstance(itemData);

            if (instance != null)
            {
                instance.AddStack(saved.stackCount - 1);
            }
        }

        return instance;
    }

    // 직렬화

    /// <summary>ItemInstance 를 SavedItemInstance 로 변환</summary>
    private SavedItemInstance SerializeItem(ItemInstance item)
    {
        if (item == null) { return null; }

        SavedItemInstance saved = new SavedItemInstance();
        saved.instanceId = item.InstanceId;
        saved.itemDataName = item.Data != null ? item.Data.name : "";
        saved.stackCount = item.StackCount;
        saved.slotX = item.SlotPosition.x;
        saved.slotY = item.SlotPosition.y;

        if (item is WeaponInstance weapon)
        {
            saved.enhancementLevel = weapon.EnhancementLevel;
        }
        else if (item is ArmorInstance armor)
        {
            saved.runeSlot1 = (int)armor.RuneSlot1;
            saved.runeSlot2 = (int)armor.RuneSlot2;
        }

        return saved;
    }

    // 슬롯 관리

    /// <summary>해당 슬롯에 저장 데이터가 있는지</summary>
    public bool HasSave(int slotIndex)
    {
        return File.Exists(GetSavePath(slotIndex));
    }

    /// <summary>슬롯 메타 정보 반환 (UI 표시용)</summary>
    public SaveData GetSaveMeta(int slotIndex)
    {
        if (HasSave(slotIndex) == false) { return null; }

        try
        {
            string json = File.ReadAllText(GetSavePath(slotIndex));
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>슬롯 삭제</summary>
    public bool DeleteSave(int slotIndex)
    {
        string path = GetSavePath(slotIndex);

        if (File.Exists(path) == false) { return false; }

        File.Delete(path);
        Debug.Log("[SaveSystem] 슬롯 " + slotIndex + " 삭제");
        return true;
    }

    /// <summary>전체 슬롯 상태 반환 (타이틀 UI 용)</summary>
    public (bool exists, string saveName, string dateTime, int floor)[] GetAllSlotInfo()
    {
        (bool, string, string, int)[] result = new (bool, string, string, int)[SLOT_COUNT];

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            SaveData meta = GetSaveMeta(i);

            if (meta != null)
            {
                result[i] = (true, meta.saveName, meta.saveDateTime, meta.currentFloor);
            }
            else
            {
                result[i] = (false, "", "", 0);
            }
        }

        return result;
    }

    // 참조 갱신

    private void RefreshReferences()
    {
        if (playerStats == null) { playerStats = FindFirstObjectByType<PlayerStats>(); }
        if (playerEquipment == null) { playerEquipment = FindFirstObjectByType<PlayerEquipment>(); }
        if (houseSystem == null) { houseSystem = FindFirstObjectByType<HouseSystem>(); }
        if (inventorySystem == null) { inventorySystem = InventorySystem.Instance; }
    }

#if UNITY_EDITOR
    [ContextMenu("슬롯 0 저장 테스트")] private void TestSave() { Save(0); }
    [ContextMenu("슬롯 0 불러오기 테스트")] private void TestLoad() { Load(0); }
    [ContextMenu("저장 경로 출력")]
    private void PrintSavePath()
    {
        Debug.Log("[SaveSystem] 저장 경로: " + Application.persistentDataPath);
    }
#endif
}