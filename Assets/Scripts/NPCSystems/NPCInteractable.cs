// NPCInteractable.cs
// 마을 NPC 오브젝트에 부착 — 플레이어 접근 시 E키로 시스템 패널을 열거나 대화를 시작한다.
// Collider(isTrigger=true) 필수, NPC 타입에 맞는 시스템 참조를 인스펙터에서 연결한다.

using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    // NPC 역할 구분
    public enum NPCType
    {
        Merchant,           // 상인
        Scholar,            // 학자
        Blacksmith,         // 대장장이
        InscriptionMaster,  // 각인술사
        Background          // 배경 NPC (주민, 예언가, 종말론자, 기사)
    }

    [Header("NPC 설정")]
    [SerializeField] private NPCType npcType = NPCType.Background;
    [SerializeField] private string  npcName = "마을 주민";

    [Header("주요 NPC 시스템 연결")]
    [Tooltip("NPCType.Merchant 일 때 연결")]
    [SerializeField] private ShopSystem              shopSystem;
    [Tooltip("NPCType.Scholar 일 때 연결")]
    [SerializeField] private ScholarSystem           scholarSystem;
    [Tooltip("NPCType.Blacksmith 일 때 연결")]
    [SerializeField] private BlacksmithSystem        blacksmithSystem;
    [Tooltip("NPCType.InscriptionMaster 일 때 연결")]
    [SerializeField] private InscriptionMasterSystem inscriptionMasterSystem;

    [Header("배경 NPC 대화 설정")]
    [Tooltip("NPCType.Background 일 때 사용. 대화 컴포넌트를 이 오브젝트 또는 자식에 부착한다.")]
    [SerializeField] private NPCDialogue npcDialogue;
    [Tooltip("배경 NPC 대화 문장 목록")]
    [SerializeField] private string[] sentences = { "..." };

    [Header("UI 힌트")]
    [Tooltip("범위에 들어오면 켜질 안내 오브젝트 (예: E 상호작용 표시). 없으면 비워둠")]
    [SerializeField] private GameObject hintObject;

    private bool _playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = true;

        SetHintVisible(true);
        Debug.Log($"[NPCInteractable] {npcName} — E키로 상호작용");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = false;

        SetHintVisible(false);
    }

    private void Update()
    {
        if (_playerInRange == false)
        {
            return;
        }
        if (InputReader.Instance == null || InputReader.Instance.InteractPressed == false)
        {
            return;
        }

        SetHintVisible(false);
        OpenNPC();
    }

    // 힌트 표시 또는 숨김 (중복 호출 방지)
    private void SetHintVisible(bool visible)
    {
        if (hintObject == null)
        {
            return;
        }
        if (hintObject.activeSelf == visible)
        {
            return;
        }
        hintObject.SetActive(visible);
    }

    // NPC 타입에 따라 해당 시스템 열기
    private void OpenNPC()
    {
        switch (npcType)
        {
            case NPCType.Merchant:
                OpenMerchant();
                break;
            case NPCType.Scholar:
                OpenScholar();
                break;
            case NPCType.Blacksmith:
                OpenBlacksmith();
                break;
            case NPCType.InscriptionMaster:
                OpenInscriptionMaster();
                break;
            case NPCType.Background:
                OpenBackgroundDialogue();
                break;
        }
    }

    private void OpenMerchant()
    {
        if (shopSystem == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: ShopSystem 미연결");
            return;
        }
        shopSystem.OpenShop();
    }

    private void OpenScholar()
    {
        if (scholarSystem == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: ScholarSystem 미연결");
            return;
        }
        scholarSystem.OpenScholar();
    }

    private void OpenBlacksmith()
    {
        if (blacksmithSystem == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: BlacksmithSystem 미연결");
            return;
        }
        blacksmithSystem.OpenBlacksmith();
    }

    private void OpenInscriptionMaster()
    {
        if (inscriptionMasterSystem == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: InscriptionMasterSystem 미연결");
            return;
        }
        inscriptionMasterSystem.OpenInscriptionMaster();
    }

    private void OpenBackgroundDialogue()
    {
        if (npcDialogue == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: NPCDialogue 미연결");
            return;
        }

        // 인스펙터에서 설정한 문장 배열로 DialogueData 생성
        DialogueData dialogueData = new DialogueData
        {
            npcName   = npcName,
            sentences = sentences
        };

        npcDialogue.StartDialogue(dialogueData);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Trigger Collider와 별개로 씬 뷰에서 범위 확인용
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
#endif
}
