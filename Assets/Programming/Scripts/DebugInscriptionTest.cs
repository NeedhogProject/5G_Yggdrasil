using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트용 각인 재료 지급 도구 (임시).
/// 각인술사 / 대장장이 / 상점 테스트에 필요한 자원, 달란, 방어구, 초기화권을 단축키로 지급한다.
/// 테스트가 끝나면 이 스크립트와 오브젝트를 삭제해도 된다.
///
/// [사용법]
/// 1. 씬의 아무 오브젝트에 이 스크립트를 부착 (DebugGiveRelic 과 같은 오브젝트도 가능)
/// 2. Give Armors 칸에 방어구(ArmorData) 에셋들을, Reset Scroll 칸에 초기화 주문서(ConsumableData) 에셋을 연결
/// 3. 플레이 중 아래 키를 누른다 (키는 인스펙터에서 바꿀 수 있음)
///    F2: 각인 자원 5종을 각각 지정 수량만큼 추가
///    F3: 달란 추가
///    F4: 연결한 방어구 전부 지급
///    F5: 초기화 주문서 1개 지급
/// 4. 각인술사 창이 열려 있을 때 지급했다면 창을 한 번 닫았다 다시 열어야 숫자가 갱신된다
/// </summary>
public class DebugInscriptionTest : MonoBehaviour
{
    [Header("자원 지급 (각인 재료 5종)")]
    [SerializeField] private Key resourceKey = Key.F2;
    [SerializeField] private int resourceAmount = 10;

    [Header("달란 지급")]
    [SerializeField] private Key goldKey = Key.F3;
    [SerializeField] private int goldAmount = 1000;

    [Header("방어구 지급 (ArmorData 에셋 연결)")]
    [SerializeField] private Key armorKey = Key.F4;
    [SerializeField] private ArmorData[] giveArmors;

    [Header("초기화 주문서 지급 (ConsumableData 에셋 연결)")]
    [SerializeField] private Key resetScrollKey = Key.F5;
    [SerializeField] private ConsumableData resetScroll;

    private void Update()
    {
        // 키보드가 없으면 무시
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[resourceKey].wasPressedThisFrame == true)
        {
            GiveResources();
        }

        if (Keyboard.current[goldKey].wasPressedThisFrame == true)
        {
            GiveGold();
        }

        if (Keyboard.current[armorKey].wasPressedThisFrame == true)
        {
            GiveArmors();
        }

        if (Keyboard.current[resetScrollKey].wasPressedThisFrame == true)
        {
            GiveResetScroll();
        }
    }

    // 각인 자원 5종을 각각 resourceAmount 만큼 추가 (최대 99)
    private void GiveResources()
    {
        if (ResourceInventory.Instance == null)
        {
            Debug.LogWarning("[DebugInscriptionTest] ResourceInventory 를 찾을 수 없음");
            return;
        }

        ResourceInventory.Instance.AddResource(InscriptionType.Fire, resourceAmount);
        ResourceInventory.Instance.AddResource(InscriptionType.Water, resourceAmount);
        ResourceInventory.Instance.AddResource(InscriptionType.Wind, resourceAmount);
        ResourceInventory.Instance.AddResource(InscriptionType.Earth, resourceAmount);
        ResourceInventory.Instance.AddResource(InscriptionType.Darkness, resourceAmount);

        // 인벤토리 각인재료 칸이 열려 있으면 바로 갱신
        ResourceInventoryPanel[] panels = FindObjectsByType<ResourceInventoryPanel>(FindObjectsSortMode.None);
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].Refresh();
        }

        Debug.Log("[DebugInscriptionTest] 각인 자원 5종 +" + resourceAmount + " 지급");
    }

    // 달란 추가
    private void GiveGold()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("[DebugInscriptionTest] PlayerStats 를 찾을 수 없음");
            return;
        }

        PlayerStats.Instance.gold = PlayerStats.Instance.gold + goldAmount;
        Debug.Log("[DebugInscriptionTest] 달란 +" + goldAmount + " (현재 " + PlayerStats.Instance.gold + ")");
    }

    // 연결한 방어구를 전부 인벤토리에 지급
    private void GiveArmors()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[DebugInscriptionTest] InventorySystem 을 찾을 수 없음");
            return;
        }
        if (giveArmors == null || giveArmors.Length == 0)
        {
            Debug.LogWarning("[DebugInscriptionTest] 지급할 방어구(ArmorData)가 연결되지 않음");
            return;
        }

        for (int i = 0; i < giveArmors.Length; i++)
        {
            if (giveArmors[i] == null)
            {
                continue;
            }

            bool bAdded = InventorySystem.Instance.AddItem(giveArmors[i]);
            if (bAdded == true)
            {
                Debug.Log("[DebugInscriptionTest] 방어구 지급: " + giveArmors[i].itemName);
            }
            else
            {
                Debug.LogWarning("[DebugInscriptionTest] 인벤토리가 가득 차 지급 실패: " + giveArmors[i].itemName);
            }
        }
    }

    // 초기화 주문서 1개 지급
    private void GiveResetScroll()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[DebugInscriptionTest] InventorySystem 을 찾을 수 없음");
            return;
        }
        if (resetScroll == null)
        {
            Debug.LogWarning("[DebugInscriptionTest] 초기화 주문서(ConsumableData)가 연결되지 않음");
            return;
        }

        bool bAdded = InventorySystem.Instance.AddItem(resetScroll);
        if (bAdded == true)
        {
            Debug.Log("[DebugInscriptionTest] 초기화 주문서 지급");
        }
        else
        {
            Debug.LogWarning("[DebugInscriptionTest] 인벤토리가 가득 차 지급 실패");
        }
    }
}
