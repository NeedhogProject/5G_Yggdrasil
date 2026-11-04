using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 처치 디졸브 연출
///
/// - EnemyBase.OnDied 시 모든 렌더러 머티리얼을 디졸브 머티리얼 인스턴스로 교체
/// - 원래 머티리얼의 텍스처(_BaseMap)와 색(_BaseColor)을 이어받음
/// - Cutoff Height 를 렌더러 월드 높이의 위에서 아래로 내려 사라지게 함 (셰이더 Position 노드는 World 기준)
///
/// [설정]
/// 1. 적 프리팹 루트(EnemyBase 와 같은 오브젝트)에 부착
/// 2. dissolveMaterial 에 Shader Graphs_Dissolve 머티리얼 연결
/// 3. EnemyBase 의 destroyDelay 를 duration 보다 길게 설정
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyDeathDissolve : MonoBehaviour
{
    [Tooltip("디졸브 셰이더 머티리얼 (Shader Graphs_Dissolve)")]
    [SerializeField] private Material dissolveMaterial;

    [Tooltip("사라지는 데 걸리는 시간 (초)")]
    [SerializeField] private float duration = 1.5f;

    // 셰이더 프로퍼티
    private static readonly int ID_CUTOFF_HEIGHT = Shader.PropertyToID("_Cutoff_Height");
    private static readonly int ID_BASE_TEXTURE  = Shader.PropertyToID("_Base_Texture");
    private static readonly int ID_ALBEDO        = Shader.PropertyToID("_Albedo");
    private static readonly int ID_NOISE_STRENGTH = Shader.PropertyToID("_Noise_Strength");
    private static readonly int ID_EDGE_WIDTH    = Shader.PropertyToID("_Edge_Width");

    // URP Lit 원본 프로퍼티
    private static readonly int ID_BASE_MAP   = Shader.PropertyToID("_BaseMap");
    private static readonly int ID_BASE_COLOR = Shader.PropertyToID("_BaseColor");

    private EnemyBase _enemyBase;
    private readonly List<Material> _instances = new List<Material>();

    private float _fStartY = 0f;
    private float _fEndY = 0f;
    private float _fElapsed = 0f;
    private bool  _bPlaying = false;

    private void Awake()
    {
        _enemyBase = GetComponent<EnemyBase>();
        _enemyBase.OnDied += OnEnemyDied;
    }

    private void OnDestroy()
    {
        if (_enemyBase != null)
        {
            _enemyBase.OnDied -= OnEnemyDied;
        }

        for (int i = 0; i < _instances.Count; i++)
        {
            Destroy(_instances[i]);
        }
        _instances.Clear();
    }

    // 사망 시 머티리얼 교체 후 디졸브 시작
    private void OnEnemyDied(EnemyBase _enemy)
    {
        if (dissolveMaterial == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool bHasRenderer = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            // 메쉬 렌더러만 대상 (HP바, 파티클 제외)
            if (renderer is SkinnedMeshRenderer == false && renderer is MeshRenderer == false)
            {
                continue;
            }
            if (renderer.enabled == false)
            {
                continue;
            }

            Material[] originals = renderer.sharedMaterials;
            Material[] replaced = new Material[originals.Length];

            for (int j = 0; j < originals.Length; j++)
            {
                replaced[j] = CreateDissolveInstance(originals[j]);
            }
            renderer.sharedMaterials = replaced;

            if (bHasRenderer == false)
            {
                bounds = renderer.bounds;
                bHasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (bHasRenderer == false)
        {
            return;
        }

        // 노이즈와 경계선 두께만큼 여유를 두어 시작/끝에서 잘림이 보이지 않게 함
        float fMargin = dissolveMaterial.GetFloat(ID_NOISE_STRENGTH) + dissolveMaterial.GetFloat(ID_EDGE_WIDTH);
        _fStartY = bounds.max.y + fMargin;
        _fEndY = bounds.min.y - fMargin;
        _fElapsed = 0f;
        _bPlaying = true;

        SetCutoff(_fStartY);
    }

    // 원본 머티리얼의 텍스처와 색을 이어받은 디졸브 인스턴스 생성
    private Material CreateDissolveInstance(Material _original)
    {
        Material instance = new Material(dissolveMaterial);

        Texture texture = null;
        Color color = Color.white;
        if (_original != null)
        {
            if (_original.HasProperty(ID_BASE_MAP) == true)
            {
                texture = _original.GetTexture(ID_BASE_MAP);
            }
            if (_original.HasProperty(ID_BASE_COLOR) == true)
            {
                color = _original.GetColor(ID_BASE_COLOR);
            }
        }

        // 위험: 비워두면 디졸브 머티리얼의 기본 텍스처가 그대로 보이므로 흰색으로 대체
        instance.SetTexture(ID_BASE_TEXTURE, texture != null ? texture : Texture2D.whiteTexture);
        instance.SetColor(ID_ALBEDO, color);

        _instances.Add(instance);
        return instance;
    }

    private void Update()
    {
        if (_bPlaying == false)
        {
            return;
        }

        _fElapsed += Time.deltaTime;
        float fT = Mathf.Clamp01(_fElapsed / Mathf.Max(0.01f, duration));
        SetCutoff(Mathf.Lerp(_fStartY, _fEndY, fT));

        if (fT >= 1f)
        {
            _bPlaying = false;
        }
    }

    private void SetCutoff(float _fHeight)
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            _instances[i].SetFloat(ID_CUTOFF_HEIGHT, _fHeight);
        }
    }
}
