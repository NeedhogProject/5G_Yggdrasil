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

    private static Dictionary<string, WeaponRow> weaponRows = null;
    private static Dictionary<string, ArmorRow> armorRows = null;
    private static float[] successRates = null;
    private static float[] attackMultipliers = null;
    private static float[] speedMultipliers = null;
    private static bool bLoadAttempted = false;   // 실패 시 매 조회마다 재시도 방지

    // 없는 ID 경고는 ID 마다 1번만
    private static readonly HashSet<string> reportedMissing = new HashSet<string>();

    public static void Load(TextAsset _weaponJson, TextAsset _armorJson, TextAsset _enhanceJson)
    {
        bLoadAttempted = true;
        if (_weaponJson == null || _armorJson == null || _enhanceJson == null)
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
             UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ENHANCE_JSON_PATH));
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
