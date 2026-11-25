using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>
/// 시트 행을 JsonUtility 가 읽는 JSON 으로 변환
///
/// 시트 형식: 1행 키(행 클래스 필드 이름), 2행 설명(무시), 3행부터 데이터
/// - 키가 빈 열은 무시 (A열을 비워 둬도 됨)
/// - 키 열이 빈 행은 건너뜀
/// - 오류가 하나라도 있으면 null 을 반환하고 오류는 _errors 에 쌓는다
/// </summary>
public static class TableJsonBuilder
{
    private const int KEY_ROW = 0;
    private const int DATA_START_ROW = 2;

    public static string Build(string _tableName, List<List<string>> _rows, Type _rowType, string _keyField,
                               List<string> _outKeys, List<string> _errors)
    {
        int nErrorCountBefore = _errors.Count;
        if (_rows.Count <= KEY_ROW)
        {
            _errors.Add(_tableName + ": 시트가 비어 있습니다.");
            return null;
        }

        // 열 번호와 필드 대응
        Dictionary<int, FieldInfo> columns = new Dictionary<int, FieldInfo>();
        HashSet<string> foundKeys = new HashSet<string>();
        List<string> keyRow = _rows[KEY_ROW];
        for (int nCol = 0; nCol < keyRow.Count; nCol++)
        {
            string key = keyRow[nCol].Trim();
            if (key == "")
            {
                continue;
            }

            FieldInfo field = _rowType.GetField(key, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                _errors.Add(_tableName + ": 알 수 없는 열 '" + key + "' (" + ColumnName(nCol) + "열)");
                continue;
            }
            if (foundKeys.Add(key) == false)
            {
                _errors.Add(_tableName + ": 열 '" + key + "' 가 두 번 있습니다.");
                continue;
            }
            columns.Add(nCol, field);
        }

        foreach (FieldInfo field in _rowType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (foundKeys.Contains(field.Name) == false)
            {
                _errors.Add(_tableName + ": 열 '" + field.Name + "' 가 없습니다.");
            }
        }

        int nKeyCol = -1;
        foreach (KeyValuePair<int, FieldInfo> pair in columns)
        {
            if (pair.Value.Name == _keyField)
            {
                nKeyCol = pair.Key;
            }
        }
        if (_errors.Count > nErrorCountBefore || nKeyCol < 0)
        {
            return null;
        }

        StringBuilder json = new StringBuilder();
        json.Append("{\"rows\":[\n");
        bool bFirstRow = true;
        for (int nRow = DATA_START_ROW; nRow < _rows.Count; nRow++)
        {
            List<string> row = _rows[nRow];
            string keyValue = CellAt(row, nKeyCol).Trim();
            if (keyValue == "")
            {
                continue;
            }

            string where = _tableName + " " + (nRow + 1) + "행";
            if (_outKeys.Contains(keyValue) == true)
            {
                _errors.Add(where + ": " + _keyField + " '" + keyValue + "' 가 중복됩니다.");
                continue;
            }
            _outKeys.Add(keyValue);

            if (bFirstRow == false)
            {
                json.Append(",\n");
            }
            bFirstRow = false;

            json.Append("{");
            bool bFirstField = true;
            foreach (KeyValuePair<int, FieldInfo> pair in columns)
            {
                string literal = ToJsonLiteral(CellAt(row, pair.Key).Trim(), pair.Value.FieldType, where + " " + pair.Value.Name, _errors);
                if (bFirstField == false)
                {
                    json.Append(",");
                }
                bFirstField = false;
                json.Append("\"").Append(pair.Value.Name).Append("\":").Append(literal);
            }
            json.Append("}");
        }
        json.Append("\n]}\n");

        if (_errors.Count > nErrorCountBefore)
        {
            return null;
        }
        return json.ToString();
    }

    // 셀 문자열을 필드 타입에 맞는 JSON 값으로
    private static string ToJsonLiteral(string _text, Type _type, string _where, List<string> _errors)
    {
        if (_type == typeof(string))
        {
            return Quote(_text);
        }

        if (_type == typeof(int))
        {
            double dValue = 0;
            if (double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue) == false
                || dValue != Math.Floor(dValue))
            {
                _errors.Add(_where + ": 정수가 아닙니다 ('" + _text + "')");
                return "0";
            }
            return ((int)dValue).ToString(CultureInfo.InvariantCulture);
        }

        if (_type == typeof(float))
        {
            double dValue = 0;
            if (double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue) == false)
            {
                _errors.Add(_where + ": 숫자가 아닙니다 ('" + _text + "')");
                return "0";
            }
            return ((float)dValue).ToString("R", CultureInfo.InvariantCulture);
        }

        if (_type == typeof(bool))
        {
            string upper = _text.ToUpperInvariant();
            if (upper == "TRUE" || upper == "1")
            {
                return "true";
            }
            if (upper == "FALSE" || upper == "0")
            {
                return "false";
            }
            _errors.Add(_where + ": TRUE 또는 FALSE 여야 합니다 ('" + _text + "')");
            return "false";
        }

        if (_type.IsEnum == true)
        {
            // JsonUtility 는 enum 을 숫자로 읽는다
            foreach (string enumName in Enum.GetNames(_type))
            {
                if (string.Equals(enumName, _text, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Convert.ToInt32(Enum.Parse(_type, enumName)).ToString(CultureInfo.InvariantCulture);
                }
            }
            _errors.Add(_where + ": '" + _text + "' 는 허용 값이 아닙니다 (" + string.Join(", ", Enum.GetNames(_type)) + ")");
            return "0";
        }

        _errors.Add(_where + ": 지원하지 않는 필드 타입 " + _type.Name);
        return "null";
    }

    private static string Quote(string _text)
    {
        StringBuilder sb = new StringBuilder("\"");
        foreach (char ch in _text)
        {
            switch (ch)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (ch < 0x20)
                    {
                        sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        sb.Append("\"");
        return sb.ToString();
    }

    private static string CellAt(List<string> _row, int _nCol)
    {
        return _nCol < _row.Count ? _row[_nCol] : "";
    }

    // 0 에서 A, 26 에서 AA
    private static string ColumnName(int _nCol)
    {
        string name = "";
        int nValue = _nCol + 1;
        while (nValue > 0)
        {
            int nRem = (nValue - 1) % 26;
            name = (char)('A' + nRem) + name;
            nValue = (nValue - 1) / 26;
        }
        return name;
    }
}
