// QuickSlotSystem.cs
// 체력바 옆 단축키 슬롯 (소모품 전용). 슬롯마다 소모품 1종과 단축키를 연결한다.
// 단축키를 누르면 인벤토리에서 그 소모품을 1개 사용하고, 슬롯에 보유 수량을 표시한다.
// 소모품 실제 사용/차감은 InventorySystem.UseConsumableByType 가 담당한다.
// 담당: 김보민

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class QuickSlotSystem : MonoBehaviour
{
    // 단축키 슬롯 한 칸 설정
    [System.Serializable]
    public class QuickSlot
    {
        [Tooltip("이 슬롯에 연결할 소모품 (체력 물약 / 정신력 물약 등)")]
        public ConsumableData consumable;

        [Tooltip("이 슬롯을 사용할 단축키")]
        public Key useKey = Key.Digit1;

        [Tooltip("소모품 아이콘을 표시할 Image")]
        public Image iconImage;

        [Tooltip("보유 수량을 표시할 텍스트")]
        public TMP_Text countText;
    }

    [Header("단축키 슬롯 (보통 2개)")]
    [SerializeField] private List<QuickSlot> slots = new List<QuickSlot>();

    [Header("보유 0개일 때 아이콘 흐림 정도 (0=투명, 1=불투명)")]
    [Range(0f, 1f)]
    [SerializeField] private float emptyAlpha = 0.35f;

    private void Start()
    {
        // 시작 시 아이콘/수량 1회 세팅
        for (int i = 0; i < slots.Count; i++)
        {
            SetupSlotIcon(slots[i]);
        }

        RefreshAll();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // 각 슬롯 단축키 입력 검사
        for (int i = 0; i < slots.Count; i++)
        {
            QuickSlot slot = slots[i];
            if (slot == null || slot.consumable == null)
            {
                continue;
            }

            if (Keyboard.current[slot.useKey].wasPressedThisFrame == true)
            {
                UseSlot(slot);
            }
        }

        // 보유 수량 표시 갱신 (줍거나 써서 바뀔 수 있으므로 매 프레임 갱신)
        RefreshAll();
    }

    // 슬롯 하나 사용 — 연결된 소모품을 1개 사용
    private void UseSlot(QuickSlot slot)
    {
        if (slot.consumable == null)
        {
            return;
        }
        if (InventorySystem.Instance == null)
        {
            return;
        }

        bool success = InventorySystem.Instance.UseConsumableByType(slot.consumable.ConsumableType);

        if (success == false)
        {
            // 보유 없음 또는 이미 최대치 등 — 실패 효과음
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        RefreshSlot(slot);
    }

    // 슬롯 아이콘 초기 세팅 (연결된 소모품 스프라이트)
    private void SetupSlotIcon(QuickSlot slot)
    {
        if (slot == null)
        {
            return;
        }
        if (slot.iconImage == null)
        {
            return;
        }

        if (slot.consumable != null && slot.consumable.itemIcon != null)
        {
            slot.iconImage.sprite = slot.consumable.itemIcon;
            slot.iconImage.enabled = true;
        }
        else
        {
            slot.iconImage.enabled = false;
        }
    }

    // 모든 슬롯 수량/흐림 갱신
    private void RefreshAll()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            RefreshSlot(slots[i]);
        }
    }

    // 슬롯 하나의 보유 수량 표시 + 보유 0이면 아이콘 흐리게
    private void RefreshSlot(QuickSlot slot)
    {
        if (slot == null || slot.consumable == null)
        {
            return;
        }

        int count = 0;
        if (InventorySystem.Instance != null)
        {
            count = InventorySystem.Instance.GetConsumableCount(slot.consumable.ConsumableType);
        }

        // 수량 텍스트
        if (slot.countText != null)
        {
            slot.countText.text = count.ToString();
        }

        // 보유 0이면 아이콘 흐리게, 있으면 선명하게
        if (slot.iconImage != null)
        {
            float alpha = count > 0 ? 1f : emptyAlpha;
            slot.iconImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}