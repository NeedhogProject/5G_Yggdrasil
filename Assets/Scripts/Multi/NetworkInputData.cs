using Fusion;
using UnityEngine;

/// <summary>
/// 멀티 입력 데이터 (클라이언트에서 호스트로 매 틱 전송)
///
/// - 현재는 접속 테스트용 이동 방향만 담는다
/// - 실제 게임에서는 클라이언트가 NavMesh 경로로 계산한 이동 방향을 여기에 넣는다
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector3 vDirection;   // 이동 방향 (y 는 0)
}
