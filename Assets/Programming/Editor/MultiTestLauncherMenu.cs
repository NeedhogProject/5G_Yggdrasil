using Fusion;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 멀티 테스트 런처 자동 생성 (에디터 전용)
///
/// - Tools > Multi > 테스트 런처 자동 생성 을 켜면, 플레이 시작 시 현재 씬에 NetworkLauncher 를 만든다
/// - 씬 파일을 바꾸지 않고 Town 등 기존 씬에서 멀티 접속을 테스트하기 위한 용도
/// - 끄면 기존과 똑같이 싱글로 동작한다
/// </summary>
[InitializeOnLoad]
public static class MultiTestLauncherMenu
{
    private const string MENU_PATH = "Tools/Multi/테스트 런처 자동 생성";
    private const string PREF_KEY = "Yggdrasil.MultiTestLauncher";
    private const string AVATAR_PREFAB_PATH = "Assets/Prefabs/Multi/PlayerAvatar.prefab";

    static MultiTestLauncherMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(MENU_PATH)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(PREF_KEY, EditorPrefs.GetBool(PREF_KEY, false) == false);
    }

    [MenuItem(MENU_PATH, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MENU_PATH, EditorPrefs.GetBool(PREF_KEY, false));
        return true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange _eState)
    {
        if (_eState != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }
        if (EditorPrefs.GetBool(PREF_KEY, false) == false)
        {
            return;
        }
        // 씬에 이미 런처가 있으면(MultiTest 씬) 만들지 않음
        if (Object.FindFirstObjectByType<NetworkLauncher>() != null)
        {
            return;
        }

        GameObject prefabAvatar = AssetDatabase.LoadAssetAtPath<GameObject>(AVATAR_PREFAB_PATH);
        if (prefabAvatar == null)
        {
            Debug.LogWarning("[MultiTestLauncherMenu] 분신 프리팹을 찾지 못함: " + AVATAR_PREFAB_PATH);
            return;
        }

        GameObject objLauncher = new GameObject("NetworkLauncher (Test)");
        // 위험: 접속 시 씬이 다시 로드되어도 입력 콜백이 끊기지 않도록 유지
        Object.DontDestroyOnLoad(objLauncher);

        NetworkLauncher launcher = objLauncher.AddComponent<NetworkLauncher>();
        SerializedObject so = new SerializedObject(launcher);
        so.FindProperty("playerPrefab").objectReferenceValue = prefabAvatar.GetComponent<NetworkObject>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
