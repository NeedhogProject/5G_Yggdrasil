using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정창(ESC) 게임 저장 버튼
///
/// [기획 반영]
/// - 누르면 저장하고 이후 자동 저장 시작 (새 게임은 이 버튼을 누르기 전까지 기존 저장을 건드리지 않음)
/// - 타이틀에서는 저장할 게임이 없어 비활성
///
/// [씬 설정]
/// - Title 씬 PersistentCanvas/KeySettingPanel 안의 저장 버튼에 부착 (ExitButton 옆)
/// - 설정창이 열릴 때마다 OnEnable 로 활성 여부 갱신 (탭 전환과 무관하게 동작)
/// </summary>
[RequireComponent(typeof(Button))]
public class SaveGameButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnEnable()
    {
        bool bInGame = GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Title;
        _button.interactable = bInGame;
    }

    // 저장 성공 시 효과음, 실패(사망 화면 등) 시 오류음
    private void OnClicked()
    {
        if (SaveSystem.SaveManually() == true)
        {
            AudioManager.Instance?.PlaySFX(SFXClip.SetEffectActivate);
        }
        else
        {
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
}
