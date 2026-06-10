/*
 * PersistentCanvas.cs
 * 씬 전환에도 유지되는 UI Canvas
 * 설정창 + 저장슬롯창처럼 게임 내내 살아있어야 하는 UI 를 담음
 * 담당: 김보민
 */

using UnityEngine;

/// <summary>
/// 씬을 넘나들며 유지되는 UI Canvas
///
/// [용도]
/// - 설정창(KeySettingPanel), 저장슬롯창(SaveSlotPanel) 처럼
///   타이틀/마을/던전 어디서든 떠야 하는 UI 를 자식으로 담음
/// - 중복 생성 방지 (씬 다시 로드돼도 1개만 유지)
///
/// [씬 설정]
/// 타이틀 씬에 Canvas 만들고 부착
/// 그 아래에 KeySettingPanel, SaveSlotPanel 등을 자식으로 배치
/// </summary>
public class PersistentCanvas : MonoBehaviour
{
    // 중복 방지용 정적 참조
    private static PersistentCanvas _instance;

    private void Awake()
    {
        // 이미 존재하면 자신을 파괴 (씬 재로드 시 중복 방지)
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}