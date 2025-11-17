using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public int slotIndex;
    public RelicData item;
    public int quantity;

    // 수량 추가
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }

    // 수량 제거
    public void RemoveQuantity(int amount)
    {
        quantity -= amount;
        if (quantity < 0)
        {
            quantity = 0;
        }
    }

    // 슬롯 초기화
    public void ClearSlot()
    {
        item = null;
        quantity = 0;
    }

    // 슬롯이 비어있는지 확인
    public bool IsEmpty()
    {
        return item == null || quantity <= 0;
    }

    // 슬롯이 가득 찼는지 확인
    public bool IsFull()
    {
        if (item == null) return false;
        return quantity >= item.maxStack;
    }

    // 추가 가능한 수량 계산
    public int GetAvailableSpace()
    {
        if (item == null) return 0;
        return item.maxStack - quantity;
    }
}