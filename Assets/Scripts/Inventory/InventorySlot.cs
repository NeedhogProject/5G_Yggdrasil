using UnityEngine;
using System.Collections.Generic;
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

    // 판매창에 담겨 어둡게 표시 중인지 여부 (인벤에선 그대로 두고 표시만 변경)
    private bool _saleMarked = false;

    // 판매 마킹 상태 외부 읽기용
    public bool IsSaleMarked
    {
        get { return _saleMarked; }
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
    // fromSlot: 드래그를 시작한 출발 슬롯 (OnDrop 에서 확보해 넘겨줌)
    // 빈 자리면 그 자리에, 아이템 1개면 위치 교환, 교환 불가면 빈 공간으로, 빈 공간도 없으면 변경 없음
    private void HandleCrossContainerDrop(InventorySlot fromSlot, ItemInstance draggedInstance)
    {
        // 창고는 인스턴스 단위 보관 — 인스턴스 없는 아이템은 이동 불가
        if (draggedInstance == null)
        {
            Debug.LogWarning("[InventorySlot] 인스턴스 없는 아이템은 옮길 수 없습니다.");
            return;
        }

        if (fromSlot == null)
        {
            return;
        }

        if (InventorySystem.Instance == null || StorageUI.Instance == null)
        {
            return;
        }

        bool bToStorage = container == SlotContainer.Storage;

        // 잡은 칸 보정: 아이템 좌상단(주인)이 들어갈 칸
        Vector2Int vTarget = gridPosition - _grabOffset;
        List<InventorySlot> targetOwners = GetOwnersInArea(bToStorage, vTarget, draggedInstance.Data.itemSize);

        // 1. 놓은 자리가 비어 있으면 그 자리에 배치
        if (targetOwners != null && targetOwners.Count == 0 && PlaceInContainer(bToStorage, GetSlot(bToStorage, vTarget), draggedInstance) == true)
        {
            RemoveFromContainer(bToStorage == false, fromSlot, draggedInstance);
            return;
        }

        // 2. 놓은 자리에 아이템 1개가 있으면 서로 위치 교환
        if (targetOwners != null && targetOwners.Count == 1 && TrySwap(bToStorage, fromSlot, draggedInstance, targetOwners[0], vTarget) == true)
        {
            return;
        }

        // 3. 교환 불가: 받는 쪽 빈 공간으로, 그것도 없으면 변경 없음
        bool bAdded = bToStorage ? StorageUI.Instance.AddToStorageData(draggedInstance) : InventorySystem.Instance.AddItem(draggedInstance);
        if (bAdded == true)
        {
            RemoveFromContainer(bToStorage == false, fromSlot, draggedInstance);
            return;
        }

        Debug.Log("[InventorySlot] 교환할 수 없고 빈 공간도 없어 이동하지 않습니다.");
    }

    // 끌어온 아이템(A)과 놓은 자리의 아이템(B) 위치 교환, 크기나 창고 용량이 맞지 않으면 false
    private bool TrySwap(bool _bToStorage, InventorySlot _fromSlot, ItemInstance _draggedInstance, InventorySlot _targetOwner, Vector2Int _vTarget)
    {
        ItemInstance targetInstance = _targetOwner.CurrentInstance;
        // 판매창에 담긴 아이템은 교환 대상에서 제외 (끌어온 쪽은 OnBeginDrag 에서 이미 차단)
        if (targetInstance == null || _targetOwner.IsSaleMarked == true)
        {
            return false;
        }

        // B 가 A 를 뺀 원래 자리에 들어가야 교환 가능 (A 쪽은 놓은 자리에 B 하나뿐이라 이미 확인됨)
        Vector2Int vSource = _fromSlot.gridPosition;
        if (IsAreaFreeExcept(_bToStorage == false, vSource, targetInstance.Data.itemSize, _fromSlot) == false)
        {
            return false;
        }

        // 창고는 칸 배치와 별개로 총 용량 제한이 있어 미리 확인 (배치 도중 실패하면 아이템이 사라짐)
        ItemData storageIn = _bToStorage ? _draggedInstance.Data : targetInstance.Data;
        ItemData storageOut = _bToStorage ? targetInstance.Data : _draggedInstance.Data;
        if (StorageUI.Instance.HasCapacity(storageIn, storageOut) == false)
        {
            return false;
        }

        RemoveFromContainer(_bToStorage == false, _fromSlot, _draggedInstance);
        RemoveFromContainer(_bToStorage, _targetOwner, targetInstance);

        PlaceInContainer(_bToStorage, GetSlot(_bToStorage, _vTarget), _draggedInstance);
        PlaceInContainer(_bToStorage == false, GetSlot(_bToStorage == false, vSource), targetInstance);

        Debug.Log("[InventorySlot] 위치 교환: " + _draggedInstance.Data.itemName + " <-> " + targetInstance.Data.itemName);
        return true;
    }

    // 좌표의 칸 (_bStorage: 창고 / 인벤토리, 범위 밖이면 null)
    private static InventorySlot GetSlot(bool _bStorage, Vector2Int _vGrid)
    {
        if (_bStorage == true)
        {
            return StorageUI.Instance.GetSlotAt(_vGrid);
        }
        return InventorySystem.Instance.GetSlotAt(_vGrid);
    }

    // 영역 안 아이템들의 주인 칸 목록 (영역이 칸 밖으로 나가면 null)
    private static List<InventorySlot> GetOwnersInArea(bool _bStorage, Vector2Int _vStart, Vector2Int _vSize)
    {
        List<InventorySlot> owners = new List<InventorySlot>();

        for (int y = 0; y < _vSize.y; y++)
        {
            for (int x = 0; x < _vSize.x; x++)
            {
                InventorySlot slot = GetSlot(_bStorage, new Vector2Int(_vStart.x + x, _vStart.y + y));
                if (slot == null)
                {
                    return null;
                }
                if (slot.isOccupied == false)
                {
                    continue;
                }

                InventorySlot owner = slot.ownerSlot != null ? slot.ownerSlot : slot;
                if (owners.Contains(owner) == false)
                {
                    owners.Add(owner);
                }
            }
        }
        return owners;
    }

    // 지정한 아이템(_ignoreOwner)을 뺐다고 가정할 때 영역이 비는지
    private static bool IsAreaFreeExcept(bool _bStorage, Vector2Int _vStart, Vector2Int _vSize, InventorySlot _ignoreOwner)
    {
        List<InventorySlot> owners = GetOwnersInArea(_bStorage, _vStart, _vSize);
        if (owners == null)
        {
            return false;
        }
        foreach (InventorySlot owner in owners)
        {
            if (owner != _ignoreOwner)
            {
                return false;
            }
        }
        return true;
    }

    // 지정 칸에 배치 (_bStorage: 창고 / 인벤토리)
    private static bool PlaceInContainer(bool _bStorage, InventorySlot _slot, ItemInstance _instance)
    {
        if (_slot == null)
        {
            return false;
        }
        if (_bStorage == true)
        {
            return StorageUI.Instance.PlaceAtSlot(_slot, _instance);
        }
        return InventorySystem.Instance.AddItemAtSlot(_slot, _instance);
    }

    // 보관함에서 데이터와 칸까지 제거
    private static void RemoveFromContainer(bool _bStorage, InventorySlot _ownerSlot, ItemInstance _instance)
    {
        if (_bStorage == true)
        {
            StorageUI.Instance.RemoveFromStorageData(_instance);
            return;
        }
        InventorySystem.Instance.RemoveItemAtSlot(_ownerSlot);
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

        // 판매 마킹도 해제하고 색 원복
        _saleMarked = false;
        if (itemIcon != null)
        {
            itemIcon.color = Color.white;
        }

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

    // 판매창에 담긴 표시 (인벤에선 그대로 두고 아이콘만 어둡게)
    // marked == true 면 어둡게, false 면 원래 흰색으로 복원
    public void SetSaleMarked(bool marked)
    {
        _saleMarked = marked;

        if (itemIcon == null)
        {
            return;
        }

        if (marked == true)
        {
            // 어둡게 (회색 톤)
            itemIcon.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        else
        {
            itemIcon.color = Color.white;
        }
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

        // 창고 슬롯에서는 우클릭(장착/판매)을 막는다.
        // 창고 아이템을 우클릭하면 장착되면서 인벤토리에 인스턴스가 추가되어 복제가 발생한다.
        if (container == SlotContainer.Storage)
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
                // 이미 담긴 슬롯이면 다시 우클릭 시 담기 취소
                if (source.IsSaleMarked == true)
                {
                    ShopSystem.Instance.UnstageSlot(source);
                }
                else
                {
                    ShopSystem.Instance.StageForSale(source);
                }
            }
            return;
        }

        // 그 외에는 장착/해제
        if (source.currentItem != null)
        {
            InventorySystem.Instance?.UseItem(source.currentItem, source.CurrentInstance);
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

        // 판매창에 담긴(마킹된) 아이템은 드래그로 옮기지 못하게 막는다
        if (dragSource.IsSaleMarked == true)
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
        // 드롭 처리 시작 시점에 출발 슬롯을 확보하고 static 참조를 즉시 비운다.
        // 멀티셀 아이템이 여러 슬롯에 걸쳐 OnDrop 이 두 번 이상 불려도 첫 번째만 처리되게 한다 (복제 방지).
        InventorySlot fromSlot = _draggingSlot;
        _draggingSlot = null;

        if (fromSlot == null || fromSlot == this)
        {
            return;
        }

        ItemInstance draggedInstance = fromSlot._currentInstance;
        ItemData draggedItem = fromSlot.currentItem;

        if (draggedItem == null)
        {
            RestoreDragVisual(fromSlot);
            return;
        }

        // 컨테이너가 다르면 (인벤토리 <-> 창고) 별도 처리 후 종료
        if (fromSlot.container != this.container)
        {
            HandleCrossContainerDrop(fromSlot, draggedInstance);
            RestoreDragVisual(fromSlot);
            _grabOffset = Vector2Int.zero;
            return;
        }

        // 둘 다 창고 슬롯이면 창고 내부 위치 이동 (StorageUI가 처리)
        // 인벤토리 MoveItem 은 인벤 그리드 기준이라 창고에 쓰면 인벤으로 빠지므로 분기한다.
        if (this.container == SlotContainer.Storage)
        {
            if (StorageUI.Instance != null)
            {
                StorageUI.Instance.MoveWithinStorage(fromSlot, this);
            }
            RestoreDragVisual(fromSlot);
            _grabOffset = Vector2Int.zero;
            return;
        }

        // 잡은 위치 보정 — 아이템 좌상단(주인)이 들어갈 칸을 역산해서 목표로 삼음
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            RestoreDragVisual(fromSlot);
            _grabOffset = Vector2Int.zero;
            return;
        }

        Vector2Int targetOwnerGrid = gridPosition - _grabOffset;
        InventorySlot targetSlot = inventory.GetSlotAt(targetOwnerGrid);
        if (targetSlot == null)
        {
            RestoreDragVisual(fromSlot);
            _grabOffset = Vector2Int.zero;
            return;
        }

        // 다중 칸 이동은 InventorySystem이 충돌 검사까지 처리 (실패 시 내부에서 원위치 복원)
        inventory.MoveItem(fromSlot, targetSlot);

        RestoreDragVisual(fromSlot);
        _grabOffset = Vector2Int.zero;
    }

    // 드래그 종료 후 아이콘 색 복원 + 드래그 아이콘 제거
    private void RestoreDragVisual(InventorySlot fromSlot)
    {
        if (fromSlot != null && fromSlot.itemIcon != null)
        {
            fromSlot.itemIcon.color = Color.white;
        }

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