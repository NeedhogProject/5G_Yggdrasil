using UnityEngine;

/// <summary>
/// 방어구 런타임 인스턴스 — ItemInstance 상속
///
/// [기획 반영]
/// - 각인은 방어구 전용 (무기에는 각인 없음)
/// - 각인 슬롯 2개, 조합에 따라 세트 효과 발동
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

    /// <summary>현재 각인된 슬롯 1 (None = 비어있음)</summary>
    public RuneElement RuneSlot1 { get; private set; } = RuneElement.None;

    /// <summary>현재 각인된 슬롯 2 (None = 비어있음)</summary>
    public RuneElement RuneSlot2 { get; private set; } = RuneElement.None;

    /// <summary>각인이 하나라도 있는지</summary>
    public bool HasRune => RuneSlot1 != RuneElement.None || RuneSlot2 != RuneElement.None;

    /// <summary>각인 슬롯이 모두 채워졌는지</summary>
    public bool IsRuneFull => RuneSlot1 != RuneElement.None && RuneSlot2 != RuneElement.None;

    // ─────────────────────── 세트 서명 캐싱 ───────────────────────

    // 각인 변경 시 서명을 다시 계산해야 하므로 캐시 무효화 처리
    private SetSignature? _cachedSignature = null;

    /// <summary>
    /// 현재 각인 기반 세트 서명 (ArmorSetManager 집계에 사용)
    /// 각인이 없으면 default(None, None) 반환
    /// </summary>
    public SetSignature SetSignature
    {
        get
        {
            if (_cachedSignature == null)
                _cachedSignature = new SetSignature(RuneSlot1, RuneSlot2);
            return _cachedSignature.Value;
        }
    }

    /// <summary>
    /// 현재 각인 기반으로 기여하는 원소 목록 반환 (중복 제거)
    /// ArmorSetManager 에서 원소별 카운트 집계에 사용
    /// </summary>
    public System.Collections.Generic.List<RuneElement> GetContributingElements()
    {
        System.Collections.Generic.List<RuneElement> result = new System.Collections.Generic.List<RuneElement>();
        if (RuneSlot1 != RuneElement.None && !result.Contains(RuneSlot1))
            result.Add(RuneSlot1);
        if (RuneSlot2 != RuneElement.None && !result.Contains(RuneSlot2))
            result.Add(RuneSlot2);
        return result;
    }

    // ─────────────────────── 생성자 ───────────────────────

    public ArmorInstance(ArmorData data) : base(data)
    {
        // ScriptableObject 에 기본 각인값이 있으면 런타임에 복사
        RuneSlot1 = data.RuneSlot1;
        RuneSlot2 = data.RuneSlot2;
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
    /// slot: 1 또는 2
    /// </summary>
    public bool SetRune(int slot, RuneElement element)
    {
        if (slot != 1 && slot != 2) return false;
        if (element == RuneElement.None) return false;

        // 같은 원소가 다른 슬롯에 이미 있으면 거부
        if (slot == 1 && RuneSlot2 == element) return false;
        if (slot == 2 && RuneSlot1 == element) return false;

        if (slot == 1) RuneSlot1 = element;
        else           RuneSlot2 = element;

        _cachedSignature = null; // 서명 캐시 무효화
        return true;
    }

    /// <summary>각인 초기화 (초기화 주문서 사용 시)</summary>
    public void ClearRunes()
    {
        RuneSlot1        = RuneElement.None;
        RuneSlot2        = RuneElement.None;
        _cachedSignature = null;
    }

    // ─────────────────────── 계산된 스탯 ───────────────────────

    /// <summary>이 방어구가 제공하는 방어력 수치</summary>
    public float DefenseBonus => ArmorData?.DefenseBonus ?? 0f;
    public float MaxHealthBonus => ArmorData?.MaxHealthBonus ?? 0f;

    /// <summary>착용 부위</summary>
    public ArmorSlot Slot => ArmorData?.ArmorSlot ?? ArmorSlot.Chest;

    public override string ToString() =>
        $"[{ArmorData?.ItemName ?? "null"}] {Slot} | 방어력+{DefenseBonus} | " +
        $"각인: {RuneSlot1}/{RuneSlot2} | 세트: {SetSignature} | 장착: {IsEquipped}";

    // ─────────────────────── 각인 색상 UI 연동 ───────────────────────

    /// <summary>
    /// 각인 슬롯 1개의 UI 표시 정보
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
    /// 슬롯 번호에 해당하는 각인 UI 정보 반환
    /// slot: 1 또는 2 / 유효하지 않은 슬롯이면 None + clear 반환
    /// </summary>
    /// <example>
    /// var info = armor.GetInscription(1);
    /// icon.color = info.Color;
    /// label.text = info.IsEmpty ? "빈 슬롯" : info.Element.ToString();
    /// </example>
    public InscriptionInfo GetInscription(int slot)
    {
        RuneElement element = slot switch
        {
            1 => RuneSlot1,
            2 => RuneSlot2,
            _ => RuneElement.None
        };
        return new InscriptionInfo(element, GetRuneColor(element));
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