using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기획 테이블(JSON) 보관소
///
/// - 게임 중: ItemDataRegistry.Awake 가 Load 로 넘겨준 JSON 을 쓴다
/// - 에디터: 아직 불러오지 않았으면 Assets/Planning 에서 직접 읽는다 (GameCore 없는 테스트 씬, 인스펙터)
/// - 조회 키는 SO 에셋 이름 (WeaponData.name 등)
/// </summary>
public static class GameTables
{
    public const string WEAPON_JSON_PATH = "Assets/Planning/Weapon.json";
    public const string ARMOR_JSON_PATH = "Assets/Planning/Armor.json";
    public const string ENHANCE_JSON_PATH = "Assets/Planning/Enhance.json";
    public const string SET_BONUS_JSON_PATH = "Assets/Planning/SetBonus.json";
    public const string SET_EFFECT_JSON_PATH = "Assets/Planning/SetEffect.json";
    public const string CONSUMABLE_JSON_PATH = "Assets/Planning/Consumable.json";
    public const string RELIC_JSON_PATH = "Assets/Planning/Relic.json";
    public const string RESOURCE_JSON_PATH = "Assets/Planning/Resource.json";
    public const string FLOOR_KEY_JSON_PATH = "Assets/Planning/FloorKey.json";
    public const string ENHANCE_MATERIAL_JSON_PATH = "Assets/Planning/EnhanceMaterial.json";
    public const string SELLABLE_JSON_PATH = "Assets/Planning/Sellable.json";

    private static Dictionary<string, WeaponRow> weaponRows = null;
    private static Dictionary<string, ArmorRow> armorRows = null;
    private static float[] successRates = null;
    private static float[] attackMultipliers = null;
    private static float[] speedMultipliers = null;
    private static SetBonusRow[] setBonusRows = null;
    private static Dictionary<string, float> setEffectValues = null;
    private static Dictionary<string, ConsumableRow> consumableRows = null;
    private static Dictionary<string, RelicRow> relicRows = null;
    private static Dictionary<string, ResourceRow> resourceRows = null;
    private static Dictionary<string, FloorKeyRow> floorKeyRows = null;
    private static Dictionary<string, EnhanceMaterialRow> enhanceMaterialRows = null;
    private static Dictionary<string, SellableRow> sellableRows = null;
    private static bool bLoadAttempted = false;   // 실패 시 매 조회마다 재시도 방지

    // 없는 ID 경고는 ID 마다 1번만
    private static readonly HashSet<string> reportedMissing = new HashSet<string>();

    public static void Load(TextAsset _weaponJson, TextAsset _armorJson, TextAsset _enhanceJson,
                            TextAsset _setBonusJson, TextAsset _setEffectJson,
                            TextAsset _consumableJson, TextAsset _relicJson,
                            TextAsset _resourceJson,
                            TextAsset _floorKeyJson,
                            TextAsset _enhanceMaterialJson,
                            TextAsset _sellableJson)
    {
        bLoadAttempted = true;
        if (_weaponJson == null || _armorJson == null || _enhanceJson == null
            || _setBonusJson == null || _setEffectJson == null
            || _consumableJson == null || _relicJson == null
            || _resourceJson == null
            || _floorKeyJson == null
            || _enhanceMaterialJson == null
            || _sellableJson == null)
        {
            Debug.LogError("[GameTables] 테이블 JSON 이 연결되지 않았습니다. GameCore 의 ItemDataRegistry 를 확인하세요.");
            return;
        }

        weaponRows = new Dictionary<string, WeaponRow>();
        foreach (WeaponRow row in JsonUtility.FromJson<WeaponTable>(_weaponJson.text).rows)
        {
            weaponRows[row.Id] = row;
        }

        armorRows = new Dictionary<string, ArmorRow>();
        foreach (ArmorRow row in JsonUtility.FromJson<ArmorTable>(_armorJson.text).rows)
        {
            armorRows[row.Id] = row;
        }

        // 강화 표는 단계 순서대로 배열로 둔다 (인덱스가 곧 강화 단계)
        EnhanceRow[] enhanceRows = JsonUtility.FromJson<EnhanceTable>(_enhanceJson.text).rows;
        System.Array.Sort(enhanceRows, (a, b) => a.nLevel.CompareTo(b.nLevel));
        successRates = new float[enhanceRows.Length];
        attackMultipliers = new float[enhanceRows.Length];
        speedMultipliers = new float[enhanceRows.Length];
        for (int i = 0; i < enhanceRows.Length; i++)
        {
            successRates[i] = enhanceRows[i].fSuccessRate;
            attackMultipliers[i] = enhanceRows[i].fAttackMultiplier;
            speedMultipliers[i] = enhanceRows[i].fSpeedMultiplier;
        }

        setBonusRows = JsonUtility.FromJson<SetBonusTable>(_setBonusJson.text).rows;

        setEffectValues = new Dictionary<string, float>();
        foreach (SetEffectRow row in JsonUtility.FromJson<SetEffectTable>(_setEffectJson.text).rows)
        {
            setEffectValues[row.Id] = row.fValue;
        }

        consumableRows = new Dictionary<string, ConsumableRow>();
        foreach (ConsumableRow row in JsonUtility.FromJson<ConsumableTable>(_consumableJson.text).rows)
        {
            consumableRows[row.Id] = row;
        }

        relicRows = new Dictionary<string, RelicRow>();
        foreach (RelicRow row in JsonUtility.FromJson<RelicTable>(_relicJson.text).rows)
        {
            relicRows[row.Id] = row;
        }

        resourceRows = new Dictionary<string, ResourceRow>();
        foreach (ResourceRow row in JsonUtility.FromJson<ResourceTable>(_resourceJson.text).rows)
        {
            resourceRows[row.Id] = row;
        }

        floorKeyRows = new Dictionary<string, FloorKeyRow>();
        foreach (FloorKeyRow row in JsonUtility.FromJson<FloorKeyTable>(_floorKeyJson.text).rows)
        {
            floorKeyRows[row.Id] = row;
        }

        enhanceMaterialRows = new Dictionary<string, EnhanceMaterialRow>();
        foreach (EnhanceMaterialRow row in JsonUtility.FromJson<EnhanceMaterialTable>(_enhanceMaterialJson.text).rows)
        {
            enhanceMaterialRows[row.Id] = row;
        }

        sellableRows = new Dictionary<string, SellableRow>();
        foreach (SellableRow row in JsonUtility.FromJson<SellableTable>(_sellableJson.text).rows)
        {
            sellableRows[row.Id] = row;
        }

        reportedMissing.Clear();
    }

    // 에디터에서 변환 직후 다시 읽게 한다
    public static void Unload()
    {
        weaponRows = null;
        armorRows = null;
        successRates = null;
        attackMultipliers = null;
        speedMultipliers = null;
        setBonusRows = null;
        setEffectValues = null;
        consumableRows = null;
        relicRows = null;
        resourceRows = null;
        floorKeyRows = null;
        enhanceMaterialRows = null;
        sellableRows = null;
        bLoadAttempted = false;
    }

    public static WeaponRow GetWeapon(string _id)
    {
        EnsureLoaded();
        WeaponRow row = null;
        if (weaponRows != null && weaponRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("Weapon", _id);
        return new WeaponRow();
    }

    public static ArmorRow GetArmor(string _id)
    {
        EnsureLoaded();
        ArmorRow row = null;
        if (armorRows != null && armorRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("Armor", _id);
        return new ArmorRow();
    }

    public static ConsumableRow GetConsumable(string _id)
    {
        EnsureLoaded();
        ConsumableRow row = null;
        if (consumableRows != null && consumableRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("Consumable", _id);
        return new ConsumableRow();
    }

    public static RelicRow GetRelic(string _id)
    {
        EnsureLoaded();
        RelicRow row = null;
        if (relicRows != null && relicRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("Relic", _id);
        return new RelicRow();
    }

    public static ResourceRow GetResource(string _id)
    {
        EnsureLoaded();
        ResourceRow row = null;
        if (resourceRows != null && resourceRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("Resource", _id);
        return new ResourceRow();
    }

    public static FloorKeyRow GetFloorKey(string _id)
    {
        EnsureLoaded();
        FloorKeyRow row = null;
        if (floorKeyRows != null && floorKeyRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("FloorKey", _id);
        return new FloorKeyRow();
    }

    public static EnhanceMaterialRow GetEnhanceMaterial(string _id)
    {
        EnsureLoaded();
        EnhanceMaterialRow row = null;
        if (enhanceMaterialRows != null && enhanceMaterialRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("EnhanceMaterial", _id);
        return new EnhanceMaterialRow();
    }

    public static SellableRow GetSellable(string _id)
    {
        EnsureLoaded();
        SellableRow row = null;
        if (sellableRows != null && sellableRows.TryGetValue(_id, out row) == true)
        {
            return row;
        }
        ReportMissing("Sellable", _id);
        return new SellableRow();
    }

    public static float[] EnhanceSuccessRates
    {
        get
        {
            EnsureLoaded();
            return successRates ?? new float[0];
        }
    }

    public static float[] AttackMultipliers
    {
        get
        {
            EnsureLoaded();
            return attackMultipliers ?? new float[] { 1f };
        }
    }

    public static float[] SpeedMultipliers
    {
        get
        {
            EnsureLoaded();
            return speedMultipliers ?? new float[] { 1f };
        }
    }

    // 세트 단계별 보너스 전체 (원소와 단계는 행에서 확인)
    public static SetBonusRow[] SetBonusRows
    {
        get
        {
            EnsureLoaded();
            return setBonusRows ?? new SetBonusRow[0];
        }
    }

    // 세트 특수 효과 수치 (없으면 0)
    public static float GetSetEffectValue(string _id)
    {
        EnsureLoaded();
        float fValue = 0f;
        if (setEffectValues != null && setEffectValues.TryGetValue(_id, out fValue) == true)
        {
            return fValue;
        }
        ReportMissing("SetEffect", _id);
        return 0f;
    }

    private static void EnsureLoaded()
    {
        if (weaponRows != null || bLoadAttempted == true)
        {
            return;
        }
        bLoadAttempted = true;
#if UNITY_EDITOR
        Load(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(WEAPON_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ARMOR_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ENHANCE_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(SET_BONUS_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(SET_EFFECT_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(CONSUMABLE_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(RELIC_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(RESOURCE_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(FLOOR_KEY_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ENHANCE_MATERIAL_JSON_PATH),
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(SELLABLE_JSON_PATH));
#endif
    }

    private static void ReportMissing(string _table, string _id)
    {
        if (reportedMissing.Add(_table + "/" + _id) == true)
        {
            Debug.LogError("[GameTables] " + _table + " 테이블에 '" + _id + "' 행이 없습니다. Tables/" + _table + ".xlsx 를 확인하고 다시 변환하세요.");
        }
    }
}
