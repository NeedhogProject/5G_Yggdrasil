using UnityEngine;

/// <summary>
/// 3층 보스 구역 BGM 전환
///
/// [기획 반영]
/// - 3층은 일반 몬스터 구역과 보스 구역으로 나뉨
/// - 플레이어가 보스 구역에 들어가면 보스 BGM, 나오면 층 BGM 으로 복귀
///
/// [씬 설정]
/// - 보스 구역 전체를 덮는 오브젝트에 부착
/// - Collider(isTrigger = true) 부착
/// - AudioManager BGM 목록에 Floor3Boss 클립 등록
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossZoneBGM : MonoBehaviour
{
    [Header("BGM 전환 페이드 시간 (초)")]
    [SerializeField] [Range(0f, 5f)] private float fadeTime = 1.0f;

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Player") == false || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlayBossZoneBGM(fadeTime);
    }

    private void OnTriggerExit(Collider _other)
    {
        if (_other.CompareTag("Player") == false || AudioManager.Instance == null || GameManager.Instance == null)
        {
            return;
        }

        // 보스 구역을 벗어나면 현재 층 BGM 으로 복귀
        AudioManager.Instance.PlayFloorBGM(GameManager.Instance.CurrentFloor, fadeTime);
    }
}
