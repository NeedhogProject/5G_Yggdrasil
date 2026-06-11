/*
 * ClickDebugger.cs
 * 마우스 클릭이 실제로 어떤 UI 오브젝트에 닿는지 Console 에 출력 (디버그용)
 * 문제 해결 후 제거할 것
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 좌클릭 시 그 위치에서 레이캐스트되는 모든 UI 오브젝트를 Console 에 출력
/// 어떤 패널이 클릭을 가로채는지 찾는 용도
///
/// [씬 설정]
/// 아무 오브젝트에나 임시로 부착 후 Play
/// </summary>
public class ClickDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame == false)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[ClickDebugger] EventSystem 이 없습니다");
            return;
        }

        // 현재 마우스 위치에서 레이캐스트
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log("[ClickDebugger] 클릭 위치에 UI 없음 (레이캐스트 결과 0개)");
            return;
        }

        Debug.Log("[ClickDebugger] 클릭 지점 UI 목록 (위에서부터 = 클릭 먹는 순서):");

        for (int i = 0; i < results.Count; i++)
        {
            string objName = results[i].gameObject.name;
            string parentName = "";

            if (results[i].gameObject.transform.parent != null)
            {
                parentName = results[i].gameObject.transform.parent.name;
            }

            Debug.Log("[ClickDebugger] " + i + ". " + objName + " (부모: " + parentName + ")");
        }
    }
}