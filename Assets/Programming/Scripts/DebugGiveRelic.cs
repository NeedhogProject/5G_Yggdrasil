using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트용 유물 지급 도구 (임시).
/// 지정한 키를 누르면 연결된 유물을 인벤토리에 미감정 상태로 넣는다.
/// 학자 감정창 테스트가 끝나면 이 스크립트와 오브젝트를 삭제해도 된다.
///
/// [사용법]
/// 1. 씬의 아무 오브젝트에 이 스크립트를 부착
/// 2. Give Relic 칸에 RelicData 에셋 연결 (예: Relic/Attack_Rlelic/Attack_Relic-001)
/// 3. 플레이 중 지정 키(기본 F1)를 누르면 인벤토리에 미감정 유물이 들어옴
/// </summary>
public class DebugGiveRelic : MonoBehaviour
{
    [Header("지급할 유물 (RelicData 에셋 연결)")]
    [SerializeField] private RelicData giveRelic;

    [Header("지급 키 (기본 F1)")]
    [SerializeField] private Key giveKey = Key.F1;

    private void Update()
    {
        // 키보드가 없으면 무시
        if (Keyboard.current == null)
        {
            return;
        }

        // 지정 키가 이번 프레임에 눌렸을 때만 동작
        if (Keyboard.current[giveKey].wasPressedThisFrame == false)
        {
            return;
        }

        if (giveRelic == null)
        {
            Debug.LogWarning("[DebugGiveRelic] 지급할 유물(RelicData)이 연결되지 않음");
            return;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[DebugGiveRelic] InventorySystem 을 찾을 수 없음");
            return;
        }

        // 유물을 인벤토리에 추가 (내부에서 미감정 RelicInstance 로 생성됨)
        bool bAdded = InventorySystem.Instance.AddItem(giveRelic);

        if (bAdded == true)
        {
            Debug.Log("[DebugGiveRelic] 유물 지급 완료: " + giveRelic.ItemName + " (미감정)");
        }
        else
        {
            Debug.LogWarning("[DebugGiveRelic] 인벤토리가 가득 차 지급 실패");
        }
    }
}
