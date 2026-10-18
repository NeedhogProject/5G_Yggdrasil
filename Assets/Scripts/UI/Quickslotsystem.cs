// QuickSlotSystem.cs
// 체력바 옆 단축키 슬롯 (소모품 전용). 인벤토리에서 Ctrl+좌클릭으로 소모품을 등록한다.
// 등록된 슬롯의 단축키(1, 2)를 누르면 그 소모품을 1개 사용하고, 슬롯에 보유 수량을 표시한다.
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
        [Tooltip("이 슬롯에 등록된 소모품 (런타임에 Ctrl+좌클릭으로 채워짐)")]
        public ConsumableData consumable;

        [Tooltip("이 슬롯을 사용할 단축키 (왼쪽부터 Digit1, Digit2)")]
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

    public static QuickSlotSystem Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // 퀵슬롯은 비운 채로 시작하고, 인벤토리에서 Ctrl+좌클릭으로 등록한다 (선착순)
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].consumable = null;
            }
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

    // 인벤토리에서 Ctrl+좌클릭한 소모품을 빈 퀵슬롯에 등록 (선착순)
    public bool AssignConsumable(ConsumableData _data)
    {
        if (_data == null)
        {
            return false;
        }

        // 이미 등록된 소모품이면 중복 등록하지 않음
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].consumable == _data)
            {
                return false;
            }
        }

        // 빈 슬롯을 앞에서부터 찾아 등록
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].consumable == null)
            {
                slots[i].consumable = _data;
                SetupSlotIcon(slots[i]);
                RefreshSlot(slots[i]);
                AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
                return true;
            }
        }

        // 빈 슬롯이 없으면 등록하지 않음 (선착순)
        AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        return false;
    }

    // 슬롯 하나 사용 — 등록된 소모품을 1개 사용
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

    // 슬롯 아이콘 세팅 (등록된 소모품 스프라이트). 빈 슬롯은 아이콘/수량을 비움
    private void SetupSlotIcon(QuickSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.consumable != null && slot.consumable.itemIcon != null)
        {
            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = slot.consumable.itemIcon;
                slot.iconImage.enabled = true;
            }
        }
        else
        {
            if (slot.iconImage != null)
            {
                slot.iconImage.enabled = false;
            }
        }

        // 빈 슬롯이면 수량 텍스트도 비운다
        if (slot.consumable == null && slot.countText != null)
        {
            slot.countText.text = "";
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