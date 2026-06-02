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

    // 런타임 아이템 인스턴스 (강화 단계, 각인 등 실시간 정보 보유)
    private ItemInstance _currentInstance = null;

    // 드래그 상태
    private static InventorySlot _draggingSlot = null;
    private static GameObject _dragIcon = null;
    private static Canvas _canvas;

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
                _draggingSlot.ClearSlot();
            }
        }
        else
        {
            // 창고에서 인벤토리로
            if (StorageUI.Instance != null)
            {
                StorageUI.Instance.RemoveFromStorageData(draggedInstance);
            }
            InventorySystem.Instance?.AddInstanceData(draggedInstance);
            _draggingSlot.ClearSlot();
            SetItem(draggedInstance);
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

        // 인스턴스가 있으면 런타임 정보(강화/각인) 포함 툴팁 표시
        // 인스턴스가 없으면 ItemData 기반 툴팁 표시
        if (_currentInstance != null)
        {
            UIItemTooltip.Instance?.ShowTooltip(_currentInstance, transform.position);
        }
        else if (currentItem != null)
        {
            UIItemTooltip.Instance?.ShowTooltip(currentItem, transform.position);
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
        // 우클릭: 장착/해제
        if (e.button == PointerEventData.InputButton.Right && isOccupied == true && currentItem != null)
        {
            InventorySystem.Instance?.UseItem(currentItem);
        }
    }

    // ─────────────────────── 드래그 시작 ───────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (isOccupied == false || currentItem == null)
        {
            return;
        }

        _draggingSlot = this;
        UIItemTooltip.Instance?.HideTooltip();

        // 드래그 중 아이콘 생성
        _dragIcon = new GameObject("DragIcon");
        _dragIcon.transform.SetParent(_canvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        Image img = _dragIcon.AddComponent<Image>();
        img.sprite = currentItem.itemIcon;
        img.raycastTarget = false;

        RectTransform rect = _dragIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        // 원본 슬롯 아이콘 반투명
        if (itemIcon != null)
        {
            itemIcon.color = new Color(1, 1, 1, 0.4f);
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

        // 인스턴스 기반으로 스왑 (인스턴스 없으면 ItemData로 폴백)
        ItemInstance draggedInstance = _draggingSlot._currentInstance;
        ItemData draggedItem = _draggingSlot.currentItem;

        if (draggedItem == null)
        {
            return;
        }

        if (isOccupied == true)
        {
            // 두 슬롯 스왑
            ItemInstance tempInstance = _currentInstance;
            ItemData tempItem = currentItem;

            if (draggedInstance != null)
            {
                SetItem(draggedInstance);
            }
            else
            {
                SetItem(draggedItem);
            }

            if (tempInstance != null)
            {
                _draggingSlot.SetItem(tempInstance);
            }
            else
            {
                _draggingSlot.SetItem(tempItem);
            }
        }
        else
        {
            // 빈 슬롯으로 이동
            if (draggedInstance != null)
            {
                SetItem(draggedInstance);
            }
            else
            {
                SetItem(draggedItem);
            }
            _draggingSlot.ClearSlot();
        }

        if (_draggingSlot != null && _draggingSlot.itemIcon != null)
        {
            _draggingSlot.itemIcon.color = Color.white;
        }

        _draggingSlot = null;

        if (_dragIcon != null)
        {
            Destroy(_dragIcon);
            _dragIcon = null;
        }
        // 컨테이너 간 이동 (인벤토리 <-> 창고)
        if (_draggingSlot.container != this.container)
        {
            HandleCrossContainerDrop(draggedInstance);

            // 드래그 정리
            if (_draggingSlot != null && _draggingSlot.itemIcon != null)
            {
                _draggingSlot.itemIcon.color = Color.white;
            }
            _draggingSlot = null;
            if (_dragIcon != null)
            {
                Destroy(_dragIcon);
                _dragIcon = null;
            }
            return;
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
