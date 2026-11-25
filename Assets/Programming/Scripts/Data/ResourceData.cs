using UnityEngine;

/// <summary>
/// 자원(원석) 종류 - 각인/세트 효과에 사용되는 5종 원소
/// WeaponData 의 RuneElement 와 1:1 대응
/// </summary>
public enum ResourceType
{
    Fire    = 0,  // 불
    Water   = 1,  // 물
    Wind    = 2,  // 바람
    Earth   = 3,  // 땅
    Darkness= 4,  // 어둠
}

/// <summary>
/// 자원 획득 출처
/// </summary>
public enum ResourceDropSource
{
    NodeAndMonster, // 자원 노드 + 몬스터 드롭 (불/물/바람/땅)
    MonsterOnly,    // 몬스터 드롭 전용 (어둠)
}

/// <summary>
/// 자원 데이터 ScriptableObject — ItemData 상속
///
/// [기획 반영]
/// - 자원 5종 (불/물/바람/땅/어둠) — 각인·세트 효과 재료
/// - 불/물/바람/땅 : 자원 노드 + 몬스터 드롭으로 획득
/// - 어둠           : 몬스터 드롭 전용 (자원 노드 배치 불가)
/// - 인벤토리에서 중첩(스택) 가능
/// - 각인술사 NPC 에게 맡겨 각인 조합에 사용
/// </summary>
[CreateAssetMenu(fileName = "NewResource", menuName = "Yggdrasil/Items/ResourceData")]
public class ResourceData : ItemData
{
    // ─────────────────────── 자원 기본 스펙 ───────────────────────

    [Header("자원 원소 종류")]
    [SerializeField] private ResourceType resourceType = ResourceType.Fire;

    [Header("획득 출처")]
    [Tooltip("NodeAndMonster: 자원 노드 + 몬스터\nMonsterOnly: 몬스터 드롭 전용 (어둠)")]
    [SerializeField] private ResourceDropSource dropSource = ResourceDropSource.NodeAndMonster;

    [Header("층별 드롭 분포")]
    [Tooltip("각 인덱스 = 층 (0=1층, 1=2층, 2=3층, 3=4층). 해당 층에서 맵에 배치되는 기본 개수")]
    [SerializeField] private int[] floorSpawnCounts = { 10, 8, 6, 4 };

    [Header("희귀도 가중치")]
    [Tooltip("드롭 테이블에서 이 자원이 선택될 상대적 확률 가중치. 높을수록 자주 등장")]
    [SerializeField] [Range(1, 100)] private int dropWeight = 20;

    // ─────────────────────── 프로퍼티 ───────────────────────

    /// <summary>원소 종류</summary>
    public ResourceType ResourceType => resourceType;

    /// <summary>획득 출처</summary>
    public ResourceDropSource DropSource => dropSource;

    /// <summary>자원 노드에 배치 가능한지 (어둠은 불가)</summary>
    public bool CanSpawnOnNode => dropSource == ResourceDropSource.NodeAndMonster;

    /// <summary>드롭 가중치</summary>
    public int DropWeight => dropWeight;

    /// <summary>
    /// 특정 층의 기본 배치 개수 반환
    /// floorIndex: 0=1층, 1=2층, 2=3층, 3=4층(보스층)
    /// </summary>
    public int GetFloorSpawnCount(int floorIndex)
    {
        if (floorSpawnCounts == null || floorSpawnCounts.Length == 0) return 0;
        floorIndex = Mathf.Clamp(floorIndex, 0, floorSpawnCounts.Length - 1);
        return floorSpawnCounts[floorIndex];
    }

    /// <summary>
    /// ResourceType → RuneElement 변환
    /// 각인 시스템(WeaponData/RuneInscriptionSystem)과 연동할 때 사용
    /// </summary>
    public RuneElement ToRuneElement()
    {
        return resourceType switch
        {
            ResourceType.Fire     => RuneElement.Fire,
            ResourceType.Water    => RuneElement.Water,
            ResourceType.Wind     => RuneElement.Wind,
            ResourceType.Earth    => RuneElement.Earth,
            ResourceType.Darkness => RuneElement.Darkness,
            _                     => RuneElement.None
        };
    }

    /// <summary>
    /// ResourceType 에서 InscriptionType 으로 변환
    /// 자원 인벤토리(ResourceInventory) 추가 시 사용
    /// </summary>
    public InscriptionType ToInscriptionType()
    {
        return resourceType switch
        {
            ResourceType.Fire     => InscriptionType.Fire,
            ResourceType.Water    => InscriptionType.Water,
            ResourceType.Wind     => InscriptionType.Wind,
            ResourceType.Earth    => InscriptionType.Earth,
            ResourceType.Darkness => InscriptionType.Darkness,
            _                     => InscriptionType.None
        };
    }

    // ─────────────────────── 에디터 유효성 검사 ───────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 어둠 원소는 자동으로 MonsterOnly 로 설정
        if (resourceType == ResourceType.Darkness &&
            dropSource != ResourceDropSource.MonsterOnly)
        {
            dropSource = ResourceDropSource.MonsterOnly;
            Debug.Log($"[ResourceData: {name}] 어둠 원소는 자동으로 MonsterOnly 설정됩니다.");
        }

        // 층 배열 4칸 고정
        if (floorSpawnCounts == null || floorSpawnCounts.Length != 4)
        {
            int[] fixed4 = new int[4];
            if (floorSpawnCounts != null)
                for (int i = 0; i < Mathf.Min(floorSpawnCounts.Length, 4); i++)
                    fixed4[i] = floorSpawnCounts[i];
            floorSpawnCounts = fixed4;
        }
        for (int i = 0; i < floorSpawnCounts.Length; i++)
            if (floorSpawnCounts[i] < 0) floorSpawnCounts[i] = 0;
    }
#endif
}
