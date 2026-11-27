// InteractOutlineEffect.cs
// 상호작용 대상을 강조하는 공용 도우미 (어디서나 Show/Hide 로 호출).
// 대상과 그 자식의 3D 메시 색을 강조색(기본 금색) 쪽으로 물들였다 되돌리기를 반복해 깜빡이게 한다.
// 실제 색 변경은 각 렌더러에 붙는 InteractTintPulse 가 하고, Hide 하면 떼어 원래 색으로 돌아간다.
// 공용 E 프롬프트(InteractionHint), 창고, 집 문, InteractableHighlight 가 이 도우미를 쓴다.
// 강조색과 세기는 아래 TintColor / TintStrength / PulseSpeed 로 한 곳에서 조절한다.
// 색 깜빡임과 함께 InteractOutline.shader(Resources 폴더) 로 금색 테두리도 그린다. UseOutline 으로 끌 수 있다.
// 담당: 김보민

using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class InteractOutlineEffect
{
    // 강조색 (기본 금색)
    public static Color TintColor = new Color(1f, 0.93f, 0.55f, 1f);

    // 가장 진할 때 강조색이 섞이는 비율 (0 이면 변화 없음, 1 이면 완전히 강조색)
    public static float TintStrength = 0.7f;

    // 깜빡이는 속도 (클수록 빠름)
    public static float PulseSpeed = 3f;

    // 강조색 밝기 배수. 1 보다 크면 블룸(빛 번짐)이 켜진 화면에서 빛나 보인다
    public static float TintIntensity = 2f;

    // 테두리 사용 여부
    public static bool UseOutline = true;

    // 테두리 두께 (화면 기준, 0~20)
    public static float OutlineWidth = 4f;

    // 기본 테두리 머티리얼 (처음 쓸 때 한 번만 만든다)
    private static Material _defaultOutlineMaterial = null;

    // 강조가 켜진 대상별로 붙인 깜빡임 컴포넌트 목록
    private static readonly Dictionary<Transform, List<InteractTintPulse>> _targets = new Dictionary<Transform, List<InteractTintPulse>>();

    // 기본 강조색으로 켜기
    public static void Show(Transform target)
    {
        Show(target, (Material)null);
    }

    // 강조 켜기. colorSource 머티리얼에 _OutlineColor 가 있으면 그 색을 강조색으로 쓴다. 이미 켜져 있으면 무시
    public static void Show(Transform target, Material colorSource)
    {
        if (target == null)
        {
            return;
        }

        Color tintColor = TintColor;
        Material outlineMaterial = GetDefaultOutlineMaterial();
        if (colorSource != null && colorSource.HasProperty("_OutlineColor") == true)
        {
            // 테두리 셰이더 머티리얼을 받았으면 그 색과 머티리얼을 그대로 쓴다
            tintColor = colorSource.GetColor("_OutlineColor");
            outlineMaterial = colorSource;
        }

        ShowRenderers(target, target.GetComponentsInChildren<Renderer>(), tintColor, outlineMaterial);
    }

    // 강조 켜기 (색이 바뀔 렌더러를 직접 지정). 목록이 비어 있으면 target 자신과 자식을 강조
    public static void Show(Transform target, Renderer[] renderers)
    {
        if (target == null)
        {
            return;
        }

        if (renderers == null || renderers.Length == 0)
        {
            Show(target);
            return;
        }

        ShowRenderers(target, renderers, TintColor, GetDefaultOutlineMaterial());
    }

    // 강조 켜기 (색이 바뀔 오브젝트를 여러 개 지정). 각 오브젝트와 그 자식 메시가 모두 깜빡인다. 비어 있으면 target 자신
    public static void Show(Transform target, GameObject[] objects)
    {
        Show(target, objects, (Material)null);
    }

    // 강조 켜기 (오브젝트 여러 개 + 테두리 머티리얼 지정). colorSource 가 비어 있으면 기본 금색
    public static void Show(Transform target, GameObject[] objects, Material colorSource)
    {
        if (target == null)
        {
            return;
        }

        // 이미 켜져 있으면 렌더러를 다시 모으지 않는다 (매 프레임 호출 대비)
        PurgeDestroyed();
        if (_targets.ContainsKey(target) == true)
        {
            return;
        }

        Renderer[] renderers = CollectRenderers(objects);
        if (renderers.Length == 0)
        {
            Show(target, colorSource);
            return;
        }

        Color tintColor = TintColor;
        Material outlineMaterial = GetDefaultOutlineMaterial();
        if (colorSource != null && colorSource.HasProperty("_OutlineColor") == true)
        {
            tintColor = colorSource.GetColor("_OutlineColor");
            outlineMaterial = colorSource;
        }

        ShowRenderers(target, renderers, tintColor, outlineMaterial);
    }

    // 지정한 오브젝트들과 그 자식에 있는 렌더러를 모두 모은다 (빈 칸은 건너뜀)
    public static Renderer[] CollectRenderers(GameObject[] objects)
    {
        List<Renderer> result = new List<Renderer>();
        if (objects == null)
        {
            return result.ToArray();
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null)
            {
                continue;
            }

            Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>();
            for (int j = 0; j < childRenderers.Length; j++)
            {
                if (result.Contains(childRenderers[j]) == false)
                {
                    result.Add(childRenderers[j]);
                }
            }
        }

        return result.ToArray();
    }

    // target 을 기록 키로 삼아 지정한 렌더러들에 깜빡임을 붙인다
    private static void ShowRenderers(Transform target, Renderer[] renderers, Color tintColor, Material outlineMaterial)
    {
        PurgeDestroyed();

        if (_targets.ContainsKey(target) == true)
        {
            return;
        }

        List<InteractTintPulse> pulses = new List<InteractTintPulse>();

        // 밝기 배수를 곱해 블룸이 걸릴 만큼 밝게 (알파는 그대로)
        Color glowColor = tintColor * TintIntensity;
        glowColor.a = tintColor.a;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (IsTintTarget(renderer) == false)
            {
                continue;
            }

            // 다른 대상이 이미 강조 중인 렌더러는 건너뛴다 (중복 방지)
            if (renderer.GetComponent<InteractTintPulse>() != null)
            {
                continue;
            }

            InteractTintPulse pulse = renderer.gameObject.AddComponent<InteractTintPulse>();
            pulse.Setup(renderer, glowColor, TintStrength, PulseSpeed, outlineMaterial);
            pulses.Add(pulse);
        }

        _targets[target] = pulses;
    }

    // 기본 금색 테두리 머티리얼. 테두리를 끄거나 셰이더를 못 찾으면 null
    private static Material GetDefaultOutlineMaterial()
    {
        if (UseOutline == false)
        {
            return null;
        }

        if (_defaultOutlineMaterial == null)
        {
            Shader outlineShader = Shader.Find("Custom/InteractOutline");
            if (outlineShader == null)
            {
                Debug.LogWarning("[InteractOutlineEffect] Custom/InteractOutline 셰이더를 찾지 못함 (Resources 폴더 확인)");
                return null;
            }
            _defaultOutlineMaterial = new Material(outlineShader);
        }

        // 테두리도 블룸이 걸리도록 밝기 배수를 곱한다
        Color outlineColor = TintColor * TintIntensity;
        outlineColor.a = TintColor.a;
        _defaultOutlineMaterial.SetColor("_OutlineColor", outlineColor);
        _defaultOutlineMaterial.SetFloat("_OutlineWidth", OutlineWidth);
        return _defaultOutlineMaterial;
    }

    // 강조 끄기 (켜져 있지 않으면 무시)
    public static void Hide(Transform target)
    {
        if (target == null)
        {
            return;
        }

        List<InteractTintPulse> pulses = null;
        if (_targets.TryGetValue(target, out pulses) == false)
        {
            return;
        }

        for (int i = 0; i < pulses.Count; i++)
        {
            InteractTintPulse pulse = pulses[i];
            if (pulse == null)
            {
                continue;
            }

            // 바로 원래 색으로 되돌린 뒤 컴포넌트 제거
            pulse.Restore();
            Object.Destroy(pulse);
        }

        _targets.Remove(target);
    }

    // 강조할 렌더러인지 (3D 메시만. 글자, 스프라이트, 파티클 등은 제외)
    private static bool IsTintTarget(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        bool isMesh = renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        if (isMesh == false)
        {
            return false;
        }

        // 3D 글자(TextMeshPro)도 MeshRenderer 를 쓰므로 제외
        if (renderer.GetComponent<TMP_Text>() != null)
        {
            return false;
        }

        return true;
    }

    // 씬 전환 등으로 파괴된 대상 기록을 정리한다
    private static void PurgeDestroyed()
    {
        List<Transform> deadTargets = null;
        foreach (KeyValuePair<Transform, List<InteractTintPulse>> pair in _targets)
        {
            if (pair.Key == null)
            {
                if (deadTargets == null)
                {
                    deadTargets = new List<Transform>();
                }
                deadTargets.Add(pair.Key);
            }
        }

        if (deadTargets == null)
        {
            return;
        }

        for (int i = 0; i < deadTargets.Count; i++)
        {
            _targets.Remove(deadTargets[i]);
        }
    }
}
