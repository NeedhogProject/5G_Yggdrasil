using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 기초 무기 지급
/// 무기 3종(단검/장검/창)을 인벤토리에 넣고, 장검을 자동 장착한다.
///
/// [사용법]
/// Player 오브젝트에 부착
/// startingWeapons 에 WeaponData 3개 연결
/// equipOnStart 에 시작 장착할 무기(장검) 연결
///
/// [주의]
/// 새 게임 시작 시에만 동작해야 하므로,
/// 세이브 로드로 들어온 경우에는 GameManager 상태를 확인해 건너뛸 수 있음
/// </summary>
public class StartingEquipment : MonoBehaviour
{
    [Header("시작 보유 무기 (인벤토리에 들어감)")]
    [SerializeField] private List<WeaponData> startingWeapons = new List<WeaponData>();

    [Header("시작 장착 무기 (장검)")]
    [SerializeField] private WeaponData equipOnStart;

    [Header("중복 지급 방지")]
    [Tooltip("이미 지급했으면 다시 주지 않음 (씬 재진입 대비)")]
    [SerializeField] private bool bAlreadyGranted = false;

    private PlayerEquipment _equipment;

    private void Start()
    {
        if (bAlreadyGranted) return;

        // 이어하기로 들어온 경우 세이브가 장비를 복원하므로 지급하지 않음
        if (GameManager.Instance != null && GameManager.Instance.IsNewGame == false)
        {
            bAlreadyGranted = true;
            return;
        }

        _equipment = GetComponent<PlayerEquipment>();
        // InventorySystem 슬롯 초기화가 끝난 뒤 지급하도록 한 프레임 대기
        StartCoroutine(GrantNextFrame());
    }

    private System.Collections.IEnumerator GrantNextFrame()
    {
        yield return null;
        GrantStartingWeapons();
        bAlreadyGranted = true;
    }

    private void GrantStartingWeapons()
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[StartingEquipment] InventorySystem 없음 — 지급 실패");
            return;
        }

        // 무기 3종 인벤토리에 추가
        foreach (WeaponData weaponData in startingWeapons)
        {
            if (weaponData == null) continue;
            inventory.AddItem(weaponData);
        }

        // 장검 장착 — 인벤토리에서 빼고 장착
        if (equipOnStart != null && _equipment != null)
        {
            inventory.RemoveItem(equipOnStart);
            _equipment.EquipItem(new WeaponInstance(equipOnStart));
            Debug.Log($"[StartingEquipment] 시작 무기 장착: {equipOnStart.ItemName}");
        }
    }
}
