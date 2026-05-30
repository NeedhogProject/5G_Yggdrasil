// KeyBindingManager.cs
// 키 바인딩 관리 싱글턴 (New Input System 기반)
// 정건희 팀원의 InputActionAsset 바인딩을 런타임에 재설정하고
// 그 오버라이드를 JSON으로 PlayerPrefs에 영구 저장한다.
//
// 전제
// 1. 정건희 팀원의 InputActionAsset(.inputactions)을 인스펙터에 연결
// 2. 슬롯의 액션 이름은 Asset에 등록된 이름과 정확히 일치해야 함
// 3. 입력을 소비하는 쪽이 InputAction을 통해 읽어야 키 변경이 실제로 반영됨
//    Keyboard.current.xxxKey.isPressed 같은 하드코딩 부분은 정건희 팀원과 협의 필요

using UnityEngine;
using UnityEngine.InputSystem;

public class KeyBindingManager : MonoBehaviour
{
    public static KeyBindingManager Instance;

    [Header("정건희 팀원 InputActionAsset 연결")]
    [SerializeField] private InputActionAsset inputActions;

    // 다른 설정(볼륨 등)과 분리된 전용 PlayerPrefs 키
    private const string OVERRIDES_KEY = "InputBindings_Overrides";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();

            // 직접 IsPressed/WasPressedThisFrame 호출하려면 액션을 활성화해야 함
            if (inputActions != null)
            {
                inputActions.Enable();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 액션 이름으로 InputAction 찾기
    public InputAction FindAction(string actionName)
    {
        if (inputActions == null)
        {
            return null;
        }
        return inputActions.FindAction(actionName, false);
    }

    // 현재 바인딩된 키의 표시 문자열 반환 ("W", "Shift", "LMB" 등)
    public string GetBindingDisplay(string actionName, int bindingIndex)
    {
        InputAction action = FindAction(actionName);
        if (action == null)
        {
            return "-";
        }
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return "-";
        }
        return action.GetBindingDisplayString(bindingIndex);
    }

    // 키 재설정 시작
    // onComplete: 새 키 입력이 완료된 경우
    // onCancel : Esc 또는 외부에서 취소된 경우
    // 반환된 RebindingOperation은 호출자가 보관해서 필요 시 Cancel/Dispose 해야 함
    public InputActionRebindingExtensions.RebindingOperation StartRebind(
        string actionName,
        int bindingIndex,
        System.Action onComplete,
        System.Action onCancel)
    {
        InputAction action = FindAction(actionName);
        if (action == null)
        {
            onCancel?.Invoke();
            return null;
        }

        // 리바인딩 중에는 액션을 비활성화해야 입력이 게임 쪽으로 새지 않음
        action.Disable();

        InputActionRebindingExtensions.RebindingOperation op = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(_ =>
            {
                action.Enable();
                onCancel?.Invoke();
            })
            .OnComplete(rebindOp =>
            {
                action.Enable();
                SaveBindings();
                onComplete?.Invoke();
                rebindOp.Dispose();
            })
            .Start();

        return op;
    }

    // 현재의 모든 바인딩 오버라이드를 JSON으로 PlayerPrefs에 저장
    public void SaveBindings()
    {
        if (inputActions == null)
        {
            return;
        }
        string json = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(OVERRIDES_KEY, json);
        PlayerPrefs.Save();
    }

    // 저장된 오버라이드를 불러와 적용
    public void LoadBindings()
    {
        if (inputActions == null)
        {
            return;
        }
        string json = PlayerPrefs.GetString(OVERRIDES_KEY, string.Empty);
        if (string.IsNullOrEmpty(json) == true)
        {
            return;
        }
        inputActions.LoadBindingOverridesFromJson(json);
    }

    // 기본값으로 복원
    // 키 관련 PlayerPrefs만 지움, 볼륨 등 다른 설정은 건드리지 않음
    public void ResetBindings()
    {
        if (inputActions == null)
        {
            return;
        }
        inputActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(OVERRIDES_KEY);
        PlayerPrefs.Save();
    }
}