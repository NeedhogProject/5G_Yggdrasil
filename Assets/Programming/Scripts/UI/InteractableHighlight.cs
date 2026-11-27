// InteractableHighlight.cs
// 상호작용 물체(정건희 쪽 줄기/포탈/떨어진 아이템, 집 소품 등)에 붙이는 공용 강조 컴포넌트.
// 플레이어가 트리거 범위에 들어오면 테두리를 빛나게 하고, 공용 "E" 프롬프트(InteractionHint)를 이 물체 위에 띄운다.
// 범위를 벗어나면 둘 다 끈다.
// 각 상호작용 스크립트를 수정하지 않고 같은 오브젝트에 이 컴포넌트만 추가하면 된다.
// 트리거 콜라이더(isTrigger=true)가 있어야 하며, 정건희 오브젝트의 기존 트리거를 그대로 공유한다.
// 테두리 머티리얼은 비워두면 InteractOutlineEffect 가 기본(금색, 천천히 빛남)으로 자동 생성한다.

using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [Header("키 프롬프트 설정")]
    [Tooltip("체크 해제하면 E 프롬프트 없이 테두리만 빛난다 (자기 힌트가 따로 있는 오브젝트용)")]
    [SerializeField] private bool showPrompt = true;

    [Tooltip("공용 'E' 프롬프트를 띄울 높이 (오브젝트 위로)")]
    [SerializeField] private float promptHeightOffset = 2f;

    [Tooltip("범위에 들어왔을 때 프롬프트에 표시할 문구 (예: E - 열쇠 삽입 / E - 채집)")]
    [SerializeField] private string interactHintText = "E - 상호작용";

    [Header("강조 표시")]
    [Tooltip("범위에 들어오면 색과 테두리가 빛날 오브젝트들(여러 개 가능, 자식 포함). 비우면 이 오브젝트 자신")]
    [SerializeField] private GameObject[] highlightObjects;

    [Header("외곽선 설정")]
    [Tooltip("다른 색이나 두께를 쓰고 싶을 때만 InteractOutline 셰이더 머티리얼을 연결. 비우면 기본 테두리")]
    [SerializeField] private Material outlineMaterial;

    private bool _highlighted = false;

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

    // 테두리 켜기 + 공용 프롬프트 표시
    private void ShowHighlight()
    {
        if (_highlighted == true)
        {
            return;
        }
        _highlighted = true;

        // 프롬프트를 등록하면 InteractionHint 도 기본 테두리를 켜려 하므로, 지정 머티리얼이 있으면 먼저 켠다
        InteractOutlineEffect.Show(transform, highlightObjects, outlineMaterial);

        if (showPrompt == true && InteractionHint.Instance != null)
        {
            InteractionHint.Instance.Register(transform, promptHeightOffset, interactHintText);
        }
    }

    // 테두리 끄기 + 공용 프롬프트 숨김
    private void HideHighlight()
    {
        if (_highlighted == false)
        {
            return;
        }
        _highlighted = false;

        if (showPrompt == true && InteractionHint.Instance != null)
        {
            InteractionHint.Instance.Unregister(transform);
        }

        InteractOutlineEffect.Hide(transform);
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 테두리/프롬프트가 남지 않도록 정리
        if (_highlighted == true)
        {
            HideHighlight();
        }
    }
}
