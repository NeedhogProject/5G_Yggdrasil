/*
 * HealthBarBillboard.cs
 * 월드 공간 체력바가 항상 카메라를 바라보도록 회전
 * 적 머리 위 HPBar Canvas 에 부착
 * 담당: 김보민
 */

using UnityEngine;

public class HealthBarBillboard : MonoBehaviour
{
    // 바라볼 카메라 (비우면 메인 카메라 자동 사용)
    private Camera _targetCamera;

    private void Start()
    {
        _targetCamera = Camera.main;
    }

    // 카메라 이동/회전이 모두 끝난 뒤 정렬하기 위해 LateUpdate 사용
    private void LateUpdate()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
            if (_targetCamera == null)
            {
                return;
            }
        }

        // 카메라가 바라보는 방향과 같은 방향을 보게 해서 화면에 정면으로 보이게 함
        Vector3 cameraForward = _targetCamera.transform.forward;
        transform.forward = cameraForward;
    }
}