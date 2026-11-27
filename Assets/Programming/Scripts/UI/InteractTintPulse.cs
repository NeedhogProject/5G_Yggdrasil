// InteractTintPulse.cs
// 상호작용 대상 렌더러의 색을 강조색(기본 금색) 쪽으로 물들였다 되돌리기를 반복해 깜빡이게 한다.
// 머티리얼 원본은 건드리지 않고 MaterialPropertyBlock 으로 이 오브젝트에만 색을 덮어쓴다.
// 여러 부위(서브메시)로 된 모델도 부위별로 원래 색을 기억했다가 각각 물들인다.
// 테두리 머티리얼을 받으면 머티리얼 목록 끝에 잠시 덧붙였다가 Restore 때 원래 목록으로 되돌린다.
// InteractOutlineEffect 가 런타임에 붙이고 떼므로 인스펙터에서 직접 붙일 필요는 없다.
// 담당: 김보민

using UnityEngine;

[DisallowMultipleComponent]
public class InteractTintPulse : MonoBehaviour
{
    private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor"); // URP Lit 기본 색
    private static readonly int COLOR_ID = Shader.PropertyToID("_Color");          // 구형/커스텀 셰이더 색

    private Renderer _renderer = null;
    private Color _tintColor = new Color(1f, 0.82f, 0.2f, 1f);
    private float _strength = 0.55f;  // 가장 진할 때 강조색 비율 (0~1)
    private float _speed = 3f;        // 깜빡이는 속도

    // 부위(머티리얼 칸)별 원래 색과 바꿀 색 이름 (색 속성이 없으면 -1)
    private Color[] _baseColors = null;
    private int[] _colorIds = null;
    private MaterialPropertyBlock _block = null;

    // 테두리를 붙이기 전 원래 머티리얼 목록 (되돌릴 때 사용)
    private Material[] _originalMaterials = null;
    private bool _outlineAdded = false;

    // 렌더러와 강조 설정을 받아 원래 색을 기억한다. outlineMaterial 이 있으면 테두리 머티리얼을 덧붙인다
    public void Setup(Renderer renderer, Color tintColor, float strength, float speed, Material outlineMaterial)
    {
        _renderer = renderer;
        _tintColor = tintColor;
        _strength = Mathf.Clamp01(strength);
        _speed = speed;
        _block = new MaterialPropertyBlock();

        Material[] materials = renderer.sharedMaterials;
        _baseColors = new Color[materials.Length];
        _colorIds = new int[materials.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            _colorIds[i] = -1;

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(BASE_COLOR_ID) == true)
            {
                _colorIds[i] = BASE_COLOR_ID;
                _baseColors[i] = material.GetColor(BASE_COLOR_ID);
            }
            else if (material.HasProperty(COLOR_ID) == true)
            {
                _colorIds[i] = COLOR_ID;
                _baseColors[i] = material.GetColor(COLOR_ID);
            }
        }

        // 테두리 머티리얼을 맨 뒤에 덧붙여 같은 메시를 한 번 더 그리게 한다
        _originalMaterials = materials;
        if (outlineMaterial != null)
        {
            Material[] withOutline = new Material[materials.Length + 1];
            for (int i = 0; i < materials.Length; i++)
            {
                withOutline[i] = materials[i];
            }
            withOutline[materials.Length] = outlineMaterial;
            renderer.sharedMaterials = withOutline;
            _outlineAdded = true;
        }
    }

    private void Update()
    {
        if (_renderer == null || _colorIds == null)
        {
            return;
        }

        // 시간이 멈춘 상태(설정창 등)에서도 깜빡이도록 실제 시간 사용
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _speed);
        ApplyTint(wave * _strength);
    }

    // 원래 색과 강조색을 amount 비율로 섞어 부위별로 적용
    private void ApplyTint(float amount)
    {
        for (int i = 0; i < _colorIds.Length; i++)
        {
            if (_colorIds[i] == -1)
            {
                continue;
            }

            Color mixed = Color.Lerp(_baseColors[i], _tintColor, amount);
            mixed.a = _baseColors[i].a;

            _renderer.GetPropertyBlock(_block, i);
            _block.SetColor(_colorIds[i], mixed);
            _renderer.SetPropertyBlock(_block, i);
        }
    }

    // 덮어쓴 색을 지워 원래 머티리얼 색으로 되돌린다
    public void Restore()
    {
        if (_renderer == null || _colorIds == null)
        {
            return;
        }

        for (int i = 0; i < _colorIds.Length; i++)
        {
            if (_colorIds[i] == -1)
            {
                continue;
            }
            _renderer.SetPropertyBlock(null, i);
        }

        // 덧붙였던 테두리 머티리얼 제거
        if (_outlineAdded == true)
        {
            _renderer.sharedMaterials = _originalMaterials;
            _outlineAdded = false;
        }
    }

    private void OnDestroy()
    {
        Restore();
    }
}
