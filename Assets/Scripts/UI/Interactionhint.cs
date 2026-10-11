// InteractionHint.cs
// 모든 상호작용 물체가 공유하는 단 하나의 "E" 프롬프트 매니저 (싱글턴).
// 상호작용 물체가 범위 진입 시 Register(대상, 높이, 문구), 이탈 시 Unregister 를 호출하면
// 매니저가 하나뿐인 프롬프트를 켜고 그 물체가 넘긴 문구를 표시한다.
// 씬에 빈 오브젝트 하나 만들어 이 스크립트를 붙이고, UI 텍스트(TMP)를 Prompt Object / Prompt Text 에 연결한다.
// 여러 물체 범위에 동시에 들어가면 가장 최근에 들어간 물체의 문구를 띄운다.
// Follow In World 를 켜면 월드에서 대상 머리 위를 따라다니고, 끄면 화면 고정 위치에서 켜고 끄기만 한다.

using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionHint : MonoBehaviour
{
    public static InteractionHint Instance { get; private set; }

    [Header("공용 'E' 프롬프트")]
    [Tooltip("켜고 끌 프롬프트 오브젝트 (UI 텍스트 등). 평소엔 꺼둔다")]
    [SerializeField] private GameObject promptObject;

    [Tooltip("문구를 표시할 텍스트. 비워두면 Prompt Object 에서 자동으로 찾는다")]
    [SerializeField] private TMP_Text promptText;

    [Tooltip("켜면 프롬프트가 대상 머리 위(월드)를 따라다님. 끄면 화면 고정 위치에서 켜지고 꺼지기만 함(권장, 더 단순)")]
    [SerializeField] private bool followInWorld = false;

    [Tooltip("월드 따라다니기(followInWorld) 켤 때만 사용 — 프롬프트가 카메라를 바라보게 할지")]
    [SerializeField] private bool faceCamera = true;

    // 현재 범위에 들어와 있는 물체들 (등록 순서 유지, 마지막이 가장 최근)
    private List<Transform> _order = new List<Transform>();
    // 물체별 프롬프트 높이 오프셋
    private Dictionary<Transform, float> _offsets = new Dictionary<Transform, float>();
    // 물체별 표시 문구
    private Dictionary<Transform, string> _messages = new Dictionary<Transform, string>();

    private Transform _currentTarget = null;
    private float _currentOffset = 2f;
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 텍스트 참조가 비어있으면 프롬프트에서 자동으로 찾는다
        if (promptText == null && promptObject != null)
        {
            promptText = promptObject.GetComponentInChildren<TMP_Text>();
        }

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 상호작용 범위 진입 시 호출 (대상, 프롬프트 높이, 표시 문구)
    public void Register(Transform target, float heightOffset, string message)
    {
        if (target == null)
        {
            return;
        }

        if (_order.Contains(target) == false)
        {
            _order.Add(target);
        }
        _offsets[target] = heightOffset;
        _messages[target] = message;

        UpdateCurrentTarget();
    }

    // 상호작용 범위 이탈 시 호출
    public void Unregister(Transform target)
    {
        if (target == null)
        {
            return;
        }

        _order.Remove(target);
        if (_offsets.ContainsKey(target) == true)
        {
            _offsets.Remove(target);
        }
        if (_messages.ContainsKey(target) == true)
        {
            _messages.Remove(target);
        }

        UpdateCurrentTarget();
    }

    // 현재 프롬프트를 띄울 대상 결정 (목록의 마지막 = 가장 최근 진입)
    private void UpdateCurrentTarget()
    {
        // 파괴되어 사라진 항목 정리
        for (int i = _order.Count - 1; i >= 0; i--)
        {
            if (_order[i] == null)
            {
                _order.RemoveAt(i);
            }
        }

        if (_order.Count == 0)
        {
            _currentTarget = null;

            if (promptObject != null)
            {
                promptObject.SetActive(false);
            }
            return;
        }

        _currentTarget = _order[_order.Count - 1];

        if (_offsets.ContainsKey(_currentTarget) == true)
        {
            _currentOffset = _offsets[_currentTarget];
        }
        else
        {
            _currentOffset = 2f;
        }

        // 현재 대상의 문구를 프롬프트에 표시
        if (promptText != null)
        {
            string message = "";
            if (_messages.ContainsKey(_currentTarget) == true)
            {
                message = _messages[_currentTarget];
            }
            promptText.text = message;
        }

        if (promptObject != null)
        {
            promptObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        // 화면 고정 모드면 위치를 따라다니지 않는다 (켜고 끄기는 Register/Unregister 가 처리)
        if (followInWorld == false)
        {
            return;
        }

        if (_currentTarget == null || promptObject == null)
        {
            return;
        }

        // 대상 머리 위로 위치
        promptObject.transform.position = _currentTarget.position + Vector3.up * _currentOffset;

        if (faceCamera == false)
        {
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
        if (_mainCamera == null)
        {
            return;
        }

        // 프롬프트가 카메라를 정면으로 바라보게 회전 (빌보드)
        Vector3 lookDir = promptObject.transform.position - _mainCamera.transform.position;
        if (lookDir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        promptObject.transform.rotation = Quaternion.LookRotation(lookDir);
    }
}