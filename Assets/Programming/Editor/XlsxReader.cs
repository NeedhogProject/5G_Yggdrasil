using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// xlsx 첫 번째 시트 읽기 (외부 라이브러리 없이 Zip 과 XML 만 사용)
///
/// 반환: 행 목록. 각 행은 열 번호(A=0)별 셀 문자열이고 빈 셀은 ""
/// 수식 셀은 엑셀이 저장해 둔 계산 결과를 읽는다
/// </summary>
public static class XlsxReader
{
    private static readonly XNamespace NS_MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace NS_REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace NS_PKG_REL = "http://schemas.openxmlformats.org/package/2006/relationships";

    // 엑셀이 파일을 열고 있어도 읽을 수 있게 공유 모드로 연다
    public static List<List<string>> ReadFirstSheet(string _path)
    {
        using (FileStream stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            List<string> sharedStrings = ReadSharedStrings(zip);
            string sheetPath = FindFirstSheetPath(zip);
            XDocument sheet = LoadXml(zip, sheetPath);

            List<List<string>> rows = new List<List<string>>();
            foreach (XElement rowElement in sheet.Descendants(NS_MAIN + "row"))
            {
                int nRowIndex = int.Parse((string)rowElement.Attribute("r")) - 1;
                while (rows.Count <= nRowIndex)
                {
                    rows.Add(new List<string>());
                }

                List<string> row = rows[nRowIndex];
                foreach (XElement cell in rowElement.Elements(NS_MAIN + "c"))
                {
                    int nColIndex = ColumnIndex((string)cell.Attribute("r"));
                    while (row.Count <= nColIndex)
                    {
                        row.Add("");
                    }
                    row[nColIndex] = CellText(cell, sharedStrings);
                }
            }
            return rows;
        }
    }

    // 공유 문자열 표 (서식이 섞인 글자는 조각을 이어 붙인다)
    private static List<string> ReadSharedStrings(ZipArchive _zip)
    {
        List<string> result = new List<string>();
        if (_zip.GetEntry("xl/sharedStrings.xml") == null)
        {
            return result;
        }

        XDocument doc = LoadXml(_zip, "xl/sharedStrings.xml");
        foreach (XElement si in doc.Root.Elements(NS_MAIN + "si"))
        {
            result.Add(string.Concat(si.Descendants(NS_MAIN + "t").Select(t => t.Value)));
        }
        return result;
    }

    // workbook.xml 의 첫 시트가 가리키는 파일 경로
    private static string FindFirstSheetPath(ZipArchive _zip)
    {
        XDocument workbook = LoadXml(_zip, "xl/workbook.xml");
        XElement firstSheet = workbook.Descendants(NS_MAIN + "sheet").First();
        string relId = (string)firstSheet.Attribute(NS_REL + "id");

        XDocument rels = LoadXml(_zip, "xl/_rels/workbook.xml.rels");
        XElement rel = rels.Root.Elements(NS_PKG_REL + "Relationship").First(r => (string)r.Attribute("Id") == relId);
        string target = (string)rel.Attribute("Target");

        // 상대 경로와 절대 경로 둘 다 나올 수 있다
        if (target.StartsWith("/") == true)
        {
            return target.TrimStart('/');
        }
        return "xl/" + target;
    }

    private static string CellText(XElement _cell, List<string> _sharedStrings)
    {
        string type = (string)_cell.Attribute("t");
        if (type == "inlineStr")
        {
            return string.Concat(_cell.Descendants(NS_MAIN + "t").Select(t => t.Value));
        }

        XElement valueElement = _cell.Element(NS_MAIN + "v");
        if (valueElement == null)
        {
            return "";
        }

        string value = valueElement.Value;
        if (type == "s")
        {
            return _sharedStrings[int.Parse(value)];
        }
        if (type == "b")
        {
            return value == "1" ? "TRUE" : "FALSE";
        }
        return value;
    }

    // "AB12" 에서 열 번호 (A=0)
    private static int ColumnIndex(string _cellRef)
    {
        int nIndex = 0;
        foreach (char ch in _cellRef)
        {
            if (ch < 'A' || ch > 'Z')
            {
                break;
            }
            nIndex = nIndex * 26 + (ch - 'A' + 1);
        }
        return nIndex - 1;
    }

    private static XDocument LoadXml(ZipArchive _zip, string _entryPath)
    {
        using (Stream entryStream = _zip.GetEntry(_entryPath).Open())
        {
            return XDocument.Load(entryStream);
        }
    }
}
