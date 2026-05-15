using UnityEngine;

public class ItemInventoryPanel : MonoBehaviour
{
    [SerializeField] InventorySlot slotPrefab;
    [SerializeField] Transform slotContainer;
    [SerializeField] int slotCount = 20;

    InventorySlot[] _slots;

    void Start()
    {
        GenerateSlots();
    }

    void GenerateSlots()
    {
        _slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.slotIndex = i;
            slot.ClearSlot();
            _slots[i] = slot;
        }
    }

    public bool TryAddItem(ItemData item)
    {
        foreach (var slot in _slots)
        {
            if (slot.isOccupied == false)
            {
                slot.SetItem(item);
                return true;
            }
        }
        return false;
    }

    public void Refresh(ItemData[] items)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            bool hasItem = items != null && i < items.Length && items[i] != null;
            if (hasItem == true)
                _slots[i].SetItem(items[i]);
            else
                _slots[i].ClearSlot();
        }
    }
}