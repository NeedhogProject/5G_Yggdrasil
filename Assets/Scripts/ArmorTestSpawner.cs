// ArmorTestSpawner.cs
// [임시 디버그] 각인 시스템 테스트용.
//   F9  = 지정한 테스트 방어구들을 인벤토리에 추가
//   F10 = 모든 원소 자원 +50, 골드 +99999, 각인 초기화권 +1
//   F11 = 각인 초기화 화면 바로 열기 (NPC 메뉴 없이 테스트)
// 테스트가 끝나면 이 스크립트와 붙인 오브젝트를 삭제한다.
// 담당: 김보민 (임시)

using UnityEngine;
using UnityEngine.InputSystem;

public class ArmorTestSpawner : MonoBehaviour
{
    [Header("인벤토리에 추가할 테스트 방어구 (ArmorData 에셋 드래그)")]
    [SerializeField] private ArmorData[] testArmors;

    [Header("각인 초기화권 (ConsumableData — ResetScroll 에셋 드래그)")]
    [SerializeField] private ConsumableData resetScroll;

    [Header("F10 지급량")]
    [SerializeField] private int resourceAmount = 50;
    [SerializeField] private int goldAmount = 99999;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame == true)
        {
            SpawnTestArmors();
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame == true)
        {
            GiveResourcesAndGold();
        }

        if (Keyboard.current.f11Key.wasPressedThisFrame == true)
        {
            OpenResetScreenForTest();
        }
    }

    // 지정한 방어구들을 인벤토리에 인스턴스로 추가
    private void SpawnTestArmors()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[ArmorTestSpawner] InventorySystem 이 없습니다.");
            return;
        }
        if (testArmors == null || testArmors.Length == 0)
        {
            Debug.LogWarning("[ArmorTestSpawner] testArmors 에 ArmorData 를 넣어주세요.");
            return;
        }

        for (int i = 0; i < testArmors.Length; i++)
        {
            if (testArmors[i] == null)
            {
                continue;
            }

            ArmorInstance instance = new ArmorInstance(testArmors[i]);
            InventorySystem.Instance.AddItem(instance);
            Debug.Log("[ArmorTestSpawner] 테스트 방어구 추가: " + testArmors[i].itemName);
        }
    }

    // 모든 원소 자원, 골드, 초기화권을 테스트용으로 지급
    private void GiveResourcesAndGold()
    {
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.AddResource(InscriptionType.Fire, resourceAmount);
            ResourceInventory.Instance.AddResource(InscriptionType.Water, resourceAmount);
            ResourceInventory.Instance.AddResource(InscriptionType.Wind, resourceAmount);
            ResourceInventory.Instance.AddResource(InscriptionType.Earth, resourceAmount);
            ResourceInventory.Instance.AddResource(InscriptionType.Darkness, resourceAmount);
            Debug.Log("[ArmorTestSpawner] 자원 +" + resourceAmount.ToString() + " 지급");
        }
        else
        {
            Debug.LogWarning("[ArmorTestSpawner] ResourceInventory 가 없습니다.");
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.gold = PlayerStats.Instance.gold + goldAmount;
            Debug.Log("[ArmorTestSpawner] 골드 +" + goldAmount.ToString() + " 지급");
        }
        else
        {
            Debug.LogWarning("[ArmorTestSpawner] PlayerStats 가 없습니다.");
        }

        if (resetScroll != null && InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(resetScroll);
            Debug.Log("[ArmorTestSpawner] 각인 초기화권 +1 지급");
        }
        else
        {
            Debug.LogWarning("[ArmorTestSpawner] resetScroll 에 ResetScroll ConsumableData 를 넣어주세요.");
        }
    }

    // 각인 초기화 화면 바로 열기 (테스트용)
    private void OpenResetScreenForTest()
    {
        if (InscriptionMasterSystem.Instance != null)
        {
            InscriptionMasterSystem.Instance.OpenResetScreen();
            Debug.Log("[ArmorTestSpawner] 초기화 화면 열기");
        }
        else
        {
            Debug.LogWarning("[ArmorTestSpawner] InscriptionMasterSystem 이 없습니다.");
        }
    }
}