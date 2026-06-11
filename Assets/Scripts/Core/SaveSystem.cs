using UnityEngine;
using System.IO;
using System.Collections.Generic;

// ─────────────────────── 저장 데이터 구조체 ───────────────────────

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
    public string instanceId;       // 런타임 고유 ID
    public string itemDataName;     // ScriptableObject 에셋 이름 (Resources 폴더 기준)
    public int    stackCount;
    public int    slotX;
    public int    slotY;

    // 무기 전용
    public int    enhancementLevel;

    // 방어구 전용
    public int    runeSlot1;        // RuneElement 정수값
    public int    runeSlot2;
}

/// <summary>전체 세이브 데이터</summary>
[System.Serializable]
public class SaveData
{
    [Header("메타 정보")]
    public int    slotIndex;
    public string saveName;         // 저장 이름 (슬롯 UI 표시용)
    public string saveDateTime;     // 저장 일시 (표시용)
    public float  playTime;         // 총 플레이 시간 (초)

    [Header("위치/층")]
    public int    currentFloor;
    public float  playerX;
    public float  playerY;
    public float  playerZ;

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

// ─────────────────────── SaveSystem ───────────────────────

/// <summary>
/// JSON 파일 기반 세이브 시스템
///
/// [기획 반영]
/// - 슬롯 3개 (slot_0.json ~ slot_2.json)
/// - 저장 항목: 플레이어 스탯, 인벤토리, 장착 장비, 창고, 층/위치, 강화/각인 상태
/// - Application.persistentDataPath 에 저장 (플랫폼별 자동 경로)
///
/// [사용법]
/// SaveSystem.Instance.Save(slotIndex)
/// SaveSystem.Instance.Load(slotIndex)
/// SaveSystem.Instance.DeleteSave(slotIndex)
/// SaveSystem.Instance.HasSave(slotIndex)
/// </summary>
public class SaveSystem : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static SaveSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────── 설정 ───────────────────────

    public const int SLOT_COUNT = 5;
    private const string SAVE_FILE_PREFIX = "slot_";
    private const string SAVE_FILE_EXT    = ".json";

    /// <summary>저장 파일 경로 반환</summary>
    private string GetSavePath(int slotIndex) =>
        Path.Combine(Application.persistentDataPath,
                     $"{SAVE_FILE_PREFIX}{slotIndex}{SAVE_FILE_EXT}");

    // ─────────────────────── 참조 ───────────────────────

    [Header("참조 (씬 로드 후 자동 탐색)")]
    [SerializeField] private PlayerStats     playerStats;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private HouseSystem     houseSystem;
    [SerializeField] private InventorySystem inventorySystem;

    [Header("아이템 데이터베이스 (복원용)")]
    [Tooltip("모든 ItemData 에셋 목록. 컴포넌트 우클릭 '아이템 데이터베이스 자동 수집'으로 채움")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    private float _playTime = 0f;

    private void Update() => _playTime += Time.unscaledDeltaTime;

    // ─────────────────────── 저장 ───────────────────────

    /// <summary>
    /// 현재 게임 상태를 슬롯에 저장 (이름 없이 호출 시 기존 이름 유지)
    /// </summary>
    public bool Save(int slotIndex)
    {
        // 기존 세이브가 있으면 그 이름을 유지, 없으면 기본 이름
        string existingName = GetSaveMeta(slotIndex)?.saveName ?? "";
        return Save(slotIndex, existingName);
    }

    /// <summary>
    /// 현재 게임 상태를 슬롯에 저장 (저장 이름 지정)
    /// </summary>
    public bool Save(int slotIndex, string saveName)
    {
        if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
        {
            Debug.LogError($"[SaveSystem] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        RefreshReferences();

        SaveData data = new SaveData
        {
            slotIndex    = slotIndex,
            saveName     = saveName,
            saveDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            playTime     = _playTime,
            currentFloor = GameManager.Instance?.CurrentFloor ?? 0
        };

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
            data.playerStats = new SavedPlayerStats
            {
                health            = playerStats.Health,
                baseDefense       = playerStats.BaseDefense,
                equipmentDefense  = playerStats.EquipmentDefense,
                mental            = playerStats.Mental
            };
        }

        // 장착 장비
        if (playerEquipment != null)
        {
            data.equippedWeapon  = SerializeItem(playerEquipment.EquippedWeapon);
            data.equippedHelmet  = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Helmet));
            data.equippedChest   = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Chest));
            data.equippedLegs    = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Legs));
            data.equippedBoots   = SerializeItem(playerEquipment.GetArmor(ArmorSlot.Boots));
        }

        // 창고
        if (houseSystem != null)
            foreach (ItemInstance item in houseSystem.GetStorageItems())
                data.storageItems.Add(SerializeItem(item));

        // 인벤토리 저장
        if (inventorySystem == null)
        {
            inventorySystem = InventorySystem.Instance;
        }

        if (inventorySystem == null == false)
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
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(GetSavePath(slotIndex), json);
            Debug.Log($"[SaveSystem] 슬롯 {slotIndex} 저장 완료 → {GetSavePath(slotIndex)}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
            return false;
        }
    }

    // ─────────────────────── 불러오기 ───────────────────────

    /// <summary>
    /// 슬롯에서 데이터 불러오기
    /// </summary>
    public bool Load(int slotIndex)
    {
        if (HasSave(slotIndex) == false)
        {
            Debug.LogWarning($"[SaveSystem] 슬롯 {slotIndex} 에 저장 데이터 없음");
            return false;
        }

        try
        {
            string   json = File.ReadAllText(GetSavePath(slotIndex));
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            RefreshReferences();
            ApplySaveData(data);

            _playTime = data.playTime;
            Debug.Log($"[SaveSystem] 슬롯 {slotIndex} 불러오기 완료");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 불러오기 실패: {e.Message}");
            return false;
        }
    }

    private void ApplySaveData(SaveData data)
    {
        // 플레이어 스탯 복원
        if (playerStats != null && data.playerStats != null)
        {
            float healthDiff  = data.playerStats.health  - playerStats.Health;
            float mentalDiff  = data.playerStats.mental  - playerStats.Mental;
            float defenseDiff = data.playerStats.baseDefense - playerStats.BaseDefense;

            if (healthDiff  != 0f) playerStats.ModifyHealth(healthDiff);
            if (mentalDiff  != 0f) playerStats.ModifyMental(mentalDiff);
            if (defenseDiff != 0f) playerStats.ModifyBaseDefense(defenseDiff);
            playerStats.SetEquipmentDefense(data.playerStats.equipmentDefense);
        }

        // 층/위치 복원
        if (GameManager.Instance != null)
        {
            // 층 이동은 GameManager 가 씬 전환으로 처리
            Debug.Log($"[SaveSystem] 복원 층: {data.currentFloor}");
        }

        // 인벤토리 복원 (저장 순서대로 빈 칸 자동 배치)
        if (inventorySystem != null)
        {
            for (int i = 0; i < data.inventoryItems.Count; i++)
            {
                ItemInstance instance = DeserializeItem(data.inventoryItems[i]);
                if (instance != null)
                {
                    inventorySystem.AddItem(instance);
                }
            }
        }

        // 장착 장비 복원
        if (playerEquipment != null)
        {
            RestoreEquipped(data.equippedWeapon);
            RestoreEquipped(data.equippedHelmet);
            RestoreEquipped(data.equippedChest);
            RestoreEquipped(data.equippedLegs);
            RestoreEquipped(data.equippedBoots);
        }

        // 창고 복원
        if (houseSystem != null)
        {
            for (int i = 0; i < data.storageItems.Count; i++)
            {
                ItemInstance instance = DeserializeItem(data.storageItems[i]);
                if (instance != null)
                {
                    houseSystem.AddToStorage(instance);
                }
            }
        }
    }

    /// <summary>저장된 장비 한 칸을 역직렬화 후 장착</summary>
    private void RestoreEquipped(SavedItemInstance saved)
    {
        ItemInstance instance = DeserializeItem(saved);
        if (instance == null) return;

        playerEquipment.EquipItem(instance);
    }

    // ─────────────────────── 직렬화 / 역직렬화 ───────────────────────

    private SavedItemInstance SerializeItem(ItemInstance item)
    {
        if (item == null) return null;

        SavedItemInstance saved = new SavedItemInstance
        {
            instanceId    = item.InstanceId,
            itemDataName  = item.Data?.name ?? "",
            stackCount    = item.StackCount,
            slotX         = item.SlotPosition.x,
            slotY         = item.SlotPosition.y,
        };

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

    /// <summary>저장 데이터를 런타임 인스턴스로 복원 (강화/각인/스택 포함)</summary>
    private ItemInstance DeserializeItem(SavedItemInstance saved)
    {
        if (saved == null || string.IsNullOrEmpty(saved.itemDataName)) return null;

        ItemData data = FindItemData(saved.itemDataName);
        if (data == null)
        {
            Debug.LogWarning($"[SaveSystem] 아이템 데이터 못 찾음: {saved.itemDataName} (데이터베이스 수집 확인)");
            return null;
        }

        ItemInstance instance;

        if (data is WeaponData weaponData)
        {
            WeaponInstance weapon = new WeaponInstance(weaponData);
            weapon.RestoreEnhancementLevel(saved.enhancementLevel);
            instance = weapon;
        }
        else if (data is ArmorData armorData)
        {
            ArmorInstance armor = new ArmorInstance(armorData);
            if ((RuneElement)saved.runeSlot1 != RuneElement.None)
            {
                armor.SetRune(1, (RuneElement)saved.runeSlot1);
            }
            if ((RuneElement)saved.runeSlot2 != RuneElement.None)
            {
                armor.SetRune(2, (RuneElement)saved.runeSlot2);
            }
            instance = armor;
        }
        else
        {
            instance = new ItemInstance(data);
        }

        // 스택 복원 (생성 시 1 이므로 차이만큼 추가)
        if (saved.stackCount > 1)
        {
            instance.AddStack(saved.stackCount - 1);
        }

        return instance;
    }

    /// <summary>에셋 이름으로 ItemData 조회</summary>
    private ItemData FindItemData(string assetName)
    {
        for (int i = 0; i < itemDatabase.Count; i++)
        {
            if (itemDatabase[i] != null && itemDatabase[i].name == assetName)
            {
                return itemDatabase[i];
            }
        }
        return null;
    }

    // ─────────────────────── 슬롯 관리 ───────────────────────

    /// <summary>해당 슬롯에 저장 데이터가 있는지</summary>
    public bool HasSave(int slotIndex) => File.Exists(GetSavePath(slotIndex));

    /// <summary>슬롯 메타 정보 반환 (타이틀 UI 표시용)</summary>
    public SaveData GetSaveMeta(int slotIndex)
    {
        if (HasSave(slotIndex) == false) return null;
        try
        {
            string json = File.ReadAllText(GetSavePath(slotIndex));
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch { return null; }
    }

    /// <summary>슬롯 저장 이름 변경 (파일의 saveName 만 교체)</summary>
    public bool RenameSave(int slotIndex, string newName)
    {
        if (HasSave(slotIndex) == false)
        {
            Debug.LogWarning($"[SaveSystem] 슬롯 {slotIndex} 에 저장 데이터 없음");
            return false;
        }

        try
        {
            string   json = File.ReadAllText(GetSavePath(slotIndex));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            data.saveName = newName;
            File.WriteAllText(GetSavePath(slotIndex), JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log($"[SaveSystem] 슬롯 {slotIndex} 이름 변경: {newName}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 이름 변경 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>슬롯 삭제</summary>
    public bool DeleteSave(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path) == false) return false;
        File.Delete(path);
        Debug.Log($"[SaveSystem] 슬롯 {slotIndex} 삭제");
        return true;
    }

    /// <summary>전체 슬롯 상태 반환 (타이틀 UI 용)</summary>
    public (bool exists, string dateTime, int floor)[] GetAllSlotInfo()
    {
        (bool, string, int)[] result = new (bool, string, int)[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            SaveData meta = GetSaveMeta(i);
            result[i] = meta != null
                ? (true, meta.saveDateTime, meta.currentFloor)
                : (false, "", 0);
        }
        return result;
    }

    // ─────────────────────── 참조 갱신 ───────────────────────

    private void RefreshReferences()
    {
        if (playerStats     == null) playerStats     = FindFirstObjectByType<PlayerStats>();
        if (playerEquipment == null) playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        if (houseSystem     == null) houseSystem     = FindFirstObjectByType<HouseSystem>();
        if (inventorySystem == null) inventorySystem = InventorySystem.Instance;
    }

#if UNITY_EDITOR
    [ContextMenu("아이템 데이터베이스 자동 수집")]
    private void CollectItemDatabase()
    {
        itemDatabase.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/ScriptableObjects" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData data = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null)
            {
                itemDatabase.Add(data);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[SaveSystem] 아이템 데이터베이스 수집: {itemDatabase.Count}개");
    }

    [ContextMenu("슬롯 0 저장 테스트")] private void TestSave() => Save(0);
    [ContextMenu("슬롯 0 불러오기 테스트")] private void TestLoad() => Load(0);
    [ContextMenu("저장 경로 출력")]
    private void PrintSavePath() =>
        Debug.Log($"[SaveSystem] 저장 경로: {Application.persistentDataPath}");
#endif
}