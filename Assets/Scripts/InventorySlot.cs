using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    public Image     itemIcon;
    public Image     highlightImage;
    public TMP_Text  stackCountText;

    [Header("Slot Data")]
    public ItemData  currentItem;
    public bool      isOccupied = false;
    public int       slotIndex  = 0;
    public Vector2Int gridPosition;

    // ─── 드래그 상태 ───
    private static InventorySlot _draggingSlot = null;
    private static GameObject    _dragIcon     = null;
    private static Canvas        _canvas;

    private void Awake()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }

    public void SetItem(ItemData item)
    {
        currentItem = item;
        isOccupied  = item != null;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(isOccupied);
            if (isOccupied) itemIcon.sprite = item.itemIcon;
        }
        if (stackCountText != null)
            stackCountText.gameObject.SetActive(false);
    }

    public void ClearSlot()
    {
        currentItem = null;
        isOccupied  = false;
        if (itemIcon != null)     itemIcon.gameObject.SetActive(false);
        if (highlightImage != null) highlightImage.gameObject.SetActive(false);
        if (stackCountText != null) stackCountText.gameObject.SetActive(false);
    }

    public bool CanPlaceItem(ItemData item) => item != null && !isOccupied;

    // ─── Pointer ───

    public void OnPointerEnter(PointerEventData e)
    {
        if (highlightImage != null) highlightImage.gameObject.SetActive(true);
        if (isOccupied && currentItem != null)
            UIItemTooltip.Instance?.ShowTooltip(currentItem, transform.position);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (highlightImage != null) highlightImage.gameObject.SetActive(false);
        UIItemTooltip.Instance?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData e)
    {
        // 우클릭 → 장착/해제
        if (e.button == PointerEventData.InputButton.Right && isOccupied && currentItem != null)
        {
            InventorySystem.Instance?.UseItem(currentItem);
        }
    }

    // ─── 드래그 시작 ───

    public void OnBeginDrag(PointerEventData e)
    {
        if (isOccupied == false || currentItem == null) return;

        _draggingSlot = this;
        UIItemTooltip.Instance?.HideTooltip();

        // 드래그 중 아이콘 생성
        _dragIcon = new GameObject("DragIcon");
        _dragIcon.transform.SetParent(_canvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        Image img = _dragIcon.AddComponent<Image>();
        img.sprite          = currentItem.itemIcon;
        img.raycastTarget   = false;

        RectTransform rect = _dragIcon.GetComponent<RectTransform>();
        rect.sizeDelta      = new Vector2(50, 50);
        rect.anchorMin      = Vector2.zero;
        rect.anchorMax      = Vector2.zero;
        rect.pivot          = new Vector2(0.5f, 0.5f);

        // 원본 슬롯 아이콘 반투명
        if (itemIcon != null) itemIcon.color = new Color(1, 1, 1, 0.4f);
    }

    // ─── 드래그 중 ───

    public void OnDrag(PointerEventData e)
    {
        if (_dragIcon == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            e.position, _canvas.worldCamera,
            out Vector2 localPoint);

        _dragIcon.GetComponent<RectTransform>().anchoredPosition = localPoint;
    }

    // ─── 드래그 끝 (빈 공간에 놓음) ───

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
                _draggingSlot.itemIcon.color = Color.white;
            _draggingSlot = null;
        }
    }

    // ─── 드롭 받음 ───

    public void OnDrop(PointerEventData e)
    {
        if (_draggingSlot == null || _draggingSlot == this) return;

        ItemData draggedItem = _draggingSlot.currentItem;
        if (draggedItem == null) return;

        if (isOccupied)
        {
            // 두 슬롯 스왑
            ItemData temp = currentItem;
            SetItem(draggedItem);
            _draggingSlot.SetItem(temp);
        }
        else
        {
            // 빈 슬롯으로 이동
            SetItem(draggedItem);
            _draggingSlot.ClearSlot();
        }

        // 아이콘 색상 복원
        if (_draggingSlot.itemIcon != null)
            _draggingSlot.itemIcon.color = Color.white;

        _draggingSlot = null;

        if (_dragIcon != null)
        {
            Destroy(_dragIcon);
            _dragIcon = null;
        }
    }
}
