using UnityEngine;

/// <summary>
/// 유물 데이터 — ScholarSystem(감정), UIItemTooltip 에서 참조
/// </summary>
[CreateAssetMenu(fileName = "NewRelic", menuName = "Yggdrasil/Items/RelicData")]
public class RelicData : ItemData
{
    [Header("유물 효과")]
    [SerializeField] private string _relicEffect = "";

    [Header("감정 여부")]
    [SerializeField] private bool _isIdentified = false;

    /// <summary>유물 효과 설명 — UIItemTooltip 참조</summary>
    public string relicEffect    => _relicEffect;

    /// <summary>감정 완료 여부 — ScholarSystem, UIItemTooltip 참조</summary>
    public bool   isIdentified
    {
        get => _isIdentified;
        set => _isIdentified = value;
    }

    /// <summary>감정 완료 시 스탯 공개 — ScholarSystem 에서 호출</summary>
    public void RevealStats()
    {
        _isIdentified = true;
        Debug.Log($"[RelicData] {itemName} 감정 완료: {_relicEffect}");
    }
}
