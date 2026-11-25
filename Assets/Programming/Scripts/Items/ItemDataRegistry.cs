using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 데이터 레지스트리
///
/// SaveSystem 이 저장/불러오기 할 때 아이템을 고유 ID(에셋 GUID)로 찾을 수 있도록
/// 모든 ItemData 에셋을 미리 등록해두는 싱글턴
/// - 에셋 이름을 바꿔도 GUID 는 그대로라 저장 데이터가 유지됨
/// - ItemData 에셋을 만들거나 지우거나 옮기면 ItemDataRegistryAutoCollect 가 목록을 자동 갱신
///
/// 사용법:
///   GameCore 프리팹에 부착 (목록은 자동으로 채워짐, 수동은 컴포넌트 우클릭 '모든 ItemData 자동 수집')
///   ItemDataRegistry.Instance.FindById(id) / GetId(data)
/// </summary>
public class ItemDataRegistry : MonoBehaviour
{
    // 등록 항목 (GUID 와 에셋)
    [System.Serializable]
    public class Entry
    {
        public string id;
        public ItemData data;
    }

    // ─────────────────────── 싱글턴 ───────────────────────

    public static ItemDataRegistry Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionary();
    }

    // ─────────────────────── 등록 목록 ───────────────────────

    [Header("모든 아이템 데이터 (자동 수집)")]
    [SerializeField] private List<Entry> items = new List<Entry>();

    // ─────────────────────── 내부 딕셔너리 ───────────────────────

    private Dictionary<string, ItemData> _byId = new Dictionary<string, ItemData>();
    private Dictionary<ItemData, string> _idOf = new Dictionary<ItemData, string>();

    /// <summary>목록을 딕셔너리로 변환 (Awake 시 1회 실행)</summary>
    private void BuildDictionary()
    {
        _byId.Clear();
        _idOf.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            Entry entry = items[i];
            if (entry == null || entry.data == null || string.IsNullOrEmpty(entry.id) == true)
            {
                continue;
            }
            if (_byId.ContainsKey(entry.id) == true)
            {
                continue;
            }
            _byId.Add(entry.id, entry.data);
            _idOf[entry.data] = entry.id;
        }

        Debug.Log("[ItemDataRegistry] 등록 완료: " + _byId.Count + "개");
    }

    // ─────────────────────── 검색 ───────────────────────

    /// <summary>고유 ID 로 ItemData 반환 (없으면 null + 경고) — SaveSystem 불러오기용</summary>
    public ItemData FindById(string _id)
    {
        if (string.IsNullOrEmpty(_id) == true)
        {
            return null;
        }
        if (_byId.TryGetValue(_id, out ItemData data) == true)
        {
            return data;
        }
        Debug.LogWarning("[ItemDataRegistry] 등록되지 않은 아이템 ID: " + _id);
        return null;
    }

    /// <summary>ItemData 의 고유 ID 반환 (등록 안 됐으면 빈 문자열) — SaveSystem 저장용</summary>
    public string GetId(ItemData _data)
    {
        if (_data != null && _idOf.TryGetValue(_data, out string id) == true)
        {
            return id;
        }
        Debug.LogWarning("[ItemDataRegistry] 등록되지 않은 아이템이라 저장하지 못함: " + (_data != null ? _data.name : "null"));
        return "";
    }

    // ─────────────────────── 에디터 ───────────────────────

#if UNITY_EDITOR
    /// <summary>프로젝트의 모든 ItemData 를 GUID 와 함께 수집 — ItemDataRegistryAutoCollect 에서도 호출</summary>
    [ContextMenu("모든 ItemData 자동 수집")]
    public void CollectAllItems()
    {
        items.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        // 순서를 고정해 같은 아이템 구성이면 프리팹 내용도 같게 (팀원 간 충돌 줄이기)
        System.Array.Sort(guids, System.StringComparer.Ordinal);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData data = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null)
            {
                continue;
            }

            Entry entry = new Entry();
            entry.id = guids[i];
            entry.data = data;
            items.Add(entry);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[ItemDataRegistry] 수집 완료: " + items.Count + "개");
    }

    [ContextMenu("등록 목록 출력")]
    private void PrintRegistry()
    {
        foreach (KeyValuePair<string, ItemData> pair in _byId)
        {
            Debug.Log("[ItemDataRegistry] 등록됨: " + pair.Value.name + " (" + pair.Key + ")");
        }
    }
#endif
}
