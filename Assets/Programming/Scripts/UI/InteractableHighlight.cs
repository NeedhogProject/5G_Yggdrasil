// InteractableHighlight.cs
// 상호작용 물체(정건희 쪽 줄기/포탈/자원노드, 집 창고 등)에 붙이는 공용 강조 컴포넌트.
// 플레이어가 트리거 범위에 들어오면 공용 "E" 프롬프트(InteractionHint)를 이 물체 위에 띄우고,
// Outline Material 이 연결돼 있으면 외곽선도 켠다. 범위를 벗어나면 둘 다 끈다.
// 각 상호작용 스크립트를 수정하지 않고 같은 오브젝트에 이 컴포넌트만 추가하면 된다.
// 트리거 콜라이더(isTrigger=true)가 있어야 하며, 정건희 오브젝트의 기존 트리거를 그대로 공유한다.

using System.Collections.Generic;
using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [Header("키 프롬프트 설정")]
    [Tooltip("공용 'E' 프롬프트를 띄울 높이 (오브젝트 위로)")]
    [SerializeField] private float promptHeightOffset = 2f;

    [Tooltip("범위에 들어왔을 때 프롬프트에 표시할 문구 (예: E - 열쇠 삽입 / E - 채집)")]
    [SerializeField] private string interactHintText = "E - 상호작용";

    [Header("외곽선 설정 (지금은 비워둬도 됨)")]
    [Tooltip("InteractOutline 셰이더로 만든 머티리얼. 비우면 외곽선 없이 프롬프트만 동작")]
    [SerializeField] private Material outlineMaterial;

    [Tooltip("외곽선을 적용할 렌더러. 비워두면 자식 포함 모든 렌더러를 자동으로 찾는다")]
    [SerializeField] private Renderer[] targetRenderers;

    // 각 렌더러의 원래 머티리얼 배열 (외곽선 해제 시 되돌리기용)
    private List<Renderer> _renderers = new List<Renderer>();
    private List<Material[]> _baseMaterials = new List<Material[]>();

    private bool _highlighted = false;

    private void Awake()
    {
        CacheRenderers();
    }

    // 외곽선을 적용할 렌더러와 원래 머티리얼을 캐싱한다
    private void CacheRenderers()
    {
        _renderers.Clear();
        _baseMaterials.Clear();

        Renderer[] source = targetRenderers;

        // 인스펙터에 지정이 없으면 자식 포함 모든 렌더러 자동 수집
        if (source == null || source.Length == 0)
        {
            source = GetComponentsInChildren<Renderer>();
        }

        for (int i = 0; i < source.Length; i++)
        {
            Renderer renderer = source[i];
            if (renderer == null)
            {
                continue;
            }

            _renderers.Add(renderer);
            _baseMaterials.Add(renderer.sharedMaterials);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        ShowHighlight();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        HideHighlight();
    }

    // 외곽선 켜기 + 공용 프롬프트 표시
    private void ShowHighlight()
    {
        if (_highlighted == true)
        {
            return;
        }
        _highlighted = true;

        ApplyOutline(true);

        if (InteractionHint.Instance != null)
        {
            InteractionHint.Instance.Register(transform, promptHeightOffset, interactHintText);
        }
    }

    // 외곽선 끄기 + 공용 프롬프트 숨김
    private void HideHighlight()
    {
        if (_highlighted == false)
        {
            return;
        }
        _highlighted = false;

        ApplyOutline(false);

        if (InteractionHint.Instance != null)
        {
            InteractionHint.Instance.Unregister(transform);
        }
    }

    // 각 렌더러에 외곽선 머티리얼을 추가하거나(on) 원래대로 되돌린다(off)
    private void ApplyOutline(bool on)
    {
        if (outlineMaterial == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Count; i++)
        {
            Renderer renderer = _renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] baseArray = _baseMaterials[i];

            if (on == true)
            {
                // 원래 머티리얼 뒤에 외곽선 머티리얼을 한 칸 덧붙인다
                Material[] combined = new Material[baseArray.Length + 1];
                for (int m = 0; m < baseArray.Length; m++)
                {
                    combined[m] = baseArray[m];
                }
                combined[baseArray.Length] = outlineMaterial;

                renderer.sharedMaterials = combined;
            }
            else
            {
                // 원래 머티리얼 배열로 복원
                renderer.sharedMaterials = baseArray;
            }
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 외곽선/프롬프트가 남지 않도록 정리
        if (_highlighted == true)
        {
            HideHighlight();
        }
    }
}
