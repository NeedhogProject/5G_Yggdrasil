// DalanIconLabel.cs
// 달란 금액을 표시하는 TMP_Text 옆(또는 가운데)에 달란 아이콘을 자동으로 붙여준다.
// 달란 텍스트와 같은 오브젝트에 이 컴포넌트를 붙이고 아이콘 스프라이트만 넣으면 된다.
// 담당: 김보민

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DalanIconLabel : MonoBehaviour
{
    [Tooltip("달란 아이콘 스프라이트")]
    public Sprite iconSprite;

    [Tooltip("아이콘을 텍스트 상자 가운데에 맞춘다 (체크 시 왼쪽/오른쪽 설정 무시)")]
    public bool centerOnBox = false;

    [Tooltip("가운데 정렬일 때 세로 위치 보정 (양수면 위로, 음수면 아래로)")]
    public float verticalOffset = 0f;

    [Tooltip("아이콘을 텍스트의 왼쪽에 둘지 여부 (체크 해제 시 오른쪽). 가운데 정렬이 아닐 때만 적용)")]
    public bool placeOnLeft = true;

    [Tooltip("아이콘 크기 (가로세로, 픽셀)")]
    public float iconSize = 28f;

    [Tooltip("텍스트와 아이콘 사이 간격 (픽셀)")]
    public float gap = 6f;

    // 런타임에 생성한 아이콘 Image
    private Image _icon = null;

    private void Start()
    {
        CreateIcon();
    }

    // 텍스트 옆(또는 가운데)에 아이콘 Image 를 하나 만들어 붙인다
    private void CreateIcon()
    {
        if (iconSprite == null)
        {
            Debug.LogWarning("[DalanIconLabel] Icon Sprite 가 비어 있습니다. 달란 이미지를 넣어주세요. (" + gameObject.name + ")");
            return;
        }

        // 이 오브젝트에 TMP_Text 가 없으면 자식, 그래도 없으면 부모에서 찾는다
        TMP_Text label = GetComponent<TMP_Text>();
        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>();
        }
        if (label == null)
        {
            label = GetComponentInParent<TMP_Text>();
        }
        if (label == null)
        {
            Debug.LogWarning("[DalanIconLabel] 근처에서 TMP_Text 를 찾지 못했습니다. 달란 텍스트 오브젝트에 붙여주세요. (" + gameObject.name + ")");
            return;
        }

        RectTransform labelRect = label.rectTransform;

        GameObject iconObj = new GameObject("DalanIcon", typeof(RectTransform));
        iconObj.transform.SetParent(labelRect, false);

        RectTransform rect = iconObj.GetComponent<RectTransform>();

        if (centerOnBox == true)
        {
            // 텍스트 상자 한가운데에 맞춘다 (세로는 verticalOffset 으로 보정)
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, verticalOffset);
        }
        else if (placeOnLeft == true)
        {
            // 텍스트 상자의 왼쪽 가장자리 바깥에, 세로 가운데로 붙인다
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-gap, 0f);
        }
        else
        {
            // 텍스트 상자의 오른쪽 가장자리 바깥에, 세로 가운데로 붙인다
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(gap, 0f);
        }

        rect.sizeDelta = new Vector2(iconSize, iconSize);
        rect.localScale = Vector3.one;

        Image image = iconObj.AddComponent<Image>();
        image.sprite = iconSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = true;

        // 다른 UI 에 가리지 않도록 맨 앞으로
        iconObj.transform.SetAsLastSibling();

        _icon = image;

        Debug.Log("[DalanIconLabel] 아이콘 생성 완료 (" + label.gameObject.name + ", centerOnBox=" + centerOnBox + ")");
    }
}
