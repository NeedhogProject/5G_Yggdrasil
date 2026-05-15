using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 집 시스템 — 창고 + 플레이어 상태 회복
///
/// [기획 반영]
/// - 창고: 인벤토리에 넣기 애매한 아이템 보관, 격자 방식, 크기 조정 가능
/// - 잠 (Sleep)    : 체력 전체 회복
/// - 휴식 (Rest)   : 정신력 전체 회복
/// - 추후 버프 효과 등 추가 가능 (RecoveryType 열거형 확장)
/// - 마을에서만 사용 가능
///
/// [컴포넌트 설정]
/// - 집 오브젝트에 부착
/// - playerStats 에 PlayerStats 연결
/// </summary>
public class HouseSystem : MonoBehaviour
{
    // ─────────────────────── 회복 종류 ───────────────────────

    public enum RecoveryType
    {
        Sleep,  // 잠  — 체력 전체 회복
        Rest,   // 휴식 — 정신력 전체 회복

        // ── 추후 추가 예시 ──────────────────
        // Meditate,   // 명상 — 체력+정신력 소폭 회복
        // Sauna,      // 사우나 — 버프 효과
    }

    // ─────────────────────── 참조 ───────────────────────

    [Header("참조")]
    [SerializeField] private PlayerStats playerStats;

    // ─────────────────────── 창고 설정 ───────────────────────

    [Header("창고 설정")]
    [Tooltip("창고 가로 칸 수 (나중에 조정 가능)")]
    [SerializeField] private int storageWidth  = 8;

    [Tooltip("창고 세로 칸 수 (나중에 조정 가능)")]
    [SerializeField] private int storageHeight = 8;

    // ─────────────────────── 회복 설정 ───────────────────────

    [Header("잠 — 체력 회복")]
    [Tooltip("잠을 잘 때 회복되는 체력량. 100 = 전체 회복")]
    [SerializeField] private float sleepHealthRestore  = 100f;

    [Header("휴식 — 정신력 회복")]
    [Tooltip("휴식 시 회복되는 정신력량. 100 = 전체 회복")]
    [SerializeField] private float restMentalRestore   = 100f;

    [Header("회복 비용 (미정 — 0이면 무료)")]
    [SerializeField] private int sleepCost = 0;
    [SerializeField] private int restCost  = 0;

    // ─────────────────────── 창고 런타임 ───────────────────────

    /// <summary>창고에 보관 중인 아이템 목록</summary>
    private readonly List<ItemInstance> _storageItems = new List<ItemInstance>();

    /// <summary>창고 최대 칸 수</summary>
    public int StorageCapacity => storageWidth * storageHeight;

    /// <summary>현재 사용 중인 칸 수</summary>
    public int UsedSlots
    {
        get
        {
            int used = 0;
            foreach (ItemInstance item in _storageItems)
                used += item.Data.InventoryWidth * item.Data.InventoryHeight;
            return used;
        }
    }

    /// <summary>창고가 가득 찼는지</summary>
    public bool IsFull => UsedSlots >= StorageCapacity;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    // ─────────────────────── 회복 시스템 ───────────────────────

    /// <summary>
    /// 회복 시도 — UI 에서 호출
    /// 반환값: 성공 여부
    /// </summary>
    public bool TryRecover(RecoveryType type)
    {
        // 마을에서만 사용 가능
        if (GameManager.Instance != null && !GameManager.Instance.IsInTown)
        {
            Debug.Log("[HouseSystem] 마을에서만 사용 가능합니다.");
            return false;
        }

        // 비용 확인 (코인 시스템 완성 후 연동)
        int cost = GetRecoveryCost(type);
        if (cost > 0)
        {
            // TODO: 코인 시스템 연동
            Debug.Log($"[HouseSystem] {type} 비용: {cost} 코인 (코인 시스템 연동 후 처리)");
        }

        switch (type)
        {
            case RecoveryType.Sleep: return DoSleep();
            case RecoveryType.Rest:  return DoRest();
            default:
                Debug.LogWarning($"[HouseSystem] 처리되지 않은 RecoveryType: {type}");
                return false;
        }
    }

    private bool DoSleep()
    {
        if (playerStats == null) return false;
        if (playerStats.Health >= 100f)
        {
            Debug.Log("[HouseSystem] 체력이 이미 최대치입니다.");
            return false;
        }
        playerStats.ModifyHealth(sleepHealthRestore);
        Debug.Log($"[HouseSystem] 잠 — 체력 +{sleepHealthRestore} 회복");
        return true;
    }

    private bool DoRest()
    {
        if (playerStats == null) return false;
        if (playerStats.Mental >= 100f)
        {
            Debug.Log("[HouseSystem] 정신력이 이미 최대치입니다.");
            return false;
        }
        playerStats.ModifyMental(restMentalRestore);
        Debug.Log($"[HouseSystem] 휴식 — 정신력 +{restMentalRestore} 회복");
        return true;
    }

    private int GetRecoveryCost(RecoveryType type) => type switch
    {
        RecoveryType.Sleep => sleepCost,
        RecoveryType.Rest  => restCost,
        _                  => 0
    };

    // ─────────────────────── 창고 시스템 ───────────────────────

    /// <summary>
    /// 창고에 아이템 추가 — UI 드래그앤드롭에서 호출
    /// 반환값: 성공 여부
    /// </summary>
    public bool AddToStorage(ItemInstance item)
    {
        if (item == null) return false;

        int itemSize = item.Data.InventoryWidth * item.Data.InventoryHeight;
        if (UsedSlots + itemSize > StorageCapacity)
        {
            Debug.Log("[HouseSystem] 창고가 가득 찼습니다.");
            return false;
        }

        _storageItems.Add(item);
        Debug.Log($"[HouseSystem] 창고 보관: {item.Data.ItemName} " +
                  $"(사용 {UsedSlots}/{StorageCapacity}칸)");
        return true;
    }

    /// <summary>
    /// 창고에서 아이템 꺼내기 — UI 드래그앤드롭에서 호출
    /// 반환값: 꺼낸 아이템 (없으면 null)
    /// </summary>
    public ItemInstance TakeFromStorage(ItemInstance item)
    {
        if (_storageItems.Remove(item) == false) return null;
        Debug.Log($"[HouseSystem] 창고에서 꺼냄: {item.Data.ItemName}");
        return item;
    }

    /// <summary>창고 아이템 목록 반환 (UI 표시용)</summary>
    public IReadOnlyList<ItemInstance> GetStorageItems() => _storageItems.AsReadOnly();

    /// <summary>창고 크기 변경 — 추후 업그레이드 시스템 연동용</summary>
    public void ResizeStorage(int newWidth, int newHeight)
    {
        storageWidth  = Mathf.Max(1, newWidth);
        storageHeight = Mathf.Max(1, newHeight);
        Debug.Log($"[HouseSystem] 창고 크기 변경: {storageWidth}x{storageHeight}");
    }

    // ─────────────────────── 저장/불러오기 ───────────────────────
    // SaveSystem 완성 후 연동

    /// <summary>창고 데이터 저장용 직렬화 (SaveSystem 연동 예정)</summary>
    public List<ItemInstance> GetStorageForSave() => new List<ItemInstance>(_storageItems);

    /// <summary>창고 데이터 불러오기 (SaveSystem 연동 예정)</summary>
    public void LoadStorage(List<ItemInstance> items)
    {
        _storageItems.Clear();
        if (items != null) _storageItems.AddRange(items);
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: 잠 (체력 회복)")]
    private void TestSleep() => TryRecover(RecoveryType.Sleep);

    [ContextMenu("테스트: 휴식 (정신력 회복)")]
    private void TestRest()  => TryRecover(RecoveryType.Rest);
#endif
}
