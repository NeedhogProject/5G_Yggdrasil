/*
 * ResourceRowUI.cs
 * 각인술사 우측 자원 목록의 한 줄 — 아이콘 + 원소 이름 + 보유 수량, 클릭 시 그 원소를 각인 원소로 선택
 * 각 줄에 원소(element)를 인스펙터에서 지정한다. (불/물/바람/땅/어둠)
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ResourceRowUI : MonoBehaviour, IPointerClickHandler
{
    [Header("이 줄이 나타내는 원소")]
    [SerializeField] private RuneElement element = RuneElement.Fire;

    [Header("UI References")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image selectedHighlight; // 선택 강조 (없어도 됨)

    // 이 줄의 원소 외부 읽기용
    public RuneElement Element
    {
        get { return element; }
    }

    private void Start()
    {
        // 이름/색상 자동 세팅 (인스펙터에서 비워둬도 됨)
        if (nameText != null)
        {
            nameText.text = RuneInscriptionSystem.GetElementName(element) + "의 자원";
            nameText.color = RuneInscriptionSystem.GetElementColor(element);
        }

        if (icon != null)
        {
            icon.color = RuneInscriptionSystem.GetElementColor(element);
        }

        SetSelected(false);
    }

    // 보유 수량 표시
    public void SetCount(int owned)
    {
        if (countText != null)
        {
            countText.text = "보유 : " + owned.ToString();
        }
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

    // 좌클릭 시 이 원소를 각인 원소로 선택
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (InscriptionMasterSystem.Instance != null)
        {
            InscriptionMasterSystem.Instance.SelectElement(element);
        }
    }
}