using UnityEngine;

/// <summary>
/// 모든 아이템의 기반이 되는 ScriptableObject
/// 이름, 아이콘, 3D 프리팹, 인벤토리 칸 수(가로×세로) 포함
///
/// [프로퍼티 규칙]
/// - PascalCase : ItemInstance, LootTable 등 코어 시스템에서 사용
/// - camelCase  : InventorySystem, UIItemTooltip 등 UI/NPC 시스템에서 사용
///   → 양쪽 모두 동일한 값을 반환하는 별칭 프로퍼티로 제공
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Yggdrasil/Items/ItemData")]
public class ItemData : ScriptableObject
{
    // ───────────────────────────── Inspector 필드 ─────────────────────────────

    [Header("기본 정보")]
    [SerializeField] private string _itemName = "아이템";

    [TextArea(2, 4)]
    [SerializeField] private string _description = "";

    [Header("아이템 분류")]
    [SerializeField] private ItemType   _itemType  = ItemType.Equipment;
    [SerializeField] private ItemRarity _rarity    = ItemRarity.Common;

    [Header("비주얼")]
    [SerializeField] private Sprite     _icon    = null;
    [SerializeField] private GameObject _prefab3D = null;

    [Header("인벤토리 크기 (디아블로 스타일 격자)")]
    [SerializeField] [Range(1, 4)] private int _inventoryWidth  = 1;
    [SerializeField] [Range(1, 4)] private int _inventoryHeight = 1;

    [Header("가격")]
    [SerializeField] private int _buyPrice  = 0;
    [SerializeField] private int _sellPrice = 0;

    [Header("기타 속성")]
    [SerializeField] private bool _isDroppable = true;
    [SerializeField] private bool _isStackable = false;
    [SerializeField] [Range(1, 99)] private int _maxStack = 1;

    // ───────────────────────────── PascalCase 프로퍼티 (코어 시스템용) ─────────────────────────────

    /// <summary>아이템 표시 이름</summary>
    public string     ItemName       => _itemName;

    /// <summary>아이템 설명</summary>
    public string     Description    => _description;

    /// <summary>아이템 종류</summary>
    public ItemType   ItemType       => _itemType;

    /// <summary>아이템 희귀도</summary>
    public ItemRarity Rarity         => _rarity;

    /// <summary>UI 아이콘 스프라이트</summary>
    public Sprite     Icon           => _icon;

    /// <summary>월드/프리뷰용 3D 프리팹</summary>
    public GameObject Prefab3D       => _prefab3D;

    /// <summary>인벤토리 가로 칸 수 (1~4)</summary>
    public int        InventoryWidth  => _inventoryWidth;

    /// <summary>인벤토리 세로 칸 수 (1~4)</summary>
    public int        InventoryHeight => _inventoryHeight;

    /// <summary>인벤토리 점유 총 칸 수</summary>
    public int        InventorySize   => _inventoryWidth * _inventoryHeight;

    /// <summary>인벤토리 칸 크기 (Vector2Int)</summary>
    public Vector2Int ItemSize        => new Vector2Int(_inventoryWidth, _inventoryHeight);

    public int  BuyPrice    => _buyPrice;
    public int  SellPrice   => _sellPrice;
    public bool IsDroppable => _isDroppable;
    public bool IsStackable => _isStackable;
    public int  MaxStack    => _isStackable ? _maxStack : 1;

    // ───────────────────────────── camelCase 별칭 (UI/NPC 시스템 호환용) ─────────────────────────────

    /// <summary>itemName — InventorySystem, UIItemTooltip 등 UI 스크립트 호환용</summary>
    public string     itemName        => _itemName;

    /// <summary>itemDescription — UIItemTooltip, ScholarSystem 호환용</summary>
    public string     itemDescription => _description;

    /// <summary>itemType — InventorySystem, ScholarSystem 호환용</summary>
    public ItemType   itemType        => _itemType;

    /// <summary>rarity — UIItemTooltip 호환용</summary>
    public ItemRarity rarity          => _rarity;

    /// <summary>itemIcon — InventorySlot, EquipmentSlotUI, UIItemTooltip 호환용</summary>
    public Sprite     itemIcon        => _icon;

    /// <summary>itemPrefab — InventorySystem.DropItem() 호환용</summary>
    public GameObject itemPrefab      => _prefab3D;

    /// <summary>itemSize — InventorySlot.CanPlaceItem() 호환용</summary>
    public Vector2Int itemSize        => new Vector2Int(_inventoryWidth, _inventoryHeight);

    /// <summary>basePrice — ShopSystem, BlacksmithSystem 호환용 (구매가 기준)</summary>
    public int        basePrice       => _buyPrice;

    // ───────────────────────────── 가상 메서드 ─────────────────────────────

    /// <summary>
    /// 소모품 사용 처리 — InventorySystem.UseItem() 에서 호출
    /// ConsumableData 등 하위 클래스에서 override
    /// </summary>
    public virtual void UseItem()
    {
        UnityEngine.Debug.LogWarning($"[{_itemName}] UseItem() 미구현 — 하위 클래스에서 override 필요");
    }

    // ───────────────────────────── 유효성 검사 ─────────────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!_isStackable) _maxStack = 1;
        if (_buyPrice  < 0) _buyPrice  = 0;
        if (_sellPrice < 0) _sellPrice = 0;
        if (_sellPrice > _buyPrice && _buyPrice > 0)
            UnityEngine.Debug.LogWarning(
                $"[{_itemName}] 판매가({_sellPrice})가 구매가({_buyPrice})보다 높습니다.");
    }
#endif
}
