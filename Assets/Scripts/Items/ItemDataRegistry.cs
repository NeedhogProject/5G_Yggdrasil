using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 데이터 레지스트리
///
/// SaveSystem 이 역직렬화할 때 에셋 이름으로 ItemData 를 찾을 수 있도록
/// Inspector 에서 모든 ScriptableObject 를 미리 등록해두는 싱글턴
///
/// 사용법:
///   ItemDataRegistry.Instance.Find("에셋이름") 으로 ItemData 반환
/// </summary>
public class ItemDataRegistry : MonoBehaviour
{
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

    // ─────────────────────── Inspector 등록 목록 ───────────────────────

    [Header("무기 데이터 목록")]
    [SerializeField] private List<WeaponData> weapons = new List<WeaponData>();

    [Header("방어구 데이터 목록")]
    [SerializeField] private List<ArmorData> armors = new List<ArmorData>();

    [Header("소모품 데이터 목록")]
    [SerializeField] private List<ConsumableData> consumables = new List<ConsumableData>();

    [Header("자원 데이터 목록")]
    [SerializeField] private List<ResourceData> resources = new List<ResourceData>();

    // ─────────────────────── 내부 딕셔너리 ───────────────────────

    private Dictionary<string, ItemData> _registry = new Dictionary<string, ItemData>();

    /// <summary>Inspector 목록을 딕셔너리로 변환 (Awake 시 1회 실행)</summary>
    private void BuildDictionary()
    {
        _registry.Clear();

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null)
            {
                continue;
            }

            if (_registry.ContainsKey(weapons[i].name) == false)
            {
                _registry.Add(weapons[i].name, weapons[i]);
            }
        }

        for (int i = 0; i < armors.Count; i++)
        {
            if (armors[i] == null)
            {
                continue;
            }

            if (_registry.ContainsKey(armors[i].name) == false)
            {
                _registry.Add(armors[i].name, armors[i]);
            }
        }

        for (int i = 0; i < consumables.Count; i++)
        {
            if (consumables[i] == null)
            {
                continue;
            }

            if (_registry.ContainsKey(consumables[i].name) == false)
            {
                _registry.Add(consumables[i].name, consumables[i]);
            }
        }

        for (int i = 0; i < resources.Count; i++)
        {
            if (resources[i] == null)
            {
                continue;
            }

            if (_registry.ContainsKey(resources[i].name) == false)
            {
                _registry.Add(resources[i].name, resources[i]);
            }
        }

        Debug.Log("[ItemDataRegistry] 등록 완료: " + _registry.Count + "개");
    }

    // ─────────────────────── 검색 ───────────────────────

    /// <summary>
    /// 에셋 이름으로 ItemData 반환
    /// 없으면 null 반환 후 경고 로그 출력
    /// </summary>
    public ItemData Find(string assetName)
    {
        if (string.IsNullOrEmpty(assetName) == true)
        {
            return null;
        }

        if (_registry.ContainsKey(assetName) == true)
        {
            return _registry[assetName];
        }

        Debug.LogWarning("[ItemDataRegistry] 등록되지 않은 아이템: " + assetName);
        return null;
    }

    /// <summary>WeaponData 로 캐스팅해서 반환. 아니면 null</summary>
    public WeaponData FindWeapon(string assetName)
    {
        ItemData data = Find(assetName);
        return data as WeaponData;
    }

    /// <summary>ArmorData 로 캐스팅해서 반환. 아니면 null</summary>
    public ArmorData FindArmor(string assetName)
    {
        ItemData data = Find(assetName);
        return data as ArmorData;
    }

    // ─────────────────────── 에디터 테스트 ───────────────────────

#if UNITY_EDITOR
    [ContextMenu("등록 목록 출력")]
    private void PrintRegistry()
    {
        foreach (string key in _registry.Keys)
        {
            Debug.Log("[ItemDataRegistry] 등록됨: " + key);
        }
    }
#endif
}