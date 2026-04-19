using UnityEngine;
using System;

/// <summary>
/// 적 베이스 클래스
///
/// [기획 반영]
/// - 체력/공격력/이동속도/공격범위 스탯
/// - 피격 시 HP바 표시
/// - 사망 시 StemManager 에 열쇠 드롭 알림
/// - 층이 깊어질수록 스탯 강화 (DungeonDifficultyScaler 연동)
/// - EnemyAI 에서 상속하여 사용
/// </summary>
public class EnemyBase : MonoBehaviour
{
    // ─────────────────────── 기본 스탯 ───────────────────────

    [Header("기본 스탯")]
    [SerializeField] protected float maxHealth    = 100f;
    [SerializeField] protected float attackPower  = 10f;
    [SerializeField] protected float moveSpeed    = 3f;
    [SerializeField] protected float attackRange  = 1.5f;

    [Header("방어력")]
    [Tooltip("0~100. PlayerStats 의 방어력 공식과 동일하게 적용")]
    [SerializeField] protected float defense = 0f;

    // ─────────────────────── 런타임 상태 ───────────────────────

    protected float _currentHealth;
    protected bool  _isDead = false;

    public bool IsDead => _isDead;

    // ─────────────────────── HP바 ───────────────────────

    [Header("HP바")]
    [Tooltip("피격 시 표시할 HP바 UI 오브젝트 (World Space Canvas 권장)")]
    [SerializeField] private GameObject hpBarObject;

    [Tooltip("HP바 Fill 이미지 (Image 컴포넌트)")]
    [SerializeField] private UnityEngine.UI.Image hpBarFill;

    [Tooltip("HP바 자동 숨김 시간 (초). 0이면 항상 표시")]
    [SerializeField] private float hpBarHideDelay = 3f;

    private float _hpBarTimer = 0f;
    private bool  _hpBarVisible = false;

    // ─────────────────────── 이벤트 ───────────────────────

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action<EnemyBase>    OnDied;

    // ─────────────────────── 프로퍼티 ───────────────────────

    public float MaxHealth   => maxHealth;
    public float Health      => _currentHealth;
    public float AttackPower => attackPower;
    public float MoveSpeed   => moveSpeed;
    public float AttackRange => attackRange;

    // ─────────────────────── 초기화 ───────────────────────

    protected virtual void Awake()
    {
        _currentHealth = maxHealth;
        if (hpBarObject != null) hpBarObject.SetActive(false);
    }

    protected virtual void Update()
    {
        UpdateHPBar();
    }

    // ─────────────────────── 피격 / 데미지 ───────────────────────

    /// <summary>
    /// 데미지 수신 — PlayerCombat.OnHit 에서 호출
    /// PlayerStats.TakeDamage 와 동일한 방어력 공식 적용
    /// </summary>
    public virtual void TakeDamage(float rawDamage)
    {
        if (_isDead) return;

        float actualDamage = CalculateDamage(rawDamage);
        _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        ShowHPBar();

        Debug.Log($"[{gameObject.name}] 피격 — 데미지: {actualDamage:F1} / 체력: {_currentHealth:F1}/{maxHealth}");

        if (_currentHealth <= 0f) Die();
    }

    /// <summary>
    /// 방어력 기반 데미지 계산 (PlayerStats.TakeDamage 와 동일한 공식)
    /// </summary>
    private float CalculateDamage(float rawDamage)
    {
        float dd;
        if      (defense <= 100f) dd = defense * 0.25f;
        else if (defense <= 200f) dd = 25f  + (defense - 100f) * 0.125f;
        else if (defense <= 300f) dd = 37.5f + (defense - 200f) * 0.0625f;
        else if (defense <= 400f) dd = 43.75f + (defense - 300f) * 0.03125f;
        else                      dd = 46.875f + (defense - 400f) * 0.015625f;

        dd = Mathf.Clamp(dd, 0f, 95f);
        float damageRatio = Mathf.Max(1f - (dd / 100f), 0.05f);
        return rawDamage * damageRatio;
    }

    // ─────────────────────── 사망 ───────────────────────

    protected virtual void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[{gameObject.name}] 사망");

        // 열쇠 드롭 — StemManager 에 알림
        FloorKeyData droppedKey = StemManager.Instance?.OnEnemyDied(gameObject);
        if (droppedKey != null)
            DropKey(droppedKey);

        // 아이템 드롭 — LootTable 연동
        GetComponent<LootTable>()?.RollDrop(transform.position);

        OnDied?.Invoke(this);

        // 사망 연출 후 오브젝트 제거
        // 애니메이션 있을 경우 Invoke 딜레이 조정
        Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DropKey(FloorKeyData key)
    {
        // 열쇠 아이템을 월드에 스폰
        // InventorySystem 완성 전 임시: 로그만 출력
        Debug.Log($"[{gameObject.name}] {key.KeyDirection} 열쇠 드롭");
        // TODO: 열쇠 아이템 프리팹 스폰 → 플레이어 획득 시 인벤토리 추가
    }

    private void DestroyEnemy() => Destroy(gameObject);

    // ─────────────────────── HP바 ───────────────────────

    private void ShowHPBar()
    {
        if (hpBarObject == null) return;
        hpBarObject.SetActive(true);
        _hpBarVisible = true;
        _hpBarTimer   = hpBarHideDelay;
        UpdateHPBarFill();
    }

    private void UpdateHPBar()
    {
        if (!_hpBarVisible || hpBarHideDelay <= 0f) return;

        _hpBarTimer -= Time.deltaTime;
        if (_hpBarTimer <= 0f)
        {
            _hpBarVisible = false;
            if (hpBarObject != null) hpBarObject.SetActive(false);
        }
    }

    private void UpdateHPBarFill()
    {
        if (hpBarFill == null) return;
        hpBarFill.fillAmount = maxHealth > 0f ? _currentHealth / maxHealth : 0f;
    }

    // ─────────────────────── 스탯 조정 (DungeonDifficultyScaler 연동) ───────────────────────

    /// <summary>
    /// 층/거리 기반 스탯 배율 적용
    /// DungeonDifficultyScaler 에서 호출
    /// </summary>
    public void ApplyDifficultyScale(float healthMult, float attackMult)
    {
        maxHealth    *= healthMult;
        attackPower  *= attackMult;
        _currentHealth = maxHealth;
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
