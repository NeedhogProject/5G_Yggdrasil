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
        // InventorySystem 슬롯 초기화가 끝난 뒤 지급하도록 대기
        StartCoroutine(GrantNextFrame());
    }

    private System.Collections.IEnumerator GrantNextFrame()
    {
        // 인스턴스 등록(Awake)과 슬롯 생성(Start)이 모두 끝난 뒤 지급 - 최소 1프레임, 최대 10프레임 대기
        int nWaitFrames = 0;
        while (nWaitFrames < 10)
        {
            yield return null;
            nWaitFrames = nWaitFrames + 1;

            if (InventorySystem.Instance != null && InventorySystem.Instance.slots.Count > 0)
            {
                break;
            }
        }

        GrantStartingWeapons();
        bAlreadyGranted = true;
    }

    private void GrantStartingWeapons()
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            // 원인 진단: 비활성 오브젝트까지 포함해 씬에 존재하는지 확인
            InventorySystem hidden = FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);

            if (hidden != null && hidden.gameObject.activeInHierarchy == false)
            {
                Debug.LogWarning("[StartingEquipment] 지급 실패 - InventorySystem 이 비활성 오브젝트에 있음: '"
                    + hidden.gameObject.name + "'. 오브젝트가 비활성이면 Awake 가 돌지 않아 싱글턴 등록이 안 됨."
                    + " 캔버스 루트는 켜두고 자식 패널만 꺼야 함");
            }
            else if (hidden != null)
            {
                Debug.LogWarning("[StartingEquipment] 지급 실패 - InventorySystem 오브젝트('" + hidden.gameObject.name
                    + "')는 활성인데 Instance 미등록. Awake 에서 자기 파괴됐는지 위쪽 '중복 감지' 로그 확인");
            }
            else
            {
                Debug.LogWarning("[StartingEquipment] 지급 실패 - 씬에 InventorySystem 오브젝트 자체가 없음."
                    + " 이 씬에 Inventory_Canvas 가 배치돼 있는지 확인");
            }
            return;
        }

        if (inventory.slots.Count == 0)
        {
            Debug.LogWarning("[StartingEquipment] 지급 실패 - 인벤토리 슬롯이 아직 생성되지 않음 (InventorySystem.Start 미실행)");
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
