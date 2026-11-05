using Fusion;
using UnityEngine;

/// <summary>
/// 멀티 접속 테스트용 플레이어 (MultiTest 씬 전용)
///
/// - NetworkInputData 의 이동 방향으로 NetworkCharacterController 를 움직인다
/// - 입력 권한 플레이어 번호로 색을 나눠 두 플레이어를 구분한다
/// - 실제 Player 프리팹 연동 전 동기화 확인용이며, 이후 단계에서 교체한다
/// </summary>
[RequireComponent(typeof(NetworkCharacterController))]
public class TestNetworkPlayer : NetworkBehaviour
{
    [Tooltip("플레이어 번호별 색 (호스트, 참가자 순)")]
    [SerializeField] private Color[] playerColors = { new Color(0.2f, 0.6f, 1f), new Color(1f, 0.5f, 0.1f) };

    private NetworkCharacterController _ncc;

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();

        // 입력 권한 플레이어 번호로 색 지정 (모든 클라이언트에서 같은 결과, 번호는 1부터 시작)
        int nIndex = Mathf.Abs(Object.InputAuthority.PlayerId - 1) % playerColors.Length;
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColors[nIndex];
        }

        gameObject.name = "Player_" + Object.InputAuthority.PlayerId;
        Debug.Log("[TestNetworkPlayer] 생성: " + gameObject.name + " 내 캐릭터=" + HasInputAuthority);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data) == true)
        {
            _ncc.Move(data.vDirection.normalized);
        }
    }
}
