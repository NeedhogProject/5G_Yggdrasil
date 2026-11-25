using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 층 이동 관리자 — StemConnector, StemAscender 에서 참조
/// GameManager 에 층 전환을 위임하는 얇은 래퍼
/// </summary>
public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>현재 층 번호 (GameManager 에서 읽어옴)</summary>
    public int CurrentFloor => GameManager.Instance?.CurrentFloor ?? 1;

    /// <summary>한 층 위로 (StemAscender 에서 호출)</summary>
    public void GoUpOneFloor()
    {
        // 멀티 세션 중이면 파티 전원 이동 요청 (호스트 승인 후 각자 GameManager.GoToFloor)
        if (NetworkPlayerAvatar.RequestPartyMove(Mathf.Max(CurrentFloor - 1, 0)) == true)
        {
            return;
        }

        if (CurrentFloor <= 1)
            GameManager.Instance?.ReturnToTown();
        else
            GameManager.Instance?.GoToFloor(CurrentFloor - 1); // 위험: GoToNextFloor 는 층 +1 이라 하강됨
    }

    /// <summary>한 층 아래로 (StemConnector 에서 호출)</summary>
    public void GoDownOneFloor()
    {
        // 최하층이 아니고 멀티 세션 중이면 파티 전원 이동 요청
        if (CurrentFloor < GameManager.MAX_FLOOR && NetworkPlayerAvatar.RequestPartyMove(CurrentFloor + 1) == true)
        {
            return;
        }

        GameManager.Instance?.GoToNextFloor();
    }

    /// <summary>씬 이름으로 직접 이동 (StemManager 에서 호출)</summary>
    public void LoadFloor(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>층 번호로 이동 — YggdrasilPortal 에서 호출</summary>
    public void LoadFloor(int floor)
    {
        if (NetworkPlayerAvatar.RequestPartyMove(floor) == true)
        {
            return;
        }

        GameManager.Instance?.GoToFloor(floor);
    }
}
