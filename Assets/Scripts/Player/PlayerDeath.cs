using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 사망 처리 시스템
///
/// [기획 반영]
/// - 사망 패널티: 인벤토리 전체 삭제 (드롭 없음)
/// - 장착 장비도 전부 해제 및 삭제
/// - 사망 시 "다시하기 / 종료" UI 표시
/// - 다시하기: 체력/정신력 완전 회복 → 마을 복귀
/// - 종료(메인화면): 타이틀 화면으로 이동 (GameManager.GoToTitle)
/// - 사망 연출: 보류 (추후 추가)
///
/// [연동]
/// - PlayerStats.OnHealthChanged 구독 → 체력 0 감지
/// - GameManager.OnPlayerDeath() 호출
/// - PlayerEquipment.UnequipAll() 로 장비 해제
/// - InventorySystem 완성 후 인벤 삭제 연동
///
/// [GameOverPanel 자동 탐색]
/// - 인스펙터 연결이 비어 있으면 런타임에 이름으로 패널을 찾는다.
/// - 패널이 PersistentCanvas 안에서 평소 꺼져 있으므로 비활성 포함 검색을 사용한다.
/// - 던전 1~4층 어디서 죽어도 패널 하나로 처리하기 위함이다.
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    // ─────────────────────── 참조 ───────────────────────

    [Header("참조")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerController playerController;

    [Header("사망 UI (GameOverPanel)")]
    [Tooltip("다시하기/종료 버튼이 있는 UI 패널. 비워두면 이름으로 자동 검색한다.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("자동 검색에 사용할 패널 오브젝트 이름")]
    [SerializeField] private string gameOverPanelName = "GameOverPanel";

    [Tooltip("다시하기 버튼 오브젝트 이름 (자동 연결용)")]
    [SerializeField] private string retryButtonName = "RetryButton";

    [Tooltip("종료 버튼 오브젝트 이름 (자동 연결용)")]
    [SerializeField] private string quitButtonName = "QuitButton";

    // 버튼 콜백 중복 등록 방지 플래그
    private bool buttonsHooked = false;

    // ─────────────────────── 상태 ───────────────────────

    public bool IsDead { get; private set; } = false;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }
        if (playerEquipment == null)
        {
            playerEquipment = GetComponent<PlayerEquipment>();
        }
        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
        }
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        // 체력 변화 이벤트 구독
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += OnHealthChanged;
        }

        // 사망 UI 초기 숨김
        ResolveGameOverPanel();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= OnHealthChanged;
        }
    }

    // ─────────────────────── GameOverPanel 자동 탐색 ───────────────────────

    // 인스펙터 연결이 없으면 이름으로 패널을 찾는다.
    // 평소 꺼져 있는 패널도 찾기 위해 비활성 포함 검색을 사용한다.
    private void ResolveGameOverPanel()
    {
        // 이미 연결되어 있으면 버튼 연결만 확인하고 사용
        if (gameOverPanel != null)
        {
            HookButtons();
            return;
        }

        if (string.IsNullOrEmpty(gameOverPanelName) == true)
        {
            return;
        }

        // 비활성 오브젝트까지 포함해 모든 Transform 을 검색한다.
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current.name != gameOverPanelName)
            {
                continue;
            }

            // 씬에 배치된 오브젝트만 사용한다 (프리팹 에셋 원본 제외)
            if (current.gameObject.scene.IsValid() == false)
            {
                continue;
            }

            gameOverPanel = current.gameObject;
            HookButtons();
            return;
        }

        Debug.LogWarning("[PlayerDeath] GameOverPanel 을 찾지 못했습니다. 이름 확인 필요: " + gameOverPanelName);
    }

    // 패널 자식에서 다시하기/종료 버튼을 찾아 콜백을 코드로 연결한다.
    // 씬과 프리팹이 분리되어 인스펙터 직접 연결이 불가능한 경우를 우회한다.
    private void HookButtons()
    {
        if (buttonsHooked == true)
        {
            return;
        }
        if (gameOverPanel == null)
        {
            return;
        }

        // 비활성 자식까지 포함해 버튼을 찾는다.
        Button[] buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button.name == retryButtonName)
            {
                button.onClick.RemoveListener(OnRetryClicked);
                button.onClick.AddListener(OnRetryClicked);
            }
            else if (button.name == quitButtonName)
            {
                button.onClick.RemoveListener(OnQuitClicked);
                button.onClick.AddListener(OnQuitClicked);
            }
        }

        buttonsHooked = true;
    }

    // ─────────────────────── 체력 감지 ───────────────────────

    private void OnHealthChanged(float currentHealth)
    {
        if (IsDead == true)
        {
            return;
        }
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // ─────────────────────── 사망 처리 ───────────────────────

    private void Die()
    {
        if (IsDead == true)
        {
            return;
        }
        IsDead = true;

        Debug.Log("[PlayerDeath] 플레이어 사망");

        // 입력 비활성화
        DisablePlayerInput();

        // 사망 패널티: 인벤토리 전체 삭제
        ApplyDeathPenalty();

        // GameManager 에 사망 알림
        GameManager.Instance?.OnPlayerDeath();

        // 사망 UI 표시
        ShowGameOverUI();

        // TODO: 사망 연출 (애니메이션/페이드 등) — 보류
    }

    // ─────────────────────── 사망 패널티 ───────────────────────

    private void ApplyDeathPenalty()
    {
        // 장착 장비 전부 해제 및 삭제
        if (playerEquipment != null)
        {
            List<ItemInstance> droppedItems = playerEquipment.UnequipAll();
            Debug.Log($"[PlayerDeath] 장착 장비 {droppedItems.Count}개 삭제");
        }

        // 인벤토리 전체 삭제
        // InventorySystem 완성 후 주석 해제
        // InventorySystem inventory = GetComponent<InventorySystem>();
        // if (inventory != null)
        // {
        //     inventory.ClearAll();
        //     Debug.Log("[PlayerDeath] 인벤토리 전체 삭제");
        // }

        Debug.Log("[PlayerDeath] 사망 패널티 적용 완료 (인벤 삭제 — InventorySystem 연동 후 활성화)");
    }

    // ─────────────────────── UI ───────────────────────

    private void ShowGameOverUI()
    {
        // 표시 직전에 한 번 더 패널을 확인한다 (씬 전환 후 대비).
        ResolveGameOverPanel();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; // 게임 일시정지
        }
        else
        {
            // GameOverPanel 미설정 시 임시: 3초 후 자동 마을 복귀
            Debug.LogWarning("[PlayerDeath] GameOverPanel 미설정 — 3초 후 자동 복귀");
            Invoke(nameof(OnRetryClicked), 3f);
        }
    }

    // ─────────────────────── 버튼 콜백 (UI 버튼에 연결) ───────────────────────

    /// <summary>
    /// 다시하기 버튼 클릭 — UI 버튼 OnClick 에 연결
    /// 체력/정신력 완전 회복 → 마을 복귀
    /// </summary>
    public void OnRetryClicked()
    {
        Time.timeScale = 1f;

        // 체력/정신력 완전 회복
        if (playerStats != null)
        {
            playerStats.ModifyHealth(100f);
            playerStats.ModifyMental(100f);
        }

        IsDead = false;

        // 입력 다시 활성화
        EnablePlayerInput();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 집에서 부활하며 마을 복귀
        GameManager.Instance?.RespawnAtHome();
        Debug.Log("[PlayerDeath] 다시하기 → 집에서 부활");
    }

    /// <summary>
    /// 메인화면(타이틀) 버튼 클릭 — UI 버튼 OnClick 에 연결
    /// 게임을 끄지 않고 타이틀 화면으로 이동한다.
    /// </summary>
    public void OnQuitClicked()
    {
        Time.timeScale = 1f;
        IsDead = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Debug.Log("[PlayerDeath] 메인화면 → 타이틀 이동");
        GameManager.Instance?.GoToTitle();
    }

    // ─────────────────────── 입력 비활성화 ───────────────────────

    private void DisablePlayerInput()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
        }
    }

    private void EnablePlayerInput()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        if (playerCombat != null)
        {
            playerCombat.enabled = true;
        }
    }
}