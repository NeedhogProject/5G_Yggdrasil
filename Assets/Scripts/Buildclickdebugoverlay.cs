// BuildClickDebugOverlay.cs
// 빌드에서 클릭이 안 먹는 원인을 화면에 직접 표시하는 임시 디버그 오버레이
// 마우스 위치 / 클릭 감지 여부 / 클릭 지점 최상단 UI / EventSystem 존재 여부를 OnGUI 로 그린다
// 원인 파악 후 반드시 삭제할 것 (임시 스크립트)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildClickDebugOverlay : MonoBehaviour
{
    private string pointerInfo = "대기 중";
    private string topUIName = "아직 클릭 안 함";
    private int hitCount = 0;
    private bool clickedThisSession = false;

    private void Update()
    {
        // 새 Input System 이 마우스를 못 잡으면 입력 자체가 안 들어오는 것
        if (Mouse.current == null)
        {
            pointerInfo = "Mouse.current 가 null — 입력 장치 인식 안 됨";
            return;
        }

        Vector2 pos = Mouse.current.position.ReadValue();
        pointerInfo = "마우스 위치: " + Mathf.RoundToInt(pos.x).ToString() + ", " + Mathf.RoundToInt(pos.y).ToString();

        // 왼쪽 버튼이 이번 프레임에 눌렸는지
        if (Mouse.current.leftButton.wasPressedThisFrame == true)
        {
            clickedThisSession = true;
            DoRaycast(pos);
        }
    }

    // 클릭 지점에서 UI 레이캐스트 수행
    private void DoRaycast(Vector2 screenPos)
    {
        if (EventSystem.current == null)
        {
            topUIName = "EventSystem.current 가 null!";
            hitCount = 0;
            return;
        }

        PointerEventData data = new PointerEventData(EventSystem.current);
        data.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        hitCount = results.Count;

        if (results.Count > 0)
        {
            topUIName = results[0].gameObject.name;
        }
        else
        {
            topUIName = "없음 (레이캐스트 0개)";
        }
    }

    // 화면에 디버그 정보 표시 (빌드에서도 보임)
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 26;
        style.normal.textColor = Color.yellow;

        float y = 20f;

        GUI.Label(new Rect(20f, y, 1400f, 40f), pointerInfo, style);
        y = y + 36f;

        string clickText = "클릭 감지: ";
        if (clickedThisSession == true)
        {
            clickText = clickText + "예 (입력 들어옴)";
        }
        else
        {
            clickText = clickText + "아니오 (클릭 입력이 안 들어옴)";
        }

        GUI.Label(new Rect(20f, y, 1400f, 40f), clickText, style);
        y = y + 36f;

        GUI.Label(new Rect(20f, y, 1400f, 40f), "클릭 지점 최상단 UI: " + topUIName, style);
        y = y + 36f;

        GUI.Label(new Rect(20f, y, 1400f, 40f), "레이캐스트 히트 수: " + hitCount.ToString(), style);
        y = y + 36f;

        string esText = "EventSystem.current: ";
        if (EventSystem.current != null)
        {
            esText = esText + EventSystem.current.gameObject.name;
        }
        else
        {
            esText = esText + "null! (활성 EventSystem 없음)";
        }

        GUI.Label(new Rect(20f, y, 1400f, 40f), esText, style);
    }
}