// QuickSlotSystem.cs
// 체력바 옆 단축키 슬롯 (소모품 전용). 인벤토리에서 Ctrl+좌클릭으로 소모품을 등록한다.
// 등록된 슬롯의 단축키(1, 2)를 누르면 그 소모품을 1개 사용하고, 슬롯에 보유 수량을 표시한다.
// 소모품 실제 사용/차감은 InventorySystem.UseConsumableByData 가 담당한다.
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

        [Tooltip("소모품 아이콘을 표시할 Image (HUD 쪽)")]
        public Image iconImage;

        [Tooltip("보유 수량을 표시할 텍스트 (HUD 쪽)")]
        public TMP_Text countText;

        [Tooltip("인벤토리 쪽 아이콘 Image (선택 — 인벤 패널에 둔 같은 칸)")]
        public Image invIconImage;

        [Tooltip("인벤토리 쪽 수량 텍스트 (선택)")]
        public TMP_Text invCountText;
    }

    [Header("단축키 슬롯 (보통 2개)")]
    [SerializeField] private List<QuickSlot> slots = new List<QuickSlot>();

    [Header("보유 0개일 때 아이콘 흐림 정도 (0=투명, 1=불투명)")]
    [Range(0f, 1f)]
    [SerializeField] private float emptyAlpha = 0.35f;

    // 씬이 바뀌면 QuickSlotSystem 이 새로 생성되므로, 등록된 소모품을 static 으로 보관해 복원한다 (슬롯 순서대로)
    private static readonly List<ConsumableData> _savedConsumables = new List<ConsumableData>();

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
        // 씬이 바뀌어도 등록이 유지되도록, 이전에 보관해 둔 소모품을 복원한다 (없으면 빈 칸)
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].consumable = (i < _savedConsumables.Count) ? _savedConsumables[i] : null;
            }
            EnsureIconImage(slots[i]);
            SetupSlotIcon(slots[i]);
        }

        RefreshAll();
    }

    // iconImage 가 인스펙터에 연결 안 됐으면 런타임에 자동 생성한다
    // (수량 텍스트 뒤에 깔리는 아이콘 Image 를 슬롯 안에 만들어 연결)
    private void EnsureIconImage(QuickSlot slot)
    {
        if (slot == null || slot.iconImage != null)
        {
            return;
        }
        if (slot.countText == null)
        {
            return;
        }

        Transform slotRoot = slot.countText.transform.parent;
        if (slotRoot == null)
        {
            return;
        }

        GameObject iconObj = new GameObject("Icon(자동생성)", typeof(RectTransform));
        iconObj.transform.SetParent(slotRoot, false);
        // 수량 텍스트보다 뒤(아래)에 그려지도록 맨 앞 형제로
        iconObj.transform.SetAsFirstSibling();

        RectTransform rect = iconObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(4f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);

        Image image = iconObj.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.enabled = false;

        slot.iconImage = image;
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
                SaveAssignments();
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

        bool success = InventorySystem.Instance.UseConsumableByData(slot.consumable);

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

        bool hasIcon = slot.consumable != null && slot.consumable.itemIcon != null;

        // HUD 아이콘
        if (slot.iconImage != null)
        {
            if (hasIcon == true)
            {
                slot.iconImage.sprite = slot.consumable.itemIcon;
            }
            slot.iconImage.enabled = hasIcon;
        }

        // 인벤토리 아이콘 (같은 소모품을 양쪽에 표시)
        if (slot.invIconImage != null)
        {
            if (hasIcon == true)
            {
                slot.invIconImage.sprite = slot.consumable.itemIcon;
            }
            slot.invIconImage.enabled = hasIcon;
        }

        // 빈 슬롯이면 수량 텍스트도 비운다 (양쪽)
        if (slot.consumable == null)
        {
            if (slot.countText != null)
            {
                slot.countText.text = "";
            }
            if (slot.invCountText != null)
            {
                slot.invCountText.text = "";
            }
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

    // 슬롯 하나의 보유 수량 표시 + 보유 0이면 칸을 비운다
    private void RefreshSlot(QuickSlot slot)
    {
        if (slot == null || slot.consumable == null)
        {
            return;
        }

        // 인벤토리가 아직 준비 안 됐으면 (씬 전환 중 등) 칸을 건드리지 않는다
        // 이걸 안 하면 전환 순간 수량이 0으로 읽혀 등록이 지워질 수 있다.
        if (InventorySystem.Instance == null)
        {
            return;
        }

        int count = InventorySystem.Instance.GetConsumableCountByData(slot.consumable);

        // 보유를 다 써서 0이 되면 칸을 비운다 (다시 등록 가능)
        if (count <= 0)
        {
            ClearSlot(slot);
            return;
        }

        // 수량 텍스트 (HUD)
        if (slot.countText != null)
        {
            slot.countText.text = count.ToString();
        }

        // 보유가 있으면 아이콘 선명하게 (HUD)
        if (slot.iconImage != null)
        {
            slot.iconImage.color = new Color(1f, 1f, 1f, 1f);
        }

        // 수량 텍스트 (인벤토리)
        if (slot.invCountText != null)
        {
            slot.invCountText.text = count.ToString();
        }

        // 인벤토리 아이콘도 선명하게
        if (slot.invIconImage != null)
        {
            slot.invIconImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    // 슬롯을 비운다 (보유 0이 되면 호출) — 등록 해제 + 아이콘/수량 비움
    private void ClearSlot(QuickSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.consumable = null;

        // HUD 쪽 비움
        if (slot.iconImage != null)
        {
            slot.iconImage.enabled = false;
        }
        if (slot.countText != null)
        {
            slot.countText.text = "";
        }

        // 인벤토리 쪽 비움
        if (slot.invIconImage != null)
        {
            slot.invIconImage.enabled = false;
        }
        if (slot.invCountText != null)
        {
            slot.invCountText.text = "";
        }

        SaveAssignments();
    }

    // 지정한 번호의 슬롯을 수동으로 등록 해제한다 (인벤/HUD 칸 우클릭용)
    public void RemoveSlotAt(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            return;
        }
        if (slots[index] == null || slots[index].consumable == null)
        {
            return;
        }

        ClearSlot(slots[index]);
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // 현재 슬롯 등록 상태를 static 보관함에 저장한다 (씬 전환 후 Start 에서 복원용)
    private void SaveAssignments()
    {
        _savedConsumables.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            _savedConsumables.Add(slots[i] != null ? slots[i].consumable : null);
        }
    }
}