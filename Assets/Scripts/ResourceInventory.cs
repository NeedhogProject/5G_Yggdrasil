using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 자원(원석) 전용 인벤토리 싱글턴
/// InscriptionMasterSystem 에서 각인 부여 시 자원 소모에 참조
/// </summary>
public class ResourceInventory : MonoBehaviour
{
    public static ResourceInventory Instance { get; private set; }

    /// <summary>원소별 보유 자원 수 (최대 99)</summary>
    private readonly Dictionary<InscriptionType, int> _resources
        = new Dictionary<InscriptionType, int>
        {
            { InscriptionType.Fire,     0 },
            { InscriptionType.Water,    0 },
            { InscriptionType.Wind,     0 },
            { InscriptionType.Earth,    0 },
            { InscriptionType.Darkness, 0 },
        };

    private const int MAX_STACK = 99;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>특정 원소 자원 보유 수 반환 — InscriptionMasterSystem 참조</summary>
    public int GetResourceCount(InscriptionType type)
    {
        return _resources.TryGetValue(type, out int count) ? count : 0;
    }

    /// <summary>자원 추가</summary>
    public void AddResource(InscriptionType type, int amount)
    {
        if (!_resources.ContainsKey(type) || type == InscriptionType.None) return;
        _resources[type] = Mathf.Clamp(_resources[type] + amount, 0, MAX_STACK);
    }

    /// <summary>자원 소모 — InscriptionMasterSystem 에서 각인 시 호출</summary>
    public void RemoveResource(InscriptionType type, int amount)
    {
        if (!_resources.ContainsKey(type)) return;
        _resources[type] = Mathf.Max(0, _resources[type] - amount);
    }
}
