// QuickSlotBox.cs
// 퀵슬롯 표시칸(인벤 쪽 또는 HUD 쪽)에 붙여, 우클릭하면 해당 번호의 퀵슬롯 등록을 해제한다.
// slotIndex 는 QuickSlotSystem 의 slots 리스트 순서와 맞춘다 (0, 1 ...).
// 담당: 김보민

using UnityEngine;
using UnityEngine.EventSystems;

public class QuickSlotBox : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("이 칸이 가리키는 퀵슬롯 번호 (QuickSlotSystem.slots 순서, 0부터)")]
    public int slotIndex = 0;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 우클릭일 때만 등록 해제
        if (eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        if (QuickSlotSystem.Instance == null)
        {
            return;
        }

        QuickSlotSystem.Instance.RemoveSlotAt(slotIndex);
    }
}
