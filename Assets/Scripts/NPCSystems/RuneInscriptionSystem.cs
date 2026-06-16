// RuneInscriptionSystem.cs
// 각인 핵심 로직 — 자원과 골드를 소모하여 방어구(ArmorInstance)에 각인을 부여하고 초기화권으로 제거한다.
// UI 는 InscriptionMasterSystem 이 담당하고, 실제 처리는 이 클래스가 맡는다.
// 자원 비용: 투구 5 / 갑옷 7 / 각반 6 / 장화 5 (속성 무관 동일)
// 골드 비용: 부위별 (인스펙터 조정)
// 각인 슬롯은 부위당 1개이며, 이미 각인된 방어구는 초기화 후에만 다시 부여할 수 있다.
// 담당: 김보민

using System.Collections.Generic;
using UnityEngine;

// 각인 부여 결과 코드
public enum InscribeResult
{
    Success,            // 성공
    NoArmor,            // 방어구 미선택
    AlreadyInscribed,   // 이미 각인됨 (부위당 1개)
    NotEnoughResource,  // 자원 부족
    NotEnoughGold,      // 골드 부족
    Invalid             // 그 외 (데이터 누락 등)
}

// 각인 초기화 결과 코드
public enum ResetResult
{
    Success,   // 성공
    NoArmor,   // 방어구 미선택
    NoRune,    // 각인이 없음
    NoScroll   // 초기화권 없음
}

public class RuneInscriptionSystem : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static RuneInscriptionSystem Instance { get; private set; }

    [Header("골드 비용 (부위별 — 인스펙터 조정)")]
    [SerializeField] private int helmetGoldCost = 2300;
    [SerializeField] private int chestGoldCost = 3200;
    [SerializeField] private int legsGoldCost = 2800;
    [SerializeField] private int bootsGoldCost = 2300;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ─────────────────────── 각인 부여 ───────────────────────

    // 방어구에 각인을 부여한다. (부위당 1개 / 자원 + 골드 동시 소모)
    public InscribeResult TryInscribe(ArmorInstance armor, RuneElement element)
    {
        if (armor == null)
        {
            return InscribeResult.NoArmor;
        }
        if (element == RuneElement.None)
        {
            return InscribeResult.Invalid;
        }

        // 부위당 각인 1개 — 이미 각인되어 있으면 초기화가 필요
        if (armor.HasRune == true)
        {
            return InscribeResult.AlreadyInscribed;
        }

        ArmorData data = armor.ArmorData;
        if (data == null)
        {
            return InscribeResult.Invalid;
        }

        ResourceInventory resourceInv = ResourceInventory.Instance;
        if (resourceInv == null)
        {
            return InscribeResult.Invalid;
        }

        PlayerStats stats = PlayerStats.Instance;
        if (stats == null)
        {
            return InscribeResult.Invalid;
        }

        int resourceCost = GetResourceCost(data.ArmorSlot);
        int goldCost = GetGoldCost(data.ArmorSlot);
        InscriptionType resourceType = ToInscriptionType(element);

        // 자원 확인
        int ownedResource = resourceInv.GetResourceCount(resourceType);
        if (ownedResource < resourceCost)
        {
            return InscribeResult.NotEnoughResource;
        }

        // 골드 확인
        if (stats.gold < goldCost)
        {
            return InscribeResult.NotEnoughGold;
        }

        // 각인 적용 (실패 시 아무것도 차감하지 않음)
        bool applied = armor.SetRune(element);
        if (applied == false)
        {
            return InscribeResult.Invalid;
        }

        // 차감
        stats.gold = stats.gold - goldCost;
        resourceInv.RemoveResource(resourceType, resourceCost);

        // 장착 중인 방어구라면 세트 효과 즉시 재계산
        RefreshSetIfEquipped(armor);

        return InscribeResult.Success;
    }

    // ─────────────────────── 각인 초기화 ───────────────────────

    // 초기화권 1개를 소모하여 방어구의 각인을 제거한다.
    public ResetResult TryReset(ArmorInstance armor)
    {
        if (armor == null)
        {
            return ResetResult.NoArmor;
        }
        if (armor.HasRune == false)
        {
            return ResetResult.NoRune;
        }

        ItemData scroll = FindResetScroll();
        if (scroll == null)
        {
            return ResetResult.NoScroll;
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.RemoveItem(scroll);
        }

        armor.ClearRunes();

        // 장착 중인 방어구라면 세트 효과 즉시 재계산
        RefreshSetIfEquipped(armor);

        return ResetResult.Success;
    }

    // ─────────────────────── 초기화권 조회 ───────────────────────

    // 인벤토리에서 초기화 주문서(ConsumableType.ResetScroll) 한 개를 찾는다.
    public ItemData FindResetScroll()
    {
        if (InventorySystem.Instance == null)
        {
            return null;
        }

        List<ItemData> items = InventorySystem.Instance.items;
        for (int i = 0; i < items.Count; i++)
        {
            ConsumableData consumable = items[i] as ConsumableData;
            if (consumable == null)
            {
                continue;
            }
            if (consumable.ConsumableType == ConsumableType.ResetScroll)
            {
                return items[i];
            }
        }

        return null;
    }

    // 보유한 초기화 주문서 개수
    public int GetResetScrollCount()
    {
        if (InventorySystem.Instance == null)
        {
            return 0;
        }

        int count = 0;
        List<ItemData> items = InventorySystem.Instance.items;
        for (int i = 0; i < items.Count; i++)
        {
            ConsumableData consumable = items[i] as ConsumableData;
            if (consumable == null)
            {
                continue;
            }
            if (consumable.ConsumableType == ConsumableType.ResetScroll)
            {
                count = count + 1;
            }
        }

        return count;
    }

    // ─────────────────────── 비용 ───────────────────────

    // 부위별 각인 자원 비용 (기획서: 투구 5 / 갑옷 7 / 각반 6 / 장화 5)
    public int GetResourceCost(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet:
                {
                    return 5;
                }
            case ArmorSlot.Chest:
                {
                    return 7;
                }
            case ArmorSlot.Legs:
                {
                    return 6;
                }
            case ArmorSlot.Boots:
                {
                    return 5;
                }
            default:
                {
                    return 5;
                }
        }
    }

    // 부위별 각인 골드 비용
    public int GetGoldCost(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet:
                {
                    return helmetGoldCost;
                }
            case ArmorSlot.Chest:
                {
                    return chestGoldCost;
                }
            case ArmorSlot.Legs:
                {
                    return legsGoldCost;
                }
            case ArmorSlot.Boots:
                {
                    return bootsGoldCost;
                }
            default:
                {
                    return helmetGoldCost;
                }
        }
    }

    // ─────────────────────── 세트 효과 재계산 ───────────────────────

    // 장착 중인 방어구의 각인이 바뀌면 세트 효과를 다시 계산한다.
    // ArmorSetManager 는 장착/해제 시에만 재계산하므로, 해제에서 재장착으로 트릭을 써서 갱신한다.
    // (정건희 ArmorSetManager 에 공개 재계산 훅이 생기면 그것으로 교체 예정)
    private void RefreshSetIfEquipped(ArmorInstance armor)
    {
        if (armor == null)
        {
            return;
        }
        if (armor.IsEquipped == false)
        {
            return;
        }

        ArmorSetManager manager = FindFirstObjectByType<ArmorSetManager>();
        if (manager == null)
        {
            return;
        }

        manager.OnArmorUnequipped(armor);
        manager.OnArmorEquipped(armor);
    }

    // ─────────────────────── 원소 변환 / 표시 ───────────────────────

    // RuneElement 를 자원 키(InscriptionType)로 변환 (두 열거형의 정수값이 다르므로 명시 변환)
    public static InscriptionType ToInscriptionType(RuneElement element)
    {
        switch (element)
        {
            case RuneElement.Fire:
                {
                    return InscriptionType.Fire;
                }
            case RuneElement.Water:
                {
                    return InscriptionType.Water;
                }
            case RuneElement.Wind:
                {
                    return InscriptionType.Wind;
                }
            case RuneElement.Earth:
                {
                    return InscriptionType.Earth;
                }
            case RuneElement.Darkness:
                {
                    return InscriptionType.Darkness;
                }
            default:
                {
                    return InscriptionType.None;
                }
        }
    }

    // 원소 한국어 이름
    public static string GetElementName(RuneElement element)
    {
        switch (element)
        {
            case RuneElement.Fire:
                {
                    return "불";
                }
            case RuneElement.Water:
                {
                    return "물";
                }
            case RuneElement.Wind:
                {
                    return "바람";
                }
            case RuneElement.Earth:
                {
                    return "땅";
                }
            case RuneElement.Darkness:
                {
                    return "어둠";
                }
            default:
                {
                    return "없음";
                }
        }
    }

    // 원소 대표 색상 (InscriptionColorHelper 팔레트와 동일하게 맞춤)
    public static Color GetElementColor(RuneElement element)
    {
        switch (element)
        {
            case RuneElement.Fire:
                {
                    return new Color(1f, 0.3f, 0.1f, 1f); // 빨강
                }
            case RuneElement.Water:
                {
                    return new Color(0.1f, 0.5f, 1f, 1f); // 파랑
                }
            case RuneElement.Wind:
                {
                    return new Color(0.5f, 1f, 0.3f, 1f); // 초록
                }
            case RuneElement.Earth:
                {
                    return new Color(0.6f, 0.4f, 0.1f, 1f); // 갈색
                }
            case RuneElement.Darkness:
                {
                    return new Color(0.4f, 0.1f, 0.6f, 1f); // 보라
                }
            default:
                {
                    return new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색 반투명
                }
        }
    }
}