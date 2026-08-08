// PlayerInteractionLock.cs
// NPC 대화·상점·대장장이·각인·던전 확인창 등 UI가 열려 있는 동안 플레이어 이동을 잠근다.
// 개별 시스템 코드(IsOpen 등)에 의존하지 않도록, 인스펙터에 "이 패널이 켜지면 잠금" 목록을 받는다.
// 목록 중 하나라도 활성화되면 PlayerController 를 비활성화하고 Rigidbody 속도와 걷기 애니메이션을 멈춘다.
// 정건희의 PlayerController 파일은 수정하지 않고, 외부에서 enabled 와 애니메이터 파라미터만 제어한다.

using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionLock : MonoBehaviour
{
    [Header("이 패널들 중 하나라도 켜지면 플레이어 이동을 잠근다")]
    [SerializeField] private List<GameObject> lockWhenActive = new List<GameObject>();

    [Header("걷기 애니메이션 정지용 (플레이어 애니메이터 파라미터명)")]
    [SerializeField] private string moveSpeedParam = "Speed";
    [SerializeField] private string movingBoolParam = "";

    private PlayerController _player;
    private Rigidbody _playerBody;
    private Animator _playerAnimator;
    private bool _locked = false;

    private void Update()
    {
        // 플레이어 참조 확보 (씬 전환으로 끊길 수 있어 없을 때마다 다시 찾는다)
        if (_player == null)
        {
            _player = PlayerController.Instance;

            if (_player != null)
            {
                _playerBody = _player.GetComponent<Rigidbody>();
                _playerAnimator = _player.GetComponentInChildren<Animator>();
            }
        }

        if (_player == null)
        {
            return;
        }

        // 잠금 대상 패널 중 하나라도 켜져 있는지 검사
        bool shouldLock = false;

        foreach (GameObject panel in lockWhenActive)
        {
            if (panel != null && panel.activeInHierarchy == true)
            {
                shouldLock = true;
                break;
            }
        }

        // 상태가 바뀔 때만 잠금/해제 처리
        if (shouldLock == true && _locked == false)
        {
            LockPlayer();
        }
        else if (shouldLock == false && _locked == true)
        {
            UnlockPlayer();
        }

        // 잠긴 동안에는 미끄러짐과 걷기 모션을 계속 눌러준다
        if (_locked == true)
        {
            StopMotion();
        }
    }

    // 이동 잠금 — PlayerController 비활성화 + 속도/애니메이션 정지
    private void LockPlayer()
    {
        _locked = true;
        _player.enabled = false;
        StopMotion();
    }

    // 이동 잠금 해제 — PlayerController 다시 활성화
    private void UnlockPlayer()
    {
        _locked = false;

        if (_player != null)
        {
            _player.enabled = true;
        }
    }

    // 속도와 걷기 애니메이션을 멈춘다 (idle 로 되돌림)
    private void StopMotion()
    {
        if (_playerBody != null)
        {
            _playerBody.linearVelocity = Vector3.zero;
            _playerBody.angularVelocity = Vector3.zero;
        }

        if (_playerAnimator != null)
        {
            if (string.IsNullOrEmpty(moveSpeedParam) == false)
            {
                _playerAnimator.SetFloat(moveSpeedParam, 0f);
            }

            if (string.IsNullOrEmpty(movingBoolParam) == false)
            {
                _playerAnimator.SetBool(movingBoolParam, false);
            }
        }
    }
}