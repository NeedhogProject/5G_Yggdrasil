// NPCInteractable.cs
// 마을 NPC 오브젝트에 부착 — 플레이어 접근 시 E키로 시스템 패널을 열거나 대화를 시작한다.
// Collider(isTrigger=true) 필수, NPC 타입에 맞는 시스템 참조를 인스펙터에서 연결한다.
// 학자/대장장이/각인술사: 디자인 시안 형태의 메뉴형 대화창을 연다.
//   선택지 0 = 기능 패널 열기, 선택지 1 = 대화하기 (창 유지, 하단 대사만 순환)
// 상인은 자체 메뉴(ShopMenuPanel)가 있어 기존 동작 유지.

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
    [SerializeField] private string npcName = "마을 주민";

    [Header("주요 NPC 시스템 연결")]
    [Tooltip("NPCType.Merchant 일 때 연결")]
    [SerializeField] private ShopSystem shopSystem;
    [Tooltip("NPCType.Scholar 일 때 연결")]
    [SerializeField] private ScholarSystem scholarSystem;
    [Tooltip("NPCType.Blacksmith 일 때 연결")]
    [SerializeField] private BlacksmithSystem blacksmithSystem;
    [Tooltip("NPCType.InscriptionMaster 일 때 연결")]
    [SerializeField] private InscriptionMasterSystem inscriptionMasterSystem;

    [Header("대화 설정 (배경 NPC와 학자/대장장이/각인술사 공통)")]
    [Tooltip("비워두면 NPCDialogue.Instance 를 자동 사용 (권장)")]
    [SerializeField] private NPCDialogue npcDialogue;
    [Tooltip("대사 목록. 주요 NPC는 0번이 인사말, 대화하기 누를 때마다 다음 대사로 순환")]
    [SerializeField] private string[] sentences = { "..." };
    [Tooltip("대화창에 표시할 NPC 초상화 (선택)")]
    [SerializeField] private Sprite npcPortrait;

    [Header("주요 NPC 대화 흐름")]
    [Tooltip("켜면 학자/대장장이/각인술사가 메뉴형 대화창을 연다. 끄면 기능 패널 바로 열기")]
    [SerializeField] private bool useDialogueBeforeFunction = true;
    [Tooltip("기능 패널을 여는 선택지 문구 (예: 강화하기 / 유물 감정 / 각인하기)")]
    [SerializeField] private string functionChoiceLabel = "맡기기";
    [Tooltip("창을 유지한 채 대사를 순환하는 선택지 문구")]
    [SerializeField] private string talkChoiceLabel = "대화하기";

    [Header("상인(벨라) 선택지 문구")]
    [Tooltip("상인의 첫 번째 기능 선택지 (구매)")]
    [SerializeField] private string merchantBuyLabel = "구매";
    [Tooltip("상인의 두 번째 기능 선택지 (판매)")]
    [SerializeField] private string merchantSellLabel = "판매";

    [Header("각인술사 선택지 문구")]
    [Tooltip("각인술사의 첫 번째 기능 선택지 (각인)")]
    [SerializeField] private string inscriptionEngraveLabel = "장비 각인하기";
    [Tooltip("각인술사의 두 번째 기능 선택지 (각인 초기화)")]
    [SerializeField] private string inscriptionResetLabel = "각인 초기화하기";

    [Header("UI 힌트")]
    [Tooltip("범위에 들어오면 켜질 안내 오브젝트 (예: E 상호작용 표시). 없으면 비워둠")]
    [SerializeField] private GameObject hintObject;

    private bool _playerInRange = false;
    // 대화하기 순환용 현재 대사 번호
    private int _talkLineIndex = 0;

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

        // 대화창이 이미 열려있으면 E키 무시 (대화 중 재시작 방지)
        NPCDialogue dialogue = ResolveDialogue();
        if (dialogue != null && dialogue.IsOpen == true)
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
        // 상점 열기 동작 (구매/판매 둘 다 상점 패널을 연다. 탭은 패널 안에서 전환)
        System.Action openShop = delegate ()
        {
            if (shopSystem == null)
            {
                Debug.LogWarning($"[NPCInteractable] {npcName}: ShopSystem 미연결");
                return;
            }
            shopSystem.OpenShop();
        };

        // 구매 / 판매 두 선택지 모두 상점을 연다
        string[] labels = new string[] { merchantBuyLabel, merchantSellLabel };
        System.Action[] actions = new System.Action[] { openShop, openShop };

        if (TryStartMenuDialogue(labels, actions) == true)
        {
            return;
        }

        // 대화 흐름을 못 쓰면 기존처럼 바로 상점 열기
        openShop();
    }

    private void OpenScholar()
    {
        // 기능 시스템이 아직 없어도 대화창은 먼저 띄운다.
        // 기능 선택지를 눌렀을 때 시스템이 없으면 그때 경고한다.
        System.Action openFunction = delegate ()
        {
            if (scholarSystem == null)
            {
                Debug.LogWarning($"[NPCInteractable] {npcName}: ScholarSystem 미연결");
                return;
            }
            scholarSystem.OpenScholar();
        };

        string[] labels = new string[] { functionChoiceLabel };
        System.Action[] actions = new System.Action[] { openFunction };

        if (TryStartMenuDialogue(labels, actions) == true)
        {
            return;
        }

        // 대화 흐름을 못 쓰는 경우(설정 꺼짐 등) 기존처럼 바로 기능 열기 시도
        openFunction();
    }

    private void OpenBlacksmith()
    {
        System.Action openFunction = delegate ()
        {
            if (blacksmithSystem == null)
            {
                Debug.LogWarning($"[NPCInteractable] {npcName}: BlacksmithSystem 미연결");
                return;
            }
            blacksmithSystem.OpenBlacksmith();
        };

        string[] labels = new string[] { functionChoiceLabel };
        System.Action[] actions = new System.Action[] { openFunction };

        if (TryStartMenuDialogue(labels, actions) == true)
        {
            return;
        }

        openFunction();
    }

    private void OpenInscriptionMaster()
    {
        // 각인하기 / 각인 초기화하기 둘 다 각인 패널을 연다.
        // (패널 내부에 각인 기능과 초기화 버튼이 함께 있으므로 같은 패널을 연다)
        System.Action openFunction = delegate ()
        {
            if (inscriptionMasterSystem == null)
            {
                Debug.LogWarning($"[NPCInteractable] {npcName}: InscriptionMasterSystem 미연결");
                return;
            }
            inscriptionMasterSystem.OpenInscriptionMaster();
        };

        string[] labels = new string[] { inscriptionEngraveLabel, inscriptionResetLabel };
        System.Action[] actions = new System.Action[] { openFunction, openFunction };

        if (TryStartMenuDialogue(labels, actions) == true)
        {
            return;
        }

        openFunction();
    }

    private void OpenBackgroundDialogue()
    {
        NPCDialogue dialogue = ResolveDialogue();
        if (dialogue == null)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: NPCDialogue 미연결");
            return;
        }

        // 인스펙터에서 설정한 문장 배열로 DialogueData 생성
        DialogueData dialogueData = new DialogueData();
        dialogueData.npcName = npcName;
        dialogueData.npcPortrait = npcPortrait;
        dialogueData.sentences = sentences;

        dialogue.StartDialogue(dialogueData);
    }

    // 주요 NPC 공통: 메뉴형 대화창 열기 (N개 기능 선택지 + 대화하기)
    // 인사말(0번 대사) + 선택지 동시 표시
    // 기능 선택지들 = functionLabels 순서대로, 맨 뒤에 "대화하기" 자동 추가
    // 기능 선택 = 대화창 닫고 해당 동작 실행, 대화하기 = 다음 대사로 순환 (창 유지)
    // 대화 시작에 성공하면 true 반환 (호출 측은 기능 직접 열기를 건너뜀)
    private bool TryStartMenuDialogue(string[] functionLabels, System.Action[] functionActions)
    {
        if (useDialogueBeforeFunction == false)
        {
            return false;
        }
        if (functionLabels == null || functionActions == null)
        {
            return false;
        }
        if (functionLabels.Length != functionActions.Length)
        {
            Debug.LogWarning($"[NPCInteractable] {npcName}: 선택지 라벨과 동작 개수가 다름");
            return false;
        }

        NPCDialogue dialogue = ResolveDialogue();
        if (dialogue == null)
        {
            return false;
        }
        if (sentences == null || sentences.Length == 0)
        {
            return false;
        }

        _talkLineIndex = 0;

        // 기능 선택지들 뒤에 대화하기를 한 개 더 붙인다
        int functionCount = functionLabels.Length;
        string[] allChoices = new string[functionCount + 1];
        for (int i = 0; i < functionCount; i++)
        {
            allChoices[i] = functionLabels[i];
        }
        allChoices[functionCount] = talkChoiceLabel;

        // 콜백에서 참조할 동작 배열을 지역 변수로 보관 (대화 닫혀도 안전)
        System.Action[] actions = functionActions;
        int talkChoiceIndex = functionCount;

        DialogueData dialogueData = new DialogueData();
        dialogueData.npcName = npcName;
        dialogueData.npcPortrait = npcPortrait;
        dialogueData.sentences = new string[] { sentences[0] };
        dialogueData.choices = allChoices;
        dialogueData.showChoicesImmediately = true;
        dialogueData.endOnChoice = false;
        dialogueData.onChoiceSelected = delegate (int index)
        {
            // 대화하기 선택: 창 유지, 다음 대사로 순환
            if (index == talkChoiceIndex)
            {
                _talkLineIndex = _talkLineIndex + 1;
                if (_talkLineIndex >= sentences.Length)
                {
                    _talkLineIndex = 0;
                }
                dialogue.ShowLine(sentences[_talkLineIndex]);
                return;
            }

            // 기능 선택: 대화창을 먼저 닫고 해당 동작을 실행한다
            if (index >= 0 && index < actions.Length)
            {
                dialogue.EndDialogue();
                if (actions[index] != null)
                {
                    actions[index]();
                }
            }
        };

        dialogue.StartDialogue(dialogueData);
        return true;
    }

    // 인스펙터 연결이 있으면 그것을, 없으면 공용 싱글톤을 사용
    private NPCDialogue ResolveDialogue()
    {
        if (npcDialogue != null)
        {
            return npcDialogue;
        }

        // 싱글톤이 등록돼 있으면 사용
        if (NPCDialogue.Instance != null)
        {
            return NPCDialogue.Instance;
        }

        // 싱글톤이 비어있으면 현재 씬에서 직접 찾는다 (마을 전용 대화창 대비)
        NPCDialogue found = FindFirstObjectByType<NPCDialogue>();
        return found;
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