using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public Image highlightImage;
    public TMP_Text stackCountText;

    [Header("Slot Data")]
    public ItemData currentItem;
    public bool isOccupied = false;
    public int slotIndex = 0;
    public Vector2Int gridPosition;

    // 다중 칸 점유 — 이 슬롯이 큰 아이템의 일부일 때 주인 슬롯을 가리킴
    // 주인 슬롯 자신은 ownerSlot == this (또는 null), 점유된 보조 칸은 주인을 참조
    public InventorySlot ownerSlot = null;

    // 이 슬롯이 아이템의 좌상단(주인)인지 여부
    public bool IsOwner => isOccupied && (ownerSlot == null || ownerSlot == this);

    // 런타임 아이템 인스턴스 (강화 단계, 각인 등 실시간 정보 보유)
    private ItemInstance _currentInstance = null;

    // 런타임 인스턴스 외부 읽기용 (상점 판매 등)
    public ItemInstance CurrentInstance
    {
        get { return _currentInstance; }
    }

    // 드래그 상태
    private static InventorySlot _draggingSlot = null;
    private static GameObject _dragIcon = null;
    private static Canvas _canvas;

    // 멀티셀 아이템에서 잡은 칸이 주인 칸으로부터 떨어진 정도 (드롭 위치 보정용)
    private static Vector2Int _grabOffset = Vector2Int.zero;

    private void Awake()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }
    }

    // 인벤토리 <-> 창고 간 아이템 이동 (데이터 동기화 포함)
    private void HandleCrossContainerDrop(ItemInstance draggedInstance)
    {
        // 창고는 인스턴스 단위 보관 — 인스턴스 없는 아이템은 이동 불가
        if (draggedInstance == null)
        {
            Debug.LogWarning("[InventorySlot] 인스턴스 없는 아이템은 옮길 수 없습니다.");
            return;
        }

        // 목적지 칸이 차있으면 이동 막음 (컨테이너 간 스왑은 미지원)
        if (isOccupied == true)
        {
            Debug.Log("[InventorySlot] 빈 칸에만 옮길 수 있습니다.");
            return;
        }

        if (container == SlotContainer.Storage)
        {
            // 인벤토리에서 창고로
            if (StorageUI.Instance == null)
            {
                return;
            }

            bool added = StorageUI.Instance.AddToStorageData(draggedInstance);
            if (added == true)
            {
                InventorySystem.Instance?.RemoveInstanceData(draggedInstance);
                // 인벤토리 출발 슬롯의 다중 칸 점유 해제
                InventorySystem.Instance?.ReleaseSlotArea(_draggingSlot);
            }
        }
        else
        {
            // 창고에서 인벤토리로
            if (StorageUI.Instance != null)
            {
                StorageUI.Instance.RemoveFromStorageData(draggedInstance);
            }
            // 창고 출발 슬롯 비우기 (창고는 단일 칸 관리)
            _draggingSlot.ClearSlot();
            // 인벤토리에 추가 — 다중 칸 점유 자동 처리
            InventorySystem.Instance?.AddItem(draggedInstance);
        }
    }

    // ─────────────────────── 슬롯 세팅 ───────────────────────

    /// <summary>
    /// ItemData만 있을 때 세팅 (상점 미리보기 등 인스턴스 없는 경우)
    /// </summary>
    public void SetItem(ItemData item)
    {
        currentItem = item;
        _currentInstance = null;
        isOccupied = item != null;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(isOccupied);
            if (isOccupied == true)
            {
                itemIcon.sprite = item.itemIcon;
            }
        }
        if (stackCountText != null)
        {
            stackCountText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ItemInstance 기반 세팅 (인벤토리 실제 아이템 배치 시 사용)
    /// 강화 단계, 각인 등 런타임 정보를 툴팁에 전달하기 위해 인스턴스 보존
    /// </summary>
    public void SetItem(ItemInstance instance)
    {
        if (instance == null || instance.Data == null)
        {
            ClearSlot();
            return;
        }

        _currentInstance = instance;
        currentItem = instance.Data;
        isOccupied = true;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = instance.Data.itemIcon;
        }

        // 수량이 2개 이상이면 숫자 표시
        if (stackCountText != null)
        {
            bool showCount = instance.StackCount > 1;
            stackCountText.gameObject.SetActive(showCount);
            if (showCount == true)
            {
                stackCountText.text = instance.StackCount.ToString();
            }
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        _currentInstance = null;
        isOccupied = false;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(false);
        }
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        if (stackCountText != null)
        {
            stackCountText.gameObject.SetActive(false);
        }
    }

    public bool CanPlaceItem(ItemData item)
    {
        return item != null && isOccupied == false;
    }

    // ─────────────────────── Pointer ───────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
        }

        if (isOccupied == false)
        {
            return;
        }

        // 보조 칸이면 주인 슬롯 기준으로 툴팁 표시
        InventorySlot source = this;
        if (ownerSlot != null && ownerSlot != this)
        {
            source = ownerSlot;
        }

        if (source._currentInstance != null)
        {
            UIItemTooltip.Instance?.ShowTooltip(source._currentInstance, transform.position);
        }
        else if (source.currentItem != null)
        {
            UIItemTooltip.Instance?.ShowTooltip(source.currentItem, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        UIItemTooltip.Instance?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right || isOccupied == false)
        {
            return;
        }

        // 보조 칸이면 주인 슬롯 기준
        InventorySlot source = this;
        if (ownerSlot != null && ownerSlot != this)
        {
            source = ownerSlot;
        }

        // 상점이 판매 모드면 우클릭 = 판매창에 담기 (장착보다 우선)
        if (container == SlotContainer.Inventory && ShopSystem.Instance != null && ShopSystem.Instance.IsOpen == true && ShopSystem.Instance.IsSellMode == true)
        {
            if (source.currentItem != null)
            {
                ShopSystem.Instance.StageForSale(source);
            }
            return;
        }

        // 그 외에는 장착/해제
        if (source.currentItem != null)
        {
            InventorySystem.Instance?.UseItem(source.currentItem);
        }
    }

    // ─────────────────────── 드래그 시작 ───────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (isOccupied == false)
        {
            return;
        }

        // 점유된 보조 칸에서 드래그 시작하면 주인 슬롯을 드래그 대상으로
        InventorySlot dragSource = this;
        if (ownerSlot != null && ownerSlot != this)
        {
            dragSource = ownerSlot;
        }

        if (dragSource.currentItem == null)
        {
            return;
        }

        _draggingSlot = dragSource;

        // 잡은 칸이 주인 칸에서 얼마나 떨어졌는지 기록 (드롭 시 역산용)
        _grabOffset = gridPosition - dragSource.gridPosition;

        UIItemTooltip.Instance?.HideTooltip();

        // 드래그 중 아이콘 생성
        _dragIcon = new GameObject("DragIcon");
        _dragIcon.transform.SetParent(_canvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        Image img = _dragIcon.AddComponent<Image>();
        img.sprite = dragSource.currentItem.itemIcon;
        img.raycastTarget = false;

        RectTransform rect = _dragIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);
        // 위치를 캔버스 중앙 기준 좌표로 넣으므로 앵커도 중앙이어야 마우스와 일치함
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // 원본(주인) 슬롯 아이콘 반투명
        if (dragSource.itemIcon != null)
        {
            dragSource.itemIcon.color = new Color(1, 1, 1, 0.4f);
        }
    }

    // ─────────────────────── 드래그 중 ───────────────────────

    public void OnDrag(PointerEventData e)
    {
        if (_dragIcon == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            e.position, _canvas.worldCamera,
            out Vector2 localPoint);

        _dragIcon.GetComponent<RectTransform>().anchoredPosition = localPoint;
    }

    // ─────────────────────── 드래그 끝 (빈 공간에 놓음) ───────────────────────

    public void OnEndDrag(PointerEventData e)
    {
        if (_dragIcon != null)
        {
            Destroy(_dragIcon);
            _dragIcon = null;
        }

        // 슬롯에 드롭 안 됐으면 원본 복원
        if (_draggingSlot != null)
        {
            if (_draggingSlot.itemIcon != null)
            {
                _draggingSlot.itemIcon.color = Color.white;
            }
            _draggingSlot = null;
        }
    }

    // ─────────────────────── 드롭 받음 ───────────────────────

    public void OnDrop(PointerEventData e)
    {
        if (_draggingSlot == null || _draggingSlot == this)
        {
            return;
        }

        ItemInstance draggedInstance = _draggingSlot._currentInstance;
        ItemData draggedItem = _draggingSlot.currentItem;

        if (draggedItem == null)
        {
            return;
        }

        // 컨테이너가 다르면 (인벤토리 <-> 창고) 별도 처리 후 종료
        if (_draggingSlot.container != this.container)
        {
            HandleCrossContainerDrop(draggedInstance);
            CleanupDrag();
            return;
        }

        // 잡은 위치 보정 — 아이템 좌상단(주인)이 들어갈 칸을 역산해서 목표로 삼음
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            CleanupDrag();
            return;
        }

        Vector2Int targetOwnerGrid = gridPosition - _grabOffset;
        InventorySlot targetSlot = inventory.GetSlotAt(targetOwnerGrid);
        if (targetSlot == null)
        {
            CleanupDrag();
            return;
        }

        // 다중 칸 이동은 InventorySystem이 충돌 검사까지 처리 (실패 시 내부에서 원위치 복원)
        inventory.MoveItem(_draggingSlot, targetSlot);

        CleanupDrag();
    }

    // 드래그 종료 후 공통 정리
    private void CleanupDrag()
    {
        if (_draggingSlot != null && _draggingSlot.itemIcon != null)
        {
            _draggingSlot.itemIcon.color = Color.white;
        }
        _draggingSlot = null;
        _grabOffset = Vector2Int.zero;

        if (_dragIcon != null)
        {
            Destroy(_dragIcon);
            _dragIcon = null;
        }
    }

    // 이 슬롯이 어느 보관함 소속인지 (드래그 이동 시 데이터 동기화 분기용)
    public enum SlotContainer
    {
        Inventory,
        Storage
    }
    public SlotContainer container = SlotContainer.Inventory;
}