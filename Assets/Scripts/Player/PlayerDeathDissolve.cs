using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 사망 디졸브 연출 (몬스터 처치 연출 EnemyDeathDissolve 와 같은 셰이더 사용)
///
/// - PlayerDeath 가 사망 시 Play, 부활 시 Restore 를 호출
/// - 사망 화면이 시간을 멈춰도 재생되도록 unscaled 시간 사용
/// - 부활하면 원래 머티리얼로 되돌림
///
/// [설정]
/// 1. Player 오브젝트(PlayerDeath 와 같은 오브젝트)에 부착
/// 2. dissolveMaterial 에 Shader Graphs_Dissolve 머티리얼 연결 (비어 있으면 연출 없이 동작)
/// </summary>
public class PlayerDeathDissolve : MonoBehaviour
{
    [Tooltip("디졸브 셰이더 머티리얼 (Shader Graphs_Dissolve)")]
    [SerializeField] private Material dissolveMaterial;

    [Tooltip("사라지는 데 걸리는 시간 (초)")]
    [SerializeField] private float duration = 1.5f;

    // 셰이더 프로퍼티
    private static readonly int ID_CUTOFF_HEIGHT  = Shader.PropertyToID("_Cutoff_Height");
    private static readonly int ID_BASE_TEXTURE   = Shader.PropertyToID("_Base_Texture");
    private static readonly int ID_ALBEDO         = Shader.PropertyToID("_Albedo");
    private static readonly int ID_NOISE_STRENGTH = Shader.PropertyToID("_Noise_Strength");
    private static readonly int ID_EDGE_WIDTH     = Shader.PropertyToID("_Edge_Width");

    // URP Lit 원본 프로퍼티
    private static readonly int ID_BASE_MAP   = Shader.PropertyToID("_BaseMap");
    private static readonly int ID_BASE_COLOR = Shader.PropertyToID("_BaseColor");

    // 교체한 렌더러와 원래 머티리얼 (부활 시 복구용, 같은 순서)
    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly List<Material[]> _originals = new List<Material[]>();
    private readonly List<Material> _instances = new List<Material>();

    private float _fStartY = 0f;
    private float _fEndY = 0f;
    private float _fElapsed = 0f;
    private bool _bPlaying = false;

    private void OnDestroy()
    {
        DestroyInstances();
    }

    /// <summary>사망 시 호출. 모든 메쉬 렌더러를 디졸브 머티리얼로 바꾸고 위에서 아래로 사라지게 함</summary>
    public void Play()
    {
        // 이미 재생했으면(복구 전) 다시 교체하지 않음
        if (dissolveMaterial == null || _renderers.Count > 0)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            // 메쉬 렌더러만 대상 (파티클 등 제외)
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

            if (_renderers.Count == 0)
            {
                bounds = renderer.bounds;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }

            _renderers.Add(renderer);
            _originals.Add(originals);
        }

        if (_renderers.Count == 0)
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

    /// <summary>부활 시 호출. 원래 머티리얼로 되돌림</summary>
    public void Restore()
    {
        for (int i = 0; i < _renderers.Count; i++)
        {
            // 위험: 사망 패널티로 무기 모델이 제거되므로 사라진 렌더러는 건너뜀
            if (_renderers[i] != null)
            {
                _renderers[i].sharedMaterials = _originals[i];
            }
        }
        _renderers.Clear();
        _originals.Clear();
        _bPlaying = false;

        DestroyInstances();
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

        _fElapsed += Time.unscaledDeltaTime;
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

    private void DestroyInstances()
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            Destroy(_instances[i]);
        }
        _instances.Clear();
    }
}
