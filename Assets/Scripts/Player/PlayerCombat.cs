using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 전투 시스템
///
/// [기획 반영]
/// - 공격 버튼: 마우스 좌클릭
/// - 공격 중 이동 가능
/// - 콤보 없음
/// - 기본 공격 속도 (초당 공격 횟수)
///   · 단검: 1.0 (1초에 1회)
///   · 장검: 0.5 (2초에 1회)
///   · 창  : 0.2 (5초에 1회)
/// - 공격력 = WeaponInstance.FinalDamage * PlayerStats.MentalMultiplier
/// - 세트 공격력/공격속도/흡혈 보너스는 ArmorSetManager 가 설정
///
/// [컴포넌트 설정]
/// - PlayerInput → Actions 에 Attack(Button) 액션 등록
/// - HitboxSystem, PlayerStats 같은 오브젝트에 부착
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : MonoBehaviour
{
    // ─────────────────────── 참조 ───────────────────────

    private HitboxSystem _hitbox;
    private Animator _animator;
    private PlayerStats _stats;

    // ─────────────────────── 장착 무기 ───────────────────────

    /// <summary>현재 장착 무기 — PlayerEquipment 에서 설정</summary>
    public WeaponInstance CurrentWeapon { get; private set; }

    // ─────────────────────── 공격 속도 ───────────────────────

    [Header("무기별 기본 공격 속도 (초당 공격 횟수)")]
    [SerializeField] private float daggerAttackSpeed = 1.0f;  // 1초에 1회
    [SerializeField] private float swordAttackSpeed = 0.5f;  // 2초에 1회
    [SerializeField] private float spearAttackSpeed = 0.2f;  // 5초에 1회

    // ─────────────────────── 세트 효과 보너스 ───────────────────────

    [Header("세트 효과 보너스 (런타임)")]
    [SerializeField] private float _attackSpeedBonus = 0f;   // 공격 속도 증가율
    [SerializeField] private float _attackDamageBonus = 0f;  // 공격력 증가율
    [SerializeField] private float _lifeStealPercent = 0f;   // 흡혈 비율 (입힌 데미지 비례 회복)

    /// <summary>공격 속도 보너스 설정 (ArmorSetManager 에서 호출)</summary>
    public void SetAttackSpeedBonus(float bonus) => _attackSpeedBonus = bonus;

    /// <summary>공격력 보너스 설정 (ArmorSetManager 에서 호출)</summary>
    public void SetAttackDamageBonus(float bonus) => _attackDamageBonus = bonus;

    /// <summary>흡혈 비율 설정 (ArmorSetManager 에서 호출. 0.03 = 데미지의 3% 회복)</summary>
    public void SetLifeStealPercent(float percent) => _lifeStealPercent = percent;

    // ─────────────────────── 런타임 디버그 ───────────────────────

    [Header("런타임 디버그 (읽기 전용)")]
    [SerializeField] private float _currentAttackInterval;  // 현재 공격 간격

    /// <summary>현재 무기의 공격 간격 (초) = 1 / 공격속도</summary>
    private float AttackInterval
    {
        get
        {
            if (CurrentWeapon?.WeaponData == null) return 1f;

            float speed = CurrentWeapon.WeaponData.WeaponType switch
            {
                WeaponType.Dagger => daggerAttackSpeed,
                WeaponType.Sword => swordAttackSpeed,
                WeaponType.Spear => spearAttackSpeed,
                _ => 1f
            };

            // 바람 세트 공격속도 보너스 반영
            speed *= (1f + _attackSpeedBonus);

            return speed > 0f ? 1f / speed : float.MaxValue;
        }
    }

    // ─────────────────────── 런타임 상태 ───────────────────────

    private float _attackCooldownTimer = 0f;
    private bool _isAttacking = false;

    /// <summary>공격 가능 여부 (쿨다운 + 무기 장착 확인)</summary>
    public bool CanAttack => _attackCooldownTimer <= 0f
                          && !_isAttacking
                          && CurrentWeapon != null;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        _hitbox = GetComponentInChildren<HitboxSystem>();
        _stats = GetComponent<PlayerStats>();
        _animator = GetComponentInChildren<Animator>();
    }

    // ─────────────────────── 업데이트 ───────────────────────

    private void Update()
    {
        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer -= Time.deltaTime;

        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // 공격 입력 — InputReader 에서 받음 (리바인딩 호환)
        if (InputReader.Instance != null && InputReader.Instance.AttackPressed)
        {
            if (pointerOverUI == false)
                TryAttackChecked();
        }

#if UNITY_EDITOR
        _currentAttackInterval = AttackInterval;
#endif
    }

    private void TryAttackChecked()
    {
        if (CanAttack == false)
        {
#if UNITY_EDITOR
            if (CurrentWeapon == null)
                Debug.LogWarning("[PlayerCombat] 무기가 장착되지 않음");
            else if (_attackCooldownTimer > 0f)
                Debug.Log($"[PlayerCombat] 쿨다운 중: {_attackCooldownTimer:F2}초 남음");
            else if (_isAttacking)
                Debug.Log("[PlayerCombat] 공격 중");
#endif
            return;
        }

        TryAttack();
    }

    // ─────────────────────── 공격 처리 ───────────────────────

    private void TryAttack()
    {
        if (CurrentWeapon?.WeaponData == null) return;

        _isAttacking = true;
        _attackCooldownTimer = AttackInterval;

        // 공격 애니메이션 트리거 (이번 학기 공통 모션, 다음 학기 무기별 분기 예정)
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // 공격 — 보호막 재충전 타이머 리셋 (비전투 판정)
        _stats?.NotifyCombatAction();

        // 판정 실행 — onHit 콜백으로 피격 처리
        _hitbox.PerformAttack(CurrentWeapon, OnHit);

        _isAttacking = false;
    }

    /// <summary>
    /// 피격 콜백 — HitboxSystem 에서 적 감지 시 호출
    /// hitIndex: 단검 다단히트 회차 (장검/창은 항상 0)
    /// </summary>
    private void OnHit(GameObject target, int hitIndex)
    {
        if (target == null) return;

        // 적 피격
        if (target.TryGetComponent<EnemyBase>(out EnemyBase enemy))
        {
            float damage = CalculateDamage(hitIndex);
            enemy.TakeDamage(damage);

            // 불 4세트 흡혈 — 입힌 데미지 비례 회복
            if (_lifeStealPercent > 0f && _stats != null)
            {
                _stats.ModifyHealth(damage * _lifeStealPercent);
            }
            return;
        }

        // 자원 노드 채집
        if (target.TryGetComponent<ResourceNode>(out ResourceNode resourceNode))
        {
            resourceNode.OnHit();
            return;
        }

#if UNITY_EDITOR
        Debug.LogWarning($"[PlayerCombat] {target.name} 피격 — 처리되지 않은 대상");
#endif
    }

    // ─────────────────────── 데미지 계산 ───────────────────────

    /// <summary>
    /// 최종 데미지 계산
    /// = 무기 공격력(강화 반영) × 정신력 배율 × 히트 배율(단검) × 세트 공격력 보너스
    /// </summary>
    private float CalculateDamage(int hitIndex)
    {
        if (CurrentWeapon == null) return 0f;

        float baseDamage = CurrentWeapon.FinalDamage;

        // 정신력 배율 (정신력 낮을수록 공격력 감소, 강도는 PlayerStats 인스펙터)
        float mentalMult = _stats != null ? _stats.MentalAttackMultiplier : 1f;

        // 단검 다단히트 배율
        float hitMult = CurrentWeapon.WeaponData.WeaponType == WeaponType.Dagger
            ? _hitbox.GetDaggerHitMultiplier(hitIndex)
            : 1f;

        // 세트 공격력 보너스
        float setBonus = 1f + _attackDamageBonus;

        return baseDamage * mentalMult * hitMult * setBonus;
    }

    // ─────────────────────── 무기 장착/해제 ───────────────────────

    /// <summary>무기 장착 (PlayerEquipment 에서 호출)</summary>
    public void EquipWeapon(WeaponInstance weapon)
    {
        CurrentWeapon = weapon;
        _attackCooldownTimer = 0f;

#if UNITY_EDITOR
        Debug.Log($"[PlayerCombat] 무기 장착: {weapon?.WeaponData?.ItemName ?? "없음"}");
#endif
    }

    /// <summary>무기 해제 (PlayerEquipment 에서 호출)</summary>
    public void UnequipWeapon()
    {
        CurrentWeapon = null;

#if UNITY_EDITOR
        Debug.Log("[PlayerCombat] 무기 해제");
#endif
    }

    // ─────────────────────── 공개 프로퍼티 ───────────────────────

    /// <summary>현재 공격 쿨다운 남은 시간 (UI 표시용)</summary>
    public float AttackCooldownRemaining => Mathf.Max(0f, _attackCooldownTimer);

    /// <summary>현재 공격 쿨다운 진행률 0~1 (UI 게이지용)</summary>
    public float AttackCooldownProgress =>
        AttackInterval > 0f ? 1f - (_attackCooldownTimer / AttackInterval) : 1f;

    // ─────────────────────── 에디터 디버그 ───────────────────────

#if UNITY_EDITOR
    [ContextMenu("테스트: 강제 공격")]
    private void TestForceAttack()
    {
        if (CurrentWeapon == null)
        {
            Debug.LogWarning("[PlayerCombat] 무기가 장착되지 않음");
            return;
        }
        TryAttack();
    }

    [ContextMenu("테스트: 공격 속도 +50%")]
    private void TestAttackSpeedBonus()
    {
        SetAttackSpeedBonus(0.5f);
        Debug.Log($"[PlayerCombat] 공격 속도 보너스 적용: +50% (간격: {AttackInterval:F2}초)");
    }

    [ContextMenu("테스트: 공격력 +30%")]
    private void TestAttackDamageBonus()
    {
        SetAttackDamageBonus(0.3f);
        Debug.Log("[PlayerCombat] 공격력 보너스 적용: +30%");
    }

    [ContextMenu("테스트: 보너스 초기화")]
    private void TestResetBonus()
    {
        SetAttackSpeedBonus(0f);
        SetAttackDamageBonus(0f);
        SetLifeStealPercent(0f);
        Debug.Log("[PlayerCombat] 모든 보너스 초기화");
    }
#endif
}
