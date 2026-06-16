/*
 * BlacksmithItemUI.cs
 * 대장장이 무기 목록 카드 — 아이콘 + 무기 이름 + 강화 단계 표시, 클릭 시 선택
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BlacksmithItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text enhanceLevelText;
    [SerializeField] private Image selectedHighlight; // 선택 시 강조 (없어도 됨)

    private BlacksmithSystem _blacksmith;
    private WeaponInstance _weapon;

    // 무기 카드 초기화
    public void Setup(WeaponInstance weapon, BlacksmithSystem blacksmith)
    {
        _weapon = weapon;
        _blacksmith = blacksmith;

        if (weapon == null || weapon.Data == null)
        {
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = weapon.Data.itemIcon;
        }

        if (weaponNameText != null)
        {
            weaponNameText.text = weapon.Data.itemName;
        }

        if (enhanceLevelText != null)
        {
            enhanceLevelText.text = "+" + weapon.EnhancementLevel.ToString() + "강";
        }

        SetSelected(false);
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

    // 이 카드가 가진 무기 인스턴스 외부 읽기용
    public WeaponInstance Weapon
    {
        get { return _weapon; }
    }

    // 좌클릭 시 이 무기를 선택
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_blacksmith != null && _weapon != null)
        {
            _blacksmith.SelectWeaponFromList(this);
        }
    }
}