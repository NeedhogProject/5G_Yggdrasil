using UnityEngine;
using UnityEditor;

/// <summary>
/// ItemData 에셋이 추가, 삭제, 이동(이름 변경 포함)되면 ItemDataRegistry 목록을 자동 갱신 (에디터 전용)
///
/// [설정]
/// - 이 파일은 Editor 폴더 안에 있어야 함 (빌드에 포함되지 않음)
/// - ItemDataRegistry 가 붙은 프리팹(GameCore)을 찾아 목록을 다시 채우고 저장
/// </summary>
public class ItemDataRegistryAutoCollect : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] _importedAssets, string[] _deletedAssets, string[] _movedAssets, string[] _movedFromAssetPaths)
    {
        // 삭제된 에셋은 종류를 알 수 없어 .asset 이면 갱신 대상으로 봄
        if (HasItemData(_importedAssets) == false && HasItemData(_movedAssets) == false && HasAssetFile(_deletedAssets) == false)
        {
            return;
        }

        // 위험: 임포트 처리 중에 프리팹을 저장하면 다시 임포트가 걸리므로 임포트가 끝난 뒤 실행
        EditorApplication.delayCall -= RefreshRegistries;
        EditorApplication.delayCall += RefreshRegistries;
    }

    // ItemDataRegistry 가 붙은 프리팹을 찾아 목록 갱신 후 저장
    private static void RefreshRegistries()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ItemDataRegistry registry = prefab != null ? prefab.GetComponent<ItemDataRegistry>() : null;
            if (registry == null)
            {
                continue;
            }

            registry.CollectAllItems();
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    // 경로 목록에 ItemData 에셋이 있는지
    private static bool HasItemData(string[] _paths)
    {
        for (int i = 0; i < _paths.Length; i++)
        {
            System.Type type = AssetDatabase.GetMainAssetTypeAtPath(_paths[i]);
            if (type != null && typeof(ItemData).IsAssignableFrom(type) == true)
            {
                return true;
            }
        }
        return false;
    }

    // 경로 목록에 .asset 파일이 있는지
    private static bool HasAssetFile(string[] _paths)
    {
        for (int i = 0; i < _paths.Length; i++)
        {
            if (_paths[i].EndsWith(".asset") == true)
            {
                return true;
            }
        }
        return false;
    }
}
