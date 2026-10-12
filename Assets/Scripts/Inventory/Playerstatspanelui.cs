// PlayerStatsPanelUI.cs
// 인벤토리 우측 상단 플레이어 스탯 정보창.
// 체력 / 정신력 / 방어력 / 공격력(장착 무기) / 골드 를 표시한다.
// 패널이 켜질 때와 스탯·장비가 바뀔 때 자동으로 갱신한다.
// PlayerStats, PlayerEquipment 는 읽기만 하며 수정하지 않는다.
// 담당: 김보민

using UnityEngine;
using TMPro;

public class PlayerStatsPanelUI : MonoBehaviour
{
    [Header("스탯 표시 텍스트 (필요한 것만 연결)")]
    [Tooltip("체력 숫자 (예: 85 / 100)")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("정신력 숫자 (예: 70 / 100)")]
    [SerializeField] private TMP_Text mentalText;

    [Tooltip("방어력 숫자")]
    [SerializeField] private TMP_Text defenseText;

    [Tooltip("공격력 숫자 (장착 무기 기준, 없으면 0)")]
    [SerializeField] private TMP_Text attackText;

    [Tooltip("골드 숫자")]
    [SerializeField] private TMP_Text goldText;

    [Header("참조 (비우면 자동 탐색)")]
    [SerializeField] private PlayerEquipment playerEquipment;

    private PlayerStats _stats;

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // 참조 확보 (씬 전환 등으로 끊길 수 있어 켜질 때마다 확인)
    private void ResolveReferences()
    {
        _stats = PlayerStats.Instance;

        if (playerEquipment == null)
        {
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        }
    }

    // 스탯/장비 변경 이벤트 구독 (중복 구독 방지 위해 먼저 해제 후 구독)
    private void Subscribe()
    {
        Unsubscribe();

        if (_stats != null)
        {
            _stats.OnStatsChanged += Refresh;
            _stats.OnGoldChanged += OnGoldChanged;
        }

        if (playerEquipment != null)
        {
            playerEquipment.OnEquipmentChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (_stats != null)
        {
            _stats.OnStatsChanged -= Refresh;
            _stats.OnGoldChanged -= OnGoldChanged;
        }

        if (playerEquipment != null)
        {
            playerEquipment.OnEquipmentChanged -= Refresh;
        }
    }

    // 골드 변경 이벤트는 int 를 받으므로 래핑해서 Refresh 호출
    private void OnGoldChanged(int newGold)
    {
        Refresh();
    }

    // 모든 스탯 텍스트 갱신
    private void Refresh()
    {
        if (_stats == null)
        {
            _stats = PlayerStats.Instance;
        }
        if (_stats == null)
        {
            return;
        }

        // 체력 / 정신력 (0~100, 최대치 함께 표시)
        if (healthText != null)
        {
            healthText.text = _stats.Health.ToString("F0") + " / 100";
        }
        if (mentalText != null)
        {
            mentalText.text = _stats.Mental.ToString("F0") + " / 100";
        }

        // 방어력 (기본 + 장비)
        if (defenseText != null)
        {
            defenseText.text = _stats.Defense.ToString("F0");
        }

        // 공격력 (장착 무기 최종 공격력, 없으면 0)
        if (attackText != null)
        {
            float attack = GetEquippedAttack();
            attackText.text = attack.ToString("F0");
        }

        // 골드
        if (goldText != null)
        {
            goldText.text = _stats.gold.ToString();
        }
    }

    // 장착한 무기의 최종 공격력 (강화 반영), 없으면 0
    private float GetEquippedAttack()
    {
        if (playerEquipment == null)
        {
            return 0f;
        }

        WeaponInstance weapon = playerEquipment.EquippedWeapon;
        if (weapon == null)
        {
            return 0f;
        }

        return weapon.FinalDamage;
    }
}