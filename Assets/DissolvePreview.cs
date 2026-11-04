
using System;
using UnityEngine;
using UnityEngine.Rendering;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Renderer))]
public class DissolvePreview : MonoBehaviour
{
    [Header("Dissolve Settings")]
    public float duration = 1.5f;
    public float startCutoff = 2f;
    public float endCutoff = -1f;

    private Material materialInstance;
    private string cutoffProperty;
    private float elapsed;
    private bool isPlaying;

    void Start()
    {
        Renderer targetRenderer = GetComponent<Renderer>();
        materialInstance = targetRenderer.material;

        Shader shader = materialInstance.shader;

        // Cutoff Height 프로퍼티 자동 검색
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            ShaderPropertyType type = shader.GetPropertyType(i);

            if (type != ShaderPropertyType.Float &&
                type != ShaderPropertyType.Range)
                continue;

            string displayName = shader.GetPropertyDescription(i);
            string referenceName = shader.GetPropertyName(i);

            if (string.Equals(displayName, "Cutoff Height",
                StringComparison.OrdinalIgnoreCase) ||
                string.Equals(referenceName.Replace("_", ""),
                "CutoffHeight",
                StringComparison.OrdinalIgnoreCase))
            {
                cutoffProperty = referenceName;
                break;
            }
        }

        if (string.IsNullOrEmpty(cutoffProperty))
        {
            Debug.LogError("Cutoff Height 프로퍼티를 찾지 못했습니다.");
            enabled = false;
            return;
        }

        // 시작 시 자동 재생
        ReplayDissolve();
    }

    void Update()
    {
        bool spacePressed = false;

#if ENABLE_INPUT_SYSTEM
        spacePressed = Keyboard.current != null &&
                       Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        spacePressed = Input.GetKeyDown(KeyCode.Space);
#endif

        // 스페이스바로 처음부터 다시 재생
        if (spacePressed)
        {
            ReplayDissolve();
        }

        if (!isPlaying)
            return;

        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(
            elapsed / Mathf.Max(0.01f, duration)
        );

        float cutoff = Mathf.Lerp(
            startCutoff,
            endCutoff,
            t
        );

        materialInstance.SetFloat(cutoffProperty, cutoff);

        if (t >= 1f)
        {
            isPlaying = false;
        }
    }

    void ReplayDissolve()
    {
        elapsed = 0f;
        isPlaying = true;

        materialInstance.SetFloat(
            cutoffProperty,
            startCutoff
        );
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
