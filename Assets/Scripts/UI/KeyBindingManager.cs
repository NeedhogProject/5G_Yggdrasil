// KeyBindingManager.cs
// 키 바인딩 저장 및 조회 싱글턴
// 액션 이름별 KeyCode를 PlayerPrefs에 영구 저장한다.
// 주의: 실제 입력 소비는 PlayerController/PlayerCombat 영역이며
//       현재 플레이어는 New Input System을 사용하므로 연동 방식은 정건희 팀원과 협의 필요

using System.Collections.Generic;
using UnityEngine;

public class KeyBindingManager : MonoBehaviour
{
    public static KeyBindingManager Instance;

    // 액션 이름과 키 매핑
    private Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();

    private void Awake()
    {
        // 싱글턴, 씬 전환에도 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadKeys();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 액션에 매핑된 키 반환, 없으면 None
    public KeyCode GetKey(string actionName)
    {
        if (keyBindings.ContainsKey(actionName) == true)
        {
            return keyBindings[actionName];
        }
        return KeyCode.None;
    }

    // 액션에 키 설정
    public void SetKey(string actionName, KeyCode key)
    {
        keyBindings[actionName] = key;
    }

    // 모든 키를 PlayerPrefs에 저장
    public void SaveKeys()
    {
        foreach (KeyValuePair<string, KeyCode> pair in keyBindings)
        {
            PlayerPrefs.SetString("KEY_" + pair.Key, pair.Value.ToString());
        }
        PlayerPrefs.Save();
    }

    // 저장된 키 불러오기, 없으면 기본값
    public void LoadKeys()
    {
        keyBindings.Clear();
        keyBindings["MoveForward"] = LoadKey("MoveForward", KeyCode.W);
        keyBindings["MoveBack"] = LoadKey("MoveBack", KeyCode.S);
        keyBindings["MoveLeft"] = LoadKey("MoveLeft", KeyCode.A);
        keyBindings["MoveRight"] = LoadKey("MoveRight", KeyCode.D);
        keyBindings["Run"] = LoadKey("Run", KeyCode.LeftShift);
        keyBindings["Attack"] = LoadKey("Attack", KeyCode.Mouse0);
        keyBindings["Inventory"] = LoadKey("Inventory", KeyCode.I);
        keyBindings["DropItem"] = LoadKey("DropItem", KeyCode.G);
        keyBindings["Interact"] = LoadKey("Interact", KeyCode.F);
        keyBindings["RotateItem"] = LoadKey("RotateItem", KeyCode.R);
    }

    // 단일 키 로드, 저장값 없으면 기본 키
    private KeyCode LoadKey(string actionName, KeyCode defaultKey)
    {
        string saved = PlayerPrefs.GetString("KEY_" + actionName, defaultKey.ToString());
        return (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
    }

    // 기본값 복원
    // 키 관련 항목만 삭제한다. DeleteAll은 볼륨 등 다른 설정까지 지우므로 사용 금지
    public void ResetDefaults()
    {
        string[] actionNames =
        {
            "MoveForward", "MoveBack", "MoveLeft", "MoveRight", "Run",
            "Attack", "Inventory", "DropItem", "Interact", "RotateItem"
        };

        foreach (string actionName in actionNames)
        {
            PlayerPrefs.DeleteKey("KEY_" + actionName);
        }

        LoadKeys();
    }
}