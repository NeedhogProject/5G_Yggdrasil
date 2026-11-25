using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기획 테이블 변환 메뉴 (Tools > 테이블 변환)
///
/// 레포 루트 Tables/*.xlsx 를 읽어 Assets/Planning/*.json 으로 저장한다
/// - 오류가 하나라도 있으면 어떤 JSON 도 바꾸지 않고 오류 목록을 보여준다
/// - 무기/방어구는 테이블 ID 와 SO 에셋 이름이 1:1 인지 대조한다
/// - 변환 후 엑셀과 JSON 을 같이 커밋해야 한다
/// </summary>
public static class TableConverterMenu
{
    private const int MAX_ERRORS_IN_DIALOG = 15;

    private class TableDef
    {
        public string Name;         // 엑셀/JSON 파일 이름
        public Type RowType;
        public string KeyField;
        public string JsonPath;
        public Type AssetType;      // ID 를 대조할 SO 타입 (없으면 null)
    }

    private static readonly TableDef[] TABLES =
    {
        new TableDef { Name = "Weapon", RowType = typeof(WeaponRow), KeyField = "Id", JsonPath = GameTables.WEAPON_JSON_PATH, AssetType = typeof(WeaponData) },
        new TableDef { Name = "Armor", RowType = typeof(ArmorRow), KeyField = "Id", JsonPath = GameTables.ARMOR_JSON_PATH, AssetType = typeof(ArmorData) },
        new TableDef { Name = "Enhance", RowType = typeof(EnhanceRow), KeyField = "nLevel", JsonPath = GameTables.ENHANCE_JSON_PATH, AssetType = null },
    };

    [MenuItem("Tools/테이블 변환")]
    public static void ConvertAll()
    {
        string tablesDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tables"));
        List<string> errors = new List<string>();
        Dictionary<TableDef, string> results = new Dictionary<TableDef, string>();

        foreach (TableDef table in TABLES)
        {
            string xlsxPath = Path.Combine(tablesDir, table.Name + ".xlsx");
            if (File.Exists(xlsxPath) == false)
            {
                errors.Add(table.Name + ": 파일이 없습니다 (" + xlsxPath + ")");
                continue;
            }

            List<List<string>> rows = null;
            try
            {
                rows = XlsxReader.ReadFirstSheet(xlsxPath);
            }
            catch (Exception e)
            {
                errors.Add(table.Name + ": 엑셀 파일을 읽지 못했습니다. 다른 이름으로 저장된 파일인지 확인하세요. (" + e.Message + ")");
                continue;
            }

            List<string> keys = new List<string>();
            string json = TableJsonBuilder.Build(table.Name, rows, table.RowType, table.KeyField, keys, errors);
            if (json == null)
            {
                continue;
            }
            if (table.AssetType != null)
            {
                CheckIdsMatchAssets(table, keys, errors);
            }
            results[table] = json;
        }

        if (errors.Count > 0)
        {
            ShowErrors(errors);
            return;
        }

        foreach (KeyValuePair<TableDef, string> pair in results)
        {
            File.WriteAllText(Path.GetFullPath(pair.Key.JsonPath), pair.Value, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(pair.Key.JsonPath);
        }
        GameTables.Unload();

        Debug.Log("[테이블 변환] 완료: " + string.Join(", ", Array.ConvertAll(TABLES, t => t.Name)));
        EditorUtility.DisplayDialog("테이블 변환", "변환 완료.\n\nTables 폴더의 엑셀과 Assets/Planning 의 JSON 을 같이 커밋하세요.", "확인");
    }

    // 테이블 ID 와 SO 에셋 이름이 서로 빠짐없이 대응하는지
    private static void CheckIdsMatchAssets(TableDef _table, List<string> _keys, List<string> _errors)
    {
        HashSet<string> assetNames = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + _table.AssetType.Name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == _table.AssetType)
            {
                assetNames.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        foreach (string key in _keys)
        {
            if (assetNames.Contains(key) == false)
            {
                _errors.Add(_table.Name + ": ID '" + key + "' 에 해당하는 " + _table.AssetType.Name + " 에셋이 없습니다.");
            }
        }
        foreach (string assetName in assetNames)
        {
            if (_keys.Contains(assetName) == false)
            {
                _errors.Add(_table.Name + ": 에셋 '" + assetName + "' 의 행이 테이블에 없습니다.");
            }
        }
    }

    private static void ShowErrors(List<string> _errors)
    {
        foreach (string error in _errors)
        {
            Debug.LogError("[테이블 변환] " + error);
        }

        List<string> shown = _errors.Count > MAX_ERRORS_IN_DIALOG ? _errors.GetRange(0, MAX_ERRORS_IN_DIALOG) : _errors;
        string message = string.Join("\n", shown);
        if (_errors.Count > MAX_ERRORS_IN_DIALOG)
        {
            message += "\n\n외 " + (_errors.Count - MAX_ERRORS_IN_DIALOG) + "개 (Console 창 참고)";
        }
        EditorUtility.DisplayDialog("테이블 변환 실패 (파일은 바뀌지 않았습니다)", message, "확인");
    }
}
