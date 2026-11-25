/*
 * InscriptionItemUI.cs
 * 각인술사 방어구 목록 카드 — 아이콘 + 방어구 이름 + 현재 각인 상태 표시, 클릭 시 선택
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InscriptionItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text armorNameText;
    [SerializeField] private TMP_Text runeStateText;   // 현재 각인 상태 (예: "불", "각인 없음")
    [SerializeField] private Image selectedHighlight;  // 선택 시 강조 (없어도 됨)

    private InscriptionMasterSystem _master;
    private ArmorInstance _armor;

    // 방어구 카드 초기화
    public void Setup(ArmorInstance armor, InscriptionMasterSystem master)
    {
        _armor = armor;
        _master = master;

        if (armor == null || armor.Data == null)
        {
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = armor.Data.itemIcon;
        }

        if (armorNameText != null)
        {
            armorNameText.text = armor.Data.itemName;
        }

        if (runeStateText != null)
        {
            runeStateText.text = BuildRuneStateText(armor);
        }

        SetSelected(false);
    }

    // 현재 각인 상태를 문자열로 만든다. (부위당 1개)
    private string BuildRuneStateText(ArmorInstance armor)
    {
        if (armor.HasRune == false)
        {
            return "각인 없음";
        }

        return RuneInscriptionSystem.GetElementName(armor.RuneSlot1);
    }

    // 선택 강조 표시
    public void SetSelected(bool selected)
    {
        if (selectedHighlight == null)
        {
            return;
        }
        selectedHighlight.gameObject.SetActive(selected);
    }

    // 이 카드가 가진 방어구 인스턴스 외부 읽기용
    public ArmorInstance Armor
    {
        get { return _armor; }
    }

    // 좌클릭 시 이 방어구를 선택
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_master != null && _armor != null)
        {
            _master.SelectArmorFromList(this);
        }
    }
}