// BossHealthBarUI.cs
// 보스 전용 체력바 (화면 상단). 보스가 등장하면 Bind 로 연결해 체력을 표시한다.
// 현재는 더미 단계 — UI 와 연동 코드만 준비해 둔다. 실제 보스 등장 시 BossNidhogg 가 Bind 를 호출하면 된다.
// 담당: 김보민

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("보스 체력바 전체 패널 (등장 시 켜고 사망 시 끈다)")]
    public GameObject barPanel;

    [Tooltip("체력 비율을 표시할 이미지 (Image Type = Filled, Fill Method = Horizontal 권장)")]
    public Image fillImage;

    [Tooltip("보스 이름 텍스트 (선택)")]
    public TMP_Text bossNameText;

    [Tooltip("현재/최대 체력 숫자 텍스트 (선택)")]
    public TMP_Text healthText;

    public static BossHealthBarUI Instance { get; private set; }

    // 현재 연결된 보스
    private EnemyBase _boss = null;

    private void Awake()
    {
        Instance = this;

        // 시작 시 숨김
        if (barPanel != null)
        {
            barPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Unbind();
    }

    // 보스 등장 시 호출 — 이 보스의 체력 변화를 체력바에 연결한다
    public void Bind(EnemyBase boss, string displayName)
    {
        if (boss == null)
        {
            return;
        }

        // 이전 보스 연결은 먼저 해제
        Unbind();

        _boss = boss;
        _boss.OnHealthChanged += HandleHealthChanged;
        _boss.OnDied += HandleBossDied;

        if (bossNameText != null)
        {
            bossNameText.text = displayName;
        }

        if (barPanel != null)
        {
            barPanel.SetActive(true);
        }

        // 초기 체력 표시
        HandleHealthChanged(_boss.Health, _boss.MaxHealth);
    }

    // 연결 해제 (사망/파괴 시)
    public void Unbind()
    {
        if (_boss != null)
        {
            _boss.OnHealthChanged -= HandleHealthChanged;
            _boss.OnDied -= HandleBossDied;
            _boss = null;
        }
    }

    // 체력이 변할 때마다 바와 숫자를 갱신
    private void HandleHealthChanged(float current, float max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = max > 0f ? current / max : 0f;
        }

        if (healthText != null)
        {
            healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
        }
    }

    // 보스 사망 시 바를 숨긴다
    private void HandleBossDied(EnemyBase boss)
    {
        Unbind();

        if (barPanel != null)
        {
            barPanel.SetActive(false);
        }
    }

    // 더미 테스트: 인스펙터에서 우클릭해 가짜 체력바를 띄워본다 (연출/배치 확인용)
    [ContextMenu("더미 체력바 표시 (테스트)")]
    private void ShowDummyBar()
    {
        if (bossNameText != null)
        {
            bossNameText.text = "니드호그 (더미)";
        }
        if (barPanel != null)
        {
            barPanel.SetActive(true);
        }
        HandleHealthChanged(1000f, 1000f);
    }

    [ContextMenu("더미 체력바 숨김 (테스트)")]
    private void HideDummyBar()
    {
        if (barPanel != null)
        {
            barPanel.SetActive(false);
        }
    }
}
