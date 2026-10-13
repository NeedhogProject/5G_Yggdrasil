using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 게임 저장/불러오기 (슬롯 없이 파일 1개)
///
/// [기획 반영]
/// - 자동 저장: 마을 도착, 층 이동, 강화/각인 결과가 나왔을 때
/// - 새 게임은 기존 저장을 건드리지 않고, 플레이어가 설정창 저장 버튼(SaveGameButton)을 처음 누른 뒤부터 자동 저장
/// - 타이틀 이어하기로 불러오기 (이어하기는 처음부터 자동 저장)
/// - 저장 항목: 인벤토리, 장착 장비, 유물 칸, 창고, 달란, 체력/정신력, 각인 재료, 현재 층
/// - 아이템은 고유 ID(에셋 GUID)로 저장하고 ItemDataRegistry 에서 찾아 복원 (에셋 이름을 바꿔도 유지)
/// </summary>
public static class SaveSystem
{
    // 저장 데이터 아이템 1개
    [System.Serializable]
    public class SavedItem
    {
        public string itemId = "";     // ItemData 고유 ID (비어 있으면 없음)
        public int stackCount = 1;
        public int slotX = -1;
        public int slotY = -1;
        public int enhancementLevel;   // 무기
        public int rune;               // 방어구 각인 (RuneElement)
        public bool identified;        // 유물 감정 여부
    }

    // 저장 데이터 전체
    [System.Serializable]
    public class SaveData
    {
        public int floor;
        public float health;
        public float mental;
        public int gold;
        public List<SavedItem> inventory = new List<SavedItem>();
        public SavedItem weapon;
        public List<SavedItem> armors = new List<SavedItem>();
        public List<SavedItem> relics = new List<SavedItem>();
        public List<SavedItem> storage = new List<SavedItem>();
        public List<int> resources = new List<int>();   // RESOURCE_TYPES 순서
    }

    private const string FILE_NAME = "save.json";

    // 저장할 방어구 부위와 각인 재료 종류 (저장 순서 고정)
    private static readonly ArmorSlot[] ARMOR_SLOTS = { ArmorSlot.Helmet, ArmorSlot.Chest, ArmorSlot.Legs, ArmorSlot.Boots };
    private static readonly InscriptionType[] RESOURCE_TYPES = { InscriptionType.Fire, InscriptionType.Water, InscriptionType.Wind, InscriptionType.Earth, InscriptionType.Darkness };

    private static string SavePath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    /// <summary>자동 저장 허용 여부 — GameManager 가 새 게임 false, 이어하기 true 로 설정</summary>
    public static bool IsAutoSaveEnabled { get; set; } = false;

    /// <summary>저장 파일이 있는지 — TitleSceneUI 이어하기 버튼 표시용</summary>
    public static bool HasSave => File.Exists(SavePath);

    // ─────────────────────── 저장 ───────────────────────

    /// <summary>자동 저장 (마을 도착, 층 이동, 강화/각인 결과). 저장 버튼을 누르기 전 새 게임이면 저장 안 함</summary>
    public static void Save()
    {
        if (IsAutoSaveEnabled == false)
        {
            return;
        }
        WriteSave();
    }

    /// <summary>설정창 저장 버튼 — 저장하고 이후 자동 저장을 켬, 저장 성공 여부 반환</summary>
    public static bool SaveManually()
    {
        if (WriteSave() == false)
        {
            return false;
        }
        IsAutoSaveEnabled = true;
        return true;
    }

    // 현재 상태를 파일로 저장 (플레이어가 없는 화면이면 저장하지 않음)
    private static bool WriteSave()
    {
        PlayerStats stats = PlayerStats.Instance;
        InventorySystem inventory = InventorySystem.Instance;
        if (stats == null || inventory == null)
        {
            return false;
        }

        // 사망 화면(체력 0, 아이템 잃은 상태)에서는 저장하지 않음
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
        {
            return false;
        }

        // 위험: 레지스트리 없이 저장하면 아이템이 전부 빈 칸으로 저장되어 기존 저장을 덮어씀
        if (ItemDataRegistry.Instance == null)
        {
            Debug.LogWarning("[SaveSystem] ItemDataRegistry 가 없어 저장하지 않음 (GameCore 프리팹 확인)");
            return false;
        }

        SaveData data = new SaveData();
        data.floor = GameManager.Instance != null ? GameManager.Instance.CurrentFloor : 0;
        data.health = stats.Health;
        data.mental = stats.Mental;
        data.gold = stats.gold;

        foreach (ItemInstance item in inventory.GetAllItemsForSave())
        {
            data.inventory.Add(ToSaved(item));
        }

        PlayerEquipment equipment = stats.GetComponent<PlayerEquipment>();
        if (equipment != null)
        {
            data.weapon = ToSaved(equipment.EquippedWeapon);
            foreach (ArmorSlot slot in ARMOR_SLOTS)
            {
                data.armors.Add(ToSaved(equipment.GetArmor(slot)));
            }
            for (int i = 0; i < equipment.RelicSlotCount; i++)
            {
                data.relics.Add(ToSaved(equipment.GetRelic(i)));
            }
        }

        foreach (ItemInstance item in HouseSystem.GetStorageForSave())
        {
            data.storage.Add(ToSaved(item));
        }

        foreach (InscriptionType type in RESOURCE_TYPES)
        {
            data.resources.Add(ResourceInventory.Instance != null ? ResourceInventory.Instance.GetResourceCount(type) : 0);
        }

        // 위험: 강화/각인 처리 도중 호출되므로 저장 실패가 예외로 번지면 NPC 창이 멈춤
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log("[SaveSystem] 저장 완료: " + SavePath);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] 저장 실패: " + e.Message);
            return false;
        }
    }

    // ─────────────────────── 불러오기 ───────────────────────

    /// <summary>저장 파일 읽기 (없거나 깨졌으면 null)</summary>
    public static SaveData Load()
    {
        if (HasSave == false)
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] 불러오기 실패: " + e.Message);
            return null;
        }
    }

    /// <summary>창고 복원 — 씬 로드 전 GameManager.ContinueGame 에서 호출</summary>
    public static void ApplyStorage(SaveData _data)
    {
        List<ItemInstance> items = new List<ItemInstance>();
        foreach (SavedItem saved in _data.storage)
        {
            ItemInstance item = FromSaved(saved);
            if (item != null)
            {
                items.Add(item);
            }
        }
        HouseSystem.LoadStorage(items);
    }

    /// <summary>플레이어 상태 복원 — 씬 로드 후 인벤토리 칸이 만들어진 뒤 GameManager 에서 호출</summary>
    public static void ApplyPlayer(SaveData _data)
    {
        PlayerStats stats = PlayerStats.Instance;
        InventorySystem inventory = InventorySystem.Instance;
        if (stats == null || inventory == null)
        {
            Debug.LogWarning("[SaveSystem] 플레이어 또는 인벤토리가 없어 복원하지 못함");
            return;
        }

        foreach (SavedItem saved in _data.inventory)
        {
            ItemInstance item = FromSaved(saved);
            if (item == null)
            {
                continue;
            }
            // 저장된 칸에 못 넣으면 빈 칸에 배치
            InventorySlot slot = inventory.GetSlotAt(new Vector2Int(saved.slotX, saved.slotY));
            if (slot == null || inventory.AddItemAtSlot(slot, item) == false)
            {
                inventory.AddItem(item);
            }
        }

        // 장비 먼저 복원해야 최대 체력, 최대 정신력이 맞춰진 뒤 현재 수치를 넣을 수 있음
        PlayerEquipment equipment = stats.GetComponent<PlayerEquipment>();
        if (equipment != null)
        {
            ItemInstance weapon = FromSaved(_data.weapon);
            if (weapon != null)
            {
                equipment.EquipItem(weapon);
            }
            foreach (SavedItem saved in _data.armors)
            {
                ItemInstance armor = FromSaved(saved);
                if (armor != null)
                {
                    equipment.EquipItem(armor);
                }
            }
            for (int i = 0; i < _data.relics.Count; i++)
            {
                ItemInstance relic = FromSaved(_data.relics[i]);
                if (relic != null)
                {
                    equipment.EquipRelicAt(i, relic);
                }
            }
        }

        stats.ModifyHealth(_data.health - stats.Health);
        stats.ModifyMental(_data.mental - stats.Mental);
        stats.gold = _data.gold;

        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.ClearAll();
            for (int i = 0; i < RESOURCE_TYPES.Length && i < _data.resources.Count; i++)
            {
                ResourceInventory.Instance.AddResource(RESOURCE_TYPES[i], _data.resources[i]);
            }
        }

        Debug.Log("[SaveSystem] 불러오기 완료");
    }

    // ─────────────────────── 변환 ───────────────────────

    // 런타임 아이템을 저장 데이터로 (없으면 이름 빈 칸)
    private static SavedItem ToSaved(ItemInstance _item)
    {
        SavedItem saved = new SavedItem();
        if (_item == null || _item.Data == null || ItemDataRegistry.Instance == null)
        {
            return saved;
        }

        saved.itemId = ItemDataRegistry.Instance.GetId(_item.Data);
        saved.stackCount = _item.StackCount;
        saved.slotX = _item.SlotPosition.x;
        saved.slotY = _item.SlotPosition.y;

        if (_item is WeaponInstance weapon)
        {
            saved.enhancementLevel = weapon.EnhancementLevel;
        }
        else if (_item is ArmorInstance armor)
        {
            saved.rune = (int)armor.RuneSlot1;
        }
        else if (_item is RelicInstance relic)
        {
            saved.identified = relic.IsIdentified;
        }
        return saved;
    }

    // 저장 데이터를 런타임 아이템으로 (강화, 각인, 감정, 수량 복원)
    private static ItemInstance FromSaved(SavedItem _saved)
    {
        if (_saved == null || string.IsNullOrEmpty(_saved.itemId) == true)
        {
            return null;
        }
        if (ItemDataRegistry.Instance == null)
        {
            Debug.LogWarning("[SaveSystem] ItemDataRegistry 가 없어 아이템을 복원하지 못함: " + _saved.itemId);
            return null;
        }

        ItemInstance item = InventorySystem.CreateInstanceFromData(ItemDataRegistry.Instance.FindById(_saved.itemId));
        if (item == null)
        {
            return null;
        }

        if (item is WeaponInstance weapon)
        {
            weapon.RestoreEnhancementLevel(_saved.enhancementLevel);
        }
        else if (item is ArmorInstance armor && (RuneElement)_saved.rune != RuneElement.None)
        {
            armor.SetRune((RuneElement)_saved.rune);
        }
        else if (item is RelicInstance relic && _saved.identified == true)
        {
            relic.Identify();
        }

        item.AddStack(_saved.stackCount - 1);
        return item;
    }
}
