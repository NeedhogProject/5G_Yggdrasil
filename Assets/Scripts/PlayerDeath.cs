using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 사망 처리 시스템
///
/// [기획 반영]
/// - 사망 패널티: 인벤토리 전체 삭제 (드롭 없음)
/// - 장착 장비도 전부 해제 및 삭제
/// - 사망 시 "다시하기 / 종료" UI 표시
/// - 다시하기: 체력/정신력 완전 회복 → 마을 복귀
/// - 종료: 게임 종료 (Application.Quit)
/// - 사망 연출: 보류 (추후 추가)
///
/// [연동]
/// - PlayerStats.OnHealthChanged 구독 → 체력 0 감지
/// - GameManager.OnPlayerDeath() 호출
/// - PlayerEquipment.UnequipAll() 로 장비 해제
/// - InventorySystem 완성 후 인벤 삭제 연동
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    // ─────────────────────── 참조 ───────────────────────

    [Header("참조")]
    [SerializeField] private PlayerStats     playerStats;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerCombat    playerCombat;
    [SerializeField] private PlayerController playerController;

    [Header("사망 UI (GameOverPanel)")]
    [Tooltip("다시하기/종료 버튼이 있는 UI 패널")]
    [SerializeField] private GameObject gameOverPanel;

    // ─────────────────────── 상태 ───────────────────────

    public bool IsDead { get; private set; } = false;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        if (playerStats      == null) playerStats      = GetComponent<PlayerStats>();
        if (playerEquipment  == null) playerEquipment  = GetComponent<PlayerEquipment>();
        if (playerCombat     == null) playerCombat     = GetComponent<PlayerCombat>();
        if (playerController == null) playerController = GetComponent<PlayerController>();

        // 체력 변화 이벤트 구독
        if (playerStats != null)
            playerStats.OnHealthChanged += OnHealthChanged;

        // 사망 UI 초기 숨김
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= OnHealthChanged;
    }

    // ─────────────────────── 체력 감지 ───────────────────────

    private void OnHealthChanged(float currentHealth)
    {
        if (IsDead) return;
        if (currentHealth <= 0f) Die();
    }

    // ─────────────────────── 사망 처리 ───────────────────────

    private void Die()
    {
        if (IsDead) return;
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
        // var inventory = GetComponent<InventorySystem>();
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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 마을 복귀
        GameManager.Instance?.ReturnToTown();
        Debug.Log("[PlayerDeath] 다시하기 → 마을 복귀");
    }

    /// <summary>
    /// 종료 버튼 클릭 — UI 버튼 OnClick 에 연결
    /// </summary>
    public void OnQuitClicked()
    {
        Time.timeScale = 1f;
        Debug.Log("[PlayerDeath] 게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────── 입력 비활성화 ───────────────────────

    private void DisablePlayerInput()
    {
        if (playerController != null) playerController.enabled = false;
        if (playerCombat     != null) playerCombat.enabled     = false;
    }

    private void EnablePlayerInput()
    {
        if (playerController != null) playerController.enabled = true;
        if (playerCombat     != null) playerCombat.enabled     = true;
    }
}
