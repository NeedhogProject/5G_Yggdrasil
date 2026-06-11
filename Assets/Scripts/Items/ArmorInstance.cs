using UnityEngine;

/// <summary>
/// 방어구 런타임 인스턴스 — ItemInstance 상속
///
/// [기획 반영]
/// - 각인은 방어구 전용 (무기에는 각인 없음)
/// - 각인 슬롯은 부위당 최대 1개
/// - 초기화 주문서로 각인 리셋 가능
/// </summary>
[System.Serializable]
public class ArmorInstance : ItemInstance
{
    // ─────────────────────── 원본 방어구 데이터 ───────────────────────

    /// <summary>ArmorData 로 캐스팅된 원본 데이터</summary>
    public ArmorData ArmorData => Data as ArmorData;

    // ─────────────────────── 착용 상태 ───────────────────────

    /// <summary>현재 장착 중인지</summary>
    public bool IsEquipped { get; private set; } = false;

    // ─────────────────────── 각인 런타임 상태 ───────────────────────

    /// <summary>현재 각인된 원소 (None = 비어있음). 부위당 최대 1개</summary>
    public RuneElement RuneSlot1 { get; private set; } = RuneElement.None;

    /// <summary>각인이 있는지</summary>
    public bool HasRune => RuneSlot1 != RuneElement.None;

    /// <summary>각인 슬롯이 채워졌는지 (1슬롯이므로 HasRune 과 동일)</summary>
    public bool IsRuneFull => RuneSlot1 != RuneElement.None;

    /// <summary>
    /// 현재 각인 기반으로 기여하는 원소 목록 반환
    /// ArmorSetManager 에서 원소별 카운트 집계에 사용
    /// </summary>
    public System.Collections.Generic.List<RuneElement> GetContributingElements()
    {
        System.Collections.Generic.List<RuneElement> result = new System.Collections.Generic.List<RuneElement>();
        if (RuneSlot1 != RuneElement.None)
            result.Add(RuneSlot1);
        return result;
    }

    // ─────────────────────── 생성자 ───────────────────────

    public ArmorInstance(ArmorData data) : base(data)
    {
        // ScriptableObject 에 기본 각인값이 있으면 런타임에 복사
        RuneSlot1 = data.RuneSlot1;
    }

    // ─────────────────────── 착용 / 해제 ───────────────────────

    /// <summary>
    /// 장착 처리 (PlayerEquipment 에서 호출)
    /// PlayerStats.AddEquipmentDefense() 연동은 PlayerEquipment 에서 담당
    /// </summary>
    public bool Equip()
    {
        if (IsEquipped) return false;
        IsEquipped = true;
        return true;
    }

    /// <summary>해제 처리 (PlayerEquipment 에서 호출)</summary>
    public bool Unequip()
    {
        if (IsEquipped == false) return false;
        IsEquipped = false;
        return true;
    }

    // ─────────────────────── 각인 메서드 ───────────────────────

    /// <summary>
    /// 각인 설정 (각인술사 시스템에서 호출)
    /// 부위당 1개 — 이미 각인이 있으면 거부 (초기화 후 다시 부여해야 함)
    /// </summary>
    public bool SetRune(RuneElement element)
    {
        if (element == RuneElement.None) return false;
        if (RuneSlot1 != RuneElement.None) return false;

        RuneSlot1 = element;
        return true;
    }

    /// <summary>각인 초기화 (초기화 주문서 사용 시)</summary>
    public void ClearRunes()
    {
        RuneSlot1 = RuneElement.None;
    }

    // ─────────────────────── 계산된 스탯 ───────────────────────

    /// <summary>이 방어구가 제공하는 방어력 수치</summary>
    public float DefenseBonus => ArmorData?.DefenseBonus ?? 0f;
    public float MaxHealthBonus => ArmorData?.MaxHealthBonus ?? 0f;

    /// <summary>착용 부위</summary>
    public ArmorSlot Slot => ArmorData?.ArmorSlot ?? ArmorSlot.Chest;

    public override string ToString() =>
        $"[{ArmorData?.ItemName ?? "null"}] {Slot} | 방어력+{DefenseBonus} | " +
        $"각인: {RuneSlot1} | 장착: {IsEquipped}";

    // ─────────────────────── 각인 색상 UI 연동 ───────────────────────

    /// <summary>
    /// 각인 슬롯의 UI 표시 정보
    /// </summary>
    public readonly struct InscriptionInfo
    {
        /// <summary>각인 원소 (None = 빈 슬롯)</summary>
        public readonly RuneElement Element;

        /// <summary>UI 표시용 색상</summary>
        public readonly Color Color;

        /// <summary>슬롯이 비어있는지</summary>
        public bool IsEmpty => Element == RuneElement.None;

        public InscriptionInfo(RuneElement element, Color color)
        {
            Element = element;
            Color = color;
        }
    }

    /// <summary>
    /// 각인 UI 정보 반환 (부위당 1슬롯)
    /// </summary>
    /// <example>
    /// var info = armor.GetInscription();
    /// icon.color = info.Color;
    /// label.text = info.IsEmpty ? "빈 슬롯" : info.Element.ToString();
    /// </example>
    public InscriptionInfo GetInscription()
    {
        return new InscriptionInfo(RuneSlot1, GetRuneColor(RuneSlot1));
    }

    /// <summary>
    /// RuneElement → UI 색상 매핑
    /// 신규 원소 추가 시 여기에만 등록하면 됩니다
    /// </summary>
    private static Color GetRuneColor(RuneElement element) => element switch
    {
        RuneElement.Fire => new Color(1.00f, 0.35f, 0.10f), // 주황-적
        RuneElement.Water => new Color(0.20f, 0.55f, 1.00f), // 하늘
        RuneElement.Earth => new Color(0.50f, 0.75f, 0.20f), // 초록-황
        RuneElement.Wind => new Color(0.65f, 0.95f, 0.75f), // 민트
        RuneElement.None => Color.gray,
        _ => Color.white
    };
}
