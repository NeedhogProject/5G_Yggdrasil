/// <summary>
/// 프로젝트 전역 열거형 정의
/// ItemData, EquipmentData, RelicData 등에서 공통 사용
/// </summary>

/// <summary>아이템 종류 — InventorySystem, ScholarSystem, UIItemTooltip 참조</summary>
public enum ItemType
{
    Equipment,   // 장비 (무기/방어구)
    Consumable,  // 소모품 (물약, 주문서)
    Relic,       // 유물
    Resource,    // 자원 (원석)
    QuestItem    // 퀘스트 아이템 (열쇠 등)
}

/// <summary>아이템 희귀도 — UIItemTooltip 참조</summary>
public enum ItemRarity
{
    Common,    // 일반  (회색)
    Rare,      // 희귀  (파랑)
    Epic,      // 영웅  (보라)
    Legendary  // 전설  (주황)
}

/// <summary>각인 속성 — InscriptionMasterSystem, EquipmentSlotUI, UIItemTooltip 참조</summary>
public enum InscriptionType
{
    None     = 0,
    Fire     = 1,  // 불
    Water    = 2,  // 물
    Wind     = 3,  // 바람
    Earth    = 4,  // 땅
    Darkness = 5   // 어둠
}

/// <summary>장비 슬롯 종류 — EquipmentSlotUI, EquipmentData, InscriptionMasterSystem 참조</summary>
public enum EquipmentType
{
    Weapon,  // 무기
    Helmet,  // 투구
    Chest,   // 갑옷
    Legs,    // 각반
    Boots    // 장화
}
