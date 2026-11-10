using Fusion;
using UnityEngine;

/// <summary>
/// 멀티 입력 데이터 (클라이언트에서 호스트로 매 틱 전송)
///
/// - 분신 방식: 각자 PC 의 씬 Player 가 계산한 위치/회전/동작 상태를 그대로 보낸다
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public NetworkBool bHasPlayer;  // 씬 Player 가 있어 아래 값이 유효한지
    public Vector3 vPosition;       // 내 캐릭터 위치
    public float fYaw;              // 내 캐릭터 Y 회전
    public byte nMoveState;         // 0 정지, 1 걷기, 2 달리기
    public byte nAttackCount;       // 공격할 때마다 1 증가
    public byte nFloor;             // 내가 있는 층 (0 마을)
}
