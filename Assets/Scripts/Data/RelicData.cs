using UnityEngine;

/// <summary>
/// 유물 데이터 — ScholarSystem(감정), UIItemTooltip 에서 참조
/// 감정 여부는 유물마다 달라 RelicInstance 에 저장
/// </summary>
[CreateAssetMenu(fileName = "NewRelic", menuName = "Yggdrasil/Items/RelicData")]
public class RelicData : ItemData
{
    [Header("유물 효과")]
    [SerializeField] private string _relicEffect = "";

    [Header("유물 옵션 (None 이면 판매용)")]
    [SerializeField] private RelicOptionType _optionType = RelicOptionType.None;
    [Tooltip("HP, 정신력, 공격력은 고정값 / 달란 증가는 비율 (0.05 = 5%)")]
    [SerializeField] private float _optionValue = 0f;

    /// <summary>유물 효과 설명 — UIItemTooltip 참조</summary>
    public string relicEffect    => _relicEffect;

    public RelicOptionType OptionType => _optionType;
    public float OptionValue => _optionValue;

    // 장착용 유물인지 (옵션 None 은 판매용)
    public bool IsEquippable => _optionType != RelicOptionType.None;
}
