/*
 * PlayerInteractionLock.cs
 * NPC 상호작용 UI(대화창/각인/대장간 등)가 열려 있는 동안 플레이어 이동을 막는다.
 * PlayerController(정건희 담당)를 외부에서 잠시 비활성화하는 방식 — 그 파일은 수정하지 않는다.
 * 매 프레임 상태를 검사하므로 여러 UI가 겹쳐 열려도 꼬이지 않는다.
 * 사용: 타운 씬의 아무 오브젝트(예: GameCore, InscriptionLogic)에 이 컴포넌트를 붙인다.
 * 담당: 김보민
 */

using UnityEngine;

public class PlayerInteractionLock : MonoBehaviour
{
    // 직전 프레임에 이동을 잠갔는지 (상태 변할 때만 처리)
    private bool _lockedLastFrame = false;

    private void Update()
    {
        if (PlayerController.Instance == null)
        {
            return;
        }

        bool shouldLock = IsAnyInteractionOpen();

        // 상태가 바뀐 순간에만 처리
        if (shouldLock == _lockedLastFrame)
        {
            return;
        }

        _lockedLastFrame = shouldLock;

        // 잠금이면 컴포넌트 끄기, 아니면 켜기
        PlayerController.Instance.enabled = shouldLock == false;

        // 잠그는 순간 움직이던 속도도 멈춰서 미끄러짐 방지
        if (shouldLock == true)
        {
            StopPlayerMomentum();
        }
    }

    // 어떤 NPC 상호작용 UI라도 열려 있으면 true
    private bool IsAnyInteractionOpen()
    {
        // NPC 메뉴 대화창 (모든 NPC의 첫 메뉴가 이걸 사용)
        if (NPCDialogue.Instance != null && NPCDialogue.Instance.IsOpen == true)
        {
            return true;
        }

        // 대장장이 강화 패널
        if (BlacksmithSystem.Instance != null && BlacksmithSystem.Instance.IsOpen == true)
        {
            return true;
        }

        // 각인술사 패널
        if (InscriptionMasterSystem.Instance != null && InscriptionMasterSystem.Instance.IsOpen == true)
        {
            return true;
        }

        // 상인/학자 패널은 각 시스템에 공개 IsOpen 을 추가한 뒤 여기에 같은 형태로 더한다.
        // if (ShopSystem.Instance != null && ShopSystem.Instance.IsOpen == true) { return true; }
        // if (ScholarSystem.Instance != null && ScholarSystem.Instance.IsOpen == true) { return true; }

        return false;
    }

    // 플레이어의 이동 관성을 멈춘다 (Rigidbody 속도 0)
    private void StopPlayerMomentum()
    {
        Rigidbody body = PlayerController.Instance.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }
}